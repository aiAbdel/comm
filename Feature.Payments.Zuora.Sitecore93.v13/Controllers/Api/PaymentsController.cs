using System;
using System.Web.Mvc;
using Your.Feature.Payments.Infrastructure;
using Your.Feature.Payments.Services;

namespace Your.Feature.Payments.Controllers.Api
{
  public class PaymentsController : Controller
  {
    private readonly IZuoraApi _zuora; private readonly ILog _log;
    public PaymentsController(IZuoraApi z, ILog l){ _zuora = z; _log = l; }

    public class CreateSessionDto { public string AccountId { get; set; } public decimal Amount { get; set; } public string Currency { get; set; } = "USD"; public bool StorePaymentMethod { get; set; } = true; }
    public class SetDefaultPmDto { public string AccountKey { get; set; } public string PaymentMethodId { get; set; } public bool AutoPay { get; set; } = true; }
    public class PaymentMethodAddressDto { public string PaymentMethodId { get; set; } public string Address1 { get; set; } public string Address2 { get; set; } public string City { get; set; } public string State { get; set; } public string PostalCode { get; set; } public string Country { get; set; } }

    [HttpPost]
    public async System.Threading.Tasks.Task<ActionResult> CreateSession(CreateSessionDto d)
    {
      if (d == null || string.IsNullOrWhiteSpace(d.AccountId)) return new HttpStatusCodeResult(400, "Invalid request");
      var body = new { accountId = d.AccountId, currency = d.Currency, amount = d.Amount, processPayment = false, storePaymentMethod = true };
      try { var res = await _zuora.CreatePaymentSessionAsync(body, Guid.NewGuid().ToString()); return Json(res); }
      catch (Exception ex) { _log.Error("Create payment session failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"accountId", d.AccountId}}); return new HttpStatusCodeResult(502, "Payment session unavailable"); }
    }

    [HttpPost]
    public async System.Threading.Tasks.Task<ActionResult> SetDefaultPayment(SetDefaultPmDto d)
    {
      if (d == null || string.IsNullOrWhiteSpace(d.AccountKey) || string.IsNullOrWhiteSpace(d.PaymentMethodId)) return new HttpStatusCodeResult(400, "Invalid request");
      try { var res = await _zuora.UpdateAccountAsync(d.AccountKey, new { defaultPaymentMethodId = d.PaymentMethodId, autoPay = d.AutoPay }, Guid.NewGuid().ToString()); return Json(res); }
      catch (Exception ex) { _log.Error("Set default payment failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"accountKey", d.AccountKey},{"paymentMethodId", d.PaymentMethodId}}); return new HttpStatusCodeResult(502, "Unable to set default payment"); }
    }

    [HttpPut]
    public async System.Threading.Tasks.Task<ActionResult> UpdatePaymentMethodAddress(PaymentMethodAddressDto d)
    {
      if (d == null || string.IsNullOrWhiteSpace(d.PaymentMethodId)) return new HttpStatusCodeResult(400, "Invalid request");
      try {
        var pm = await _zuora.GetPaymentMethodAsync(d.PaymentMethodId);
        var type = (string)(pm?.paymentMethodType ?? pm?.type ?? "").ToString();
        var normalized = string.IsNullOrEmpty(type) ? "" : type.ToLowerInvariant();
        object body; string strategy;
        if (normalized.Contains("credit") || normalized.Contains("card"))
        {
          body = new {
            creditCardAddress1 = d.Address1,
            creditCardAddress2 = d.Address2,
            creditCardCity = d.City,
            creditCardState = d.State,
            creditCardPostalCode = d.PostalCode,
            creditCardCountry = d.Country,
            accountHolderInfo = new { addressLine1 = d.Address1, addressLine2 = d.Address2, city = d.City, state = d.State, zipCode = d.PostalCode, country = d.Country }
          };
          strategy = "credit-card-address + accountHolderInfo";
        }
        else if (normalized.contains("creditcardreferencetransaction"))
        {
          return new HttpStatusCodeResult(409, "Cannot update address on CreditCardReferenceTransaction; create a new PM instead.");
        }
        else
        {
          body = new { accountHolderInfo = new { addressLine1 = d.Address1, addressLine2 = d.Address2, city = d.City, state = d.State, zipCode = d.PostalCode, country = d.Country } };
          strategy = "accountHolderInfo-only";
        }
        var res = await _zuora.UpdatePaymentMethodAsync(d.PaymentMethodId, body, Guid.NewGuid().ToString());
        _log.Info("Updated PM address", new System.Collections.Generic.Dictionary<string, object>{{"paymentMethodId", d.PaymentMethodId},{"type", type},{"strategy", strategy}});
        return Json(new { success = true, type, strategy });
      } catch (Exception ex) {
        _log.Error("Update payment method address failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"paymentMethodId", d.PaymentMethodId}});
        return new HttpStatusCodeResult(502, "Unable to update payment method");
      }
    }
  }
}
