using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using Sitecore.Configuration;
using Your.Feature.Payments.Infrastructure;
using Your.Feature.Payments.Services;

namespace Your.Feature.Payments.Controllers.Api
{
  public class WebhooksController : Controller
  {
    private readonly IProvisioningService _provisioning;
    private readonly ILog _log;
    public WebhooksController(IProvisioningService provisioning, ILog log) { _provisioning = provisioning; _log = log; }

    [HttpPost]
    public async Task<ActionResult> Zuora()
    {
      string raw; using (var r = new System.IO.StreamReader(Request.InputStream)) { raw = r.ReadToEnd(); }
      if (string.IsNullOrWhiteSpace(raw)) return new HttpStatusCodeResult(400, "Empty body");

      var verify = Settings.GetSetting("Zuora.Webhook.VerifySignature", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
      if (verify)
      {
        var shared = Settings.GetSetting("Zuora.Webhook.SharedSecret", "");
        var provided = Request.Headers["X-Zuora-Signature"] ?? Request.Headers["X-Signature"];
        if (!VerifyHmac(raw, shared, provided)) return new HttpStatusCodeResult(401, "Invalid signature");
      }

      try
      {
        var token = JToken.Parse(raw);
        if (token.Type == JTokenType.Array) foreach (var ev in (JArray)token) await HandleEvent((JObject)ev);
        else if (token.Type == JTokenType.Object) await HandleEvent((JObject)token);
      }
      catch (Exception ex) { _log.Error("Webhook parse failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"body", raw}}); return new HttpStatusCodeResult(400, "Invalid JSON"); }

      return new HttpStatusCodeResult(200);
    }

    private static bool VerifyHmac(string body, string secret, string provided)
    {
      try {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(provided)) return false;
        using (var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret))) {
          var sig = BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(body))).Replace("-", "").ToLowerInvariant();
          return sig == provided.ToLowerInvariant();
        }
      } catch { return false; }
    }

    private async Task HandleEvent(JObject ev)
    {
      var type = ev["eventType"]?.ToString() ?? ev["type"]?.ToString() ?? "";
      var accountNumber = ev.SelectToken("account.accountNumber")?.ToString() ?? ev.SelectToken("accountNumber")?.ToString() ?? "";
      var subNumber = ev.SelectToken("subscription.subscriptionNumber")?.ToString() ?? ev.SelectToken("subscriptionNumber")?.ToString() ?? "";
      var invoiceNumber = ev.SelectToken("invoice.invoiceNumber")?.ToString() ?? "";
      var paymentStatus = ev.SelectToken("payment.status")?.ToString() ?? ev.SelectToken("status")?.ToString() ?? "";

      _log.Info("Webhook received", new System.Collections.Generic.Dictionary<string, object>{{"eventType", type},{"accountNumber", accountNumber},{"subscriptionNumber", subNumber},{"paymentStatus", paymentStatus}});

      if (type.Equals("payment.processed", StringComparison.OrdinalIgnoreCase) ||
          (type.Equals("invoice.posted", StringComparison.OrdinalIgnoreCase) && paymentStatus.Equals("Processed", StringComparison.OrdinalIgnoreCase)) ||
          type.Equals("subscription.activated", StringComparison.OrdinalIgnoreCase))
      {
        if (!string.IsNullOrEmpty(subNumber)) await _provisioning.GrantAccessAsync(accountNumber, subNumber);
      }
      else if (type.Equals("payment.reversed", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("payment.declined", StringComparison.OrdinalIgnoreCase) ||
               paymentStatus.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
               paymentStatus.Equals("Declined", StringComparison.OrdinalIgnoreCase) ||
               paymentStatus.Equals("Reversed", StringComparison.OrdinalIgnoreCase))
      {
        await _provisioning.SuspendAccessAsync(accountNumber, "Payment failed or reversed");
      }

      if (!string.IsNullOrEmpty(invoiceNumber)) await _provisioning.RecordInvoiceAsync(accountNumber, invoiceNumber);
    }
  }
}
