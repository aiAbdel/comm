using System;
using System.Web.Mvc;
using Your.Feature.Payments.Infrastructure;
using Your.Feature.Payments.Services;

namespace Your.Feature.Payments.Controllers.Api
{
  public class PaymentsController : Controller
  {
    private readonly IZuoraApi _zuora;
    private readonly ILog _log;
    public PaymentsController(IZuoraApi zuora, ILog log) { _zuora = zuora; _log = log; }

    public class CreateSessionDto { public string AccountId { get; set; } public decimal Amount { get; set; } public string Currency { get; set; } = "USD"; public bool ProcessPayment { get; set; } = true; public bool StorePaymentMethod { get; set; } = true; public string PaymentGateway { get; set; } }
    public class SetDefaultPmDto { public string AccountKey { get; set; } public string PaymentMethodId { get; set; } public bool AutoPay { get; set; } = true; }
    public class ApplyPaymentDto { public string PaymentId { get; set; } public string InvoiceId { get; set; } public decimal Amount { get; set; } }

    [HttpPost]
    public async System.Threading.Tasks.Task<ActionResult> CreateSession(CreateSessionDto d)
    {
      if (d == null || string.IsNullOrWhiteSpace(d.AccountId)) return new HttpStatusCodeResult(400, "Invalid request");
      var body = new { accountId = d.AccountId, currency = d.Currency, amount = d.Amount, processPayment = d.ProcessPayment, storePaymentMethod = d.StorePaymentMethod, paymentGateway = d.PaymentGateway };
      try { var res = await _zuora.CreatePaymentSessionAsync(body, Guid.NewGuid().ToString()); return Json(res); }
      catch (Exception ex) { _log.Error("Create payment session failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"accountId", d.AccountId},{"amount", d.Amount}}); return new HttpStatusCodeResult(502, "Payment session unavailable"); }
    }

    [HttpPost]
    public async System.Threading.Tasks.Task<ActionResult> SetDefaultPayment(SetDefaultPmDto d)
    {
      if (d == null || string.IsNullOrWhiteSpace(d.AccountKey) || string.IsNullOrWhiteSpace(d.PaymentMethodId)) return new HttpStatusCodeResult(400, "Invalid request");
      try { var res = await _zuora.UpdateAccountAsync(d.AccountKey, new { defaultPaymentMethodId = d.PaymentMethodId, autoPay = d.AutoPay }, Guid.NewGuid().ToString()); return Json(res); }
      catch (Exception ex) { _log.Error("Set default payment failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"accountKey", d.AccountKey},{"paymentMethodId", d.PaymentMethodId}}); return new HttpStatusCodeResult(502, "Unable to set default payment"); }
    }

    [HttpPost]
    public async System.Threading.Tasks.Task<ActionResult> Apply(ApplyPaymentDto d)
    {
      if (d == null || string.IsNullOrWhiteSpace(d.PaymentId) || string.IsNullOrWhiteSpace(d.InvoiceId) || d.Amount <= 0) return new HttpStatusCodeResult(400, "Invalid request");
      try { var res = await _zuora.ApplyPaymentAsync(d.PaymentId, new { invoices = new [] { new { invoiceId = d.InvoiceId, amount = d.Amount } } }, Guid.NewGuid().ToString()); return Json(res); }
      catch (Exception ex) { _log.Error("Apply payment failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"paymentId", d.PaymentId},{"invoiceId", d.InvoiceId},{"amount", d.Amount}}); return new HttpStatusCodeResult(502, "Unable to apply payment"); }
    }
  }
}
