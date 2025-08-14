using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using Your.Feature.Payments.Infrastructure;
using Your.Feature.Payments.Services;

namespace Your.Feature.Payments.Controllers.Api
{
  public class WebhooksController : Controller
  {
    private readonly IProvisioningService _prov; private readonly ILog _log;
    public WebhooksController(IProvisioningService p, ILog log){ _prov = p; _log = log; }

    [HttpPost]
    public async Task<ActionResult> Zuora()
    {
      string raw; using (var r = new System.IO.StreamReader(Request.InputStream)) { raw = r.ReadToEnd(); }
      if (string.IsNullOrWhiteSpace(raw)) return new HttpStatusCodeResult(400,"Empty body");
      try {
        var token = JToken.Parse(raw);
        if (token.Type == JTokenType.Array) foreach (JObject ev in (JArray)token) await HandleEvent(ev);
        else await HandleEvent((JObject)token);
      } catch (Exception ex) { _log.Error("Webhook parse failed", ex); return new HttpStatusCodeResult(400,"Invalid JSON"); }
      return new HttpStatusCodeResult(200);
    }

    private async Task HandleEvent(JObject ev)
    {
      var type = ev["eventType"]?.ToString() ?? ev["type"]?.ToString() ?? "";
      var accountNumber = ev.SelectToken("account.accountNumber")?.ToString() ?? ev.SelectToken("accountNumber")?.ToString() ?? "";
      var subNumber = ev.SelectToken("subscription.subscriptionNumber")?.ToString() ?? ev.SelectToken("subscriptionNumber")?.ToString() ?? "";
      var invoiceNumber = ev.SelectToken("invoice.invoiceNumber")?.ToString() ?? "";
      var paymentStatus = ev.SelectToken("payment.status")?.ToString() ?? "";
      _log.Info("Webhook", new System.Collections.Generic.Dictionary<string, object>{{"type",type},{"accountNumber",accountNumber},{"subscriptionNumber",subNumber},{"paymentStatus",paymentStatus}});

      if (type.Equals("payment.processed", StringComparison.OrdinalIgnoreCase) || type.Equals("subscription.activated", StringComparison.OrdinalIgnoreCase))
      { if (!string.IsNullOrEmpty(subNumber)) await _prov.GrantAccessAsync(accountNumber, subNumber); }
      if (type.Equals("payment.reversed", StringComparison.OrdinalIgnoreCase) || type.Equals("payment.declined", StringComparison.OrdinalIgnoreCase))
      { await _prov.SuspendAccessAsync(accountNumber, "Payment failed or reversed"); }
      if (!string.IsNullOrEmpty(invoiceNumber)) await _prov.RecordInvoiceAsync(accountNumber, invoiceNumber);
    }
  }
}
