// DTOs
public sealed class FinalizeRequest {
  public string AccountNumber { get; set; }              // e.g. "A00001234"
  public string PaymentMethodId { get; set; }            // from Payment Form result
  public string ProductRatePlanId { get; set; }
  public string ProductRatePlanChargeId { get; set; }
  public int Quantity { get; set; } = 1;
}

public sealed class FinalizeResponse {
  public bool Success { get; set; }
  public string OrderNumber { get; set; }
  public string SubscriptionNumber { get; set; }
  public string InvoiceNumber { get; set; }
  public string PaymentId { get; set; }
}

// Controller
public class PaymentsController : Controller
{
  private readonly IZuoraApi _zuora;
  private readonly IZuoraAccounts _accounts;     // service that does GET /v1/accounts/{key}
  private readonly IIdempotencyService _idem;
  private readonly ILog _log;

  public PaymentsController(IZuoraApi zuora, IZuoraAccounts accounts, IIdempotencyService idem, ILog log)
  {
    _zuora = zuora;
    _accounts = accounts;   // may be null if you haven’t wired it; code guards for that
    _idem = idem;
    _log = log;
  }

  [HttpPost]
  [ValidateAntiForgeryToken(false)]
  public async Task<ActionResult> Finalize([System.Web.Http.FromBody] FinalizeRequest req)
  {
    if (req == null || string.IsNullOrWhiteSpace(req.AccountNumber) ||
        string.IsNullOrWhiteSpace(req.PaymentMethodId) ||
        string.IsNullOrWhiteSpace(req.ProductRatePlanId) ||
        string.IsNullOrWhiteSpace(req.ProductRatePlanChargeId))
      return new HttpStatusCodeResult(400, "Missing required fields.");

    // Correlation for logs
    var corr = Guid.NewGuid().ToString("n");

    try
    {
      // 0) (Optional) Read latest Bill-To to patch PM address
      dynamic acc = null;
      try { acc = _accounts != null ? await _accounts.GetAsync(req.AccountNumber) : null; }
      catch (Exception ex) { _log.Warn("Finalize: account fetch failed", ex); }

      var bill = acc?.billToContact;
      if (bill != null)
      {
        // 1) Patch the saved PM with accountHolderInfo (aligns AVS/fraud signals)
        var body = new {
          accountHolderInfo = new {
            accountHolderName = $"{(string)bill.firstName} {(string)bill.lastName}".Trim(),
            addressLine1 = (string)bill.address1,
            addressLine2 = (string)bill.address2,
            city        = (string)bill.city,
            state       = (string)bill.state,
            zipCode     = (string)bill.zipCode,
            country     = (string)bill.country,
            email       = (string)bill.workEmail,
            phone       = (string)bill.workPhone
          }
        };
        await _zuora.UpdatePaymentMethodAsync(req.PaymentMethodId, body, idempotencyKey: $"pm-{corr}");
      }

      // 2) Set default PM & AutoPay
      var updateAccountBody = new {
        billingAndPayment = new {
          defaultPaymentMethodId = req.PaymentMethodId,
          autoPay = true
        }
      };
      await _zuora.UpdateAccountAsync(req.AccountNumber, updateAccountBody, idempotencyKey: $"acct-{corr}");

      // 3) Create Order (atomic bill + collect)
      var payload = new {
        existingAccountNumber = req.AccountNumber,
        orderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        subscriptions = new [] {
          new {
            orderActions = new [] {
              new {
                type = "CreateSubscription",
                createSubscription = new {
                  subscribeToRatePlans = new [] {
                    new {
                      productRatePlanId = req.ProductRatePlanId,
                      chargeOverrides = new [] {
                        new {
                          productRatePlanChargeId = req.ProductRatePlanChargeId,
                          quantity = req.Quantity
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        },
        processingOptions = new { runBilling = true, collectPayment = true }
      };

      // If your wrapper supports query params, prefer SetQueryParam("returnIds", true) internally.
      // If not, you can expose another method. Here we assume CreateOrderAsync accepts the path "v1/orders?returnIds=true".
      dynamic orderRes = await _zuora.CreateOrderAsync(payload, idempotencyKey: $"ord-{corr}"); // add ?returnIds=true in wrapper if needed

      // 4) Extract identifiers safely
      string orderNumber = (string)(orderRes?.orderNumber ?? "");
      string subscriptionNumber = (string)(orderRes?.subscriptions?.FirstOrDefault()?.subscriptionNumber ?? "");
      string invoiceNumber = (string)(orderRes?.invoices?.FirstOrDefault()?.invoiceNumber ?? "");
      string paymentId = (string)(orderRes?.payments?.FirstOrDefault()?.id ?? "");

      if (string.IsNullOrEmpty(orderNumber))
        return new HttpStatusCodeResult(502, "Order response missing orderNumber");

      _log.Info("Zuora checkout finalized", new {
        correlationId = corr, orderNumber, subscriptionNumber, invoiceNumber, paymentId
      });

      // 5) Success payload to the browser
      return Json(new FinalizeResponse {
        Success = true,
        OrderNumber = orderNumber,
        SubscriptionNumber = subscriptionNumber,
        InvoiceNumber = invoiceNumber,
        PaymentId = paymentId
      });
    }
    catch (Flurl.Http.FlurlHttpException ex)
    {
      var body = await ex.GetResponseStringAsync();
      _log.Error("Zuora finalize failed", ex, new { correlationId = corr, response = body });
      return new HttpStatusCodeResult((int)ex.Call.HttpStatus ?? 500, body ?? "Zuora error");
    }
    catch (Exception ex)
    {
      _log.Error("Finalize unexpected error", ex, new { correlationId = corr });
      return new HttpStatusCodeResult(500, "Unexpected error");
    }
  }
}
