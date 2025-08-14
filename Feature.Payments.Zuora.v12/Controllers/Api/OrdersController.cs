using System;
using System.Web.Mvc;
using Your.Feature.Payments.Infrastructure;
using Your.Feature.Payments.Services;

namespace Your.Feature.Payments.Controllers.Api
{
  public class OrdersController : Controller
  {
    private readonly IZuoraApi _zuora; private readonly ILog _log; private readonly IIdempotencyService _idem;
    public OrdersController(IZuoraApi z, ILog l, IIdempotencyService idem){ _zuora = z; _log = l; _idem = idem; }

    public class PlaceOrderDto { public string AccountNumber { get; set; } public string ProductRatePlanId { get; set; } public string ProductRatePlanChargeId { get; set; } public int Quantity { get; set; } = 1; public bool Collect { get; set; } = true; }
    public class PreviewTotalDto { public string AccountNumber { get; set; } public string ProductRatePlanId { get; set; } public string ProductRatePlanChargeId { get; set; } public int Quantity { get; set; } = 1; }

    [HttpPost]
    public async System.Threading.Tasks.Task<ActionResult> Place(PlaceOrderDto d)
    {
      if (d == null || string.IsNullOrWhiteSpace(d.AccountNumber) || string.IsNullOrWhiteSpace(d.ProductRatePlanId))
        return new HttpStatusCodeResult(400, "Invalid request");

      var order = new {
        existingAccountNumber = d.AccountNumber,
        processingOptions = new { runBilling = true, collectPayment = d.Collect },
        subscriptions = new [] {
          new { orderActions = new [] {
            new { type = "CreateSubscription", createSubscription = new {
              terms = new {
                initialTerm = new { period = 12, periodType = "Month", termType = "TERMED" },
                renewalSetting = "RENEW_WITH_SPECIFIC_TERM",
                renewalTerms = new [] { new { period = 12, periodType = "Month" } }
              },
              subscribeToRatePlans = new [] {
                new { productRatePlanId = d.ProductRatePlanId, chargeOverrides = new [] { new { productRatePlanChargeId = d.ProductRatePlanChargeId, quantity = d.Quantity } } }
              }
            }}
          }}
        }
      };
      try { var idem = await _idem.GetOrCreateKeyAsync(d.AccountNumber, d.ProductRatePlanId, d.ProductRatePlanChargeId, d.Quantity);
      var res = await _zuora.CreateOrderAsync(order, idem); return Json(res); }
      catch (Exception ex) { _log.Error("Order placement failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"accountNumber", d.AccountNumber},{"collect", d.Collect}}); return new HttpStatusCodeResult(502, "Order placement failed"); }
    }

    [HttpPost]
    public async System.Threading.Tasks.Task<ActionResult> PreviewTotal(PreviewTotalDto d)
    {
      if (d == null || string.IsNullOrWhiteSpace(d.AccountNumber) || string.IsNullOrWhiteSpace(d.ProductRatePlanId))
        return new HttpStatusCodeResult(400, "Invalid request");

      var body = new {
        order = new {
          accountNumber = d.AccountNumber,
          orderActions = new object[] {
            new {
              type = "CreateSubscription",
              createSubscription = new {
                termType = "TERMED", initialTerm = 12,
                subscriptions = new [] { new { ratePlanData = new {
                  ratePlan = new { productRatePlanId = d.ProductRatePlanId },
                  ratePlanCharges = new [] { new { productRatePlanChargeId = d.ProductRatePlanChargeId, quantity = d.Quantity } }
                } } }
              }
            }
          }
        }
      };
      try { var res = await _zuora.PreviewOrderAsync(body, Guid.NewGuid().ToString()); return Json(res); }
      catch (Exception ex) { _log.Error("Preview total failed", ex); return new HttpStatusCodeResult(502, "Preview total failed"); }
    }
  }
}
