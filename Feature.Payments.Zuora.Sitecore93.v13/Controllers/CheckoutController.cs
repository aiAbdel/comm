using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Sitecore.Configuration;
using Your.Feature.Payments.Models;
using Your.Feature.Payments.Services;

namespace Your.Feature.Payments.Controllers
{
  public class CheckoutController : Controller
  {
    private readonly IZuoraApi _zuora;
    public CheckoutController(IZuoraApi z){ _zuora = z; }

    private class PlanIds { public string RatePlanId; public string ChargeId; public string Currency; public decimal Price; public bool PerUser; }
    private async Task<PlanIds> ResolvePlanBySku(string sku)
    {
      dynamic cat = await _zuora.GetCatalogAsync();
      foreach (var p in (System.Collections.Generic.IEnumerable<dynamic>)(cat?.products ?? new dynamic[0]))
      {
        string ps = (string)(p?.sku ?? p?.name ?? "");
        if (!string.Equals(ps, sku, System.StringComparison.OrdinalIgnoreCase)) continue;
        foreach (var rp in (System.Collections.Generic.IEnumerable<dynamic>)(p?.productRatePlans ?? new dynamic[0]))
        foreach (var ch in (System.Collections.Generic.IEnumerable<dynamic>)(rp?.productRatePlanCharges ?? new dynamic[0]))
        {
          var type = (string)(ch?.chargeType ?? ""); if (!string.Equals(type, "Recurring", StringComparison.OrdinalIgnoreCase)) continue;
          decimal price = 0m; string currency = "USD"; bool perUser=false;
          try { if (ch.price != null) price = (decimal)ch.price; } catch {}
          if (price <= 0m && ch.pricing != null && ch.pricing.price != null) try { price = (decimal)ch.pricing.price; } catch {}
          if (price <= 0m && ch.tiers != null && ch.tiers.Count > 0 && ch.tiers[0].price != null) try { price = (decimal)ch.tiers[0].price; } catch {}
          try { if (ch.currency != null) currency = (string)ch.currency; else if (ch.pricing?.currency != null) currency = (string)ch.pricing.currency; } catch {}
          try { var model = (string)(ch?.chargeModel ?? ch?.model ?? ""); perUser = model.IndexOf("Per Unit", System.StringComparison.OrdinalIgnoreCase)>=0 || model.IndexOf("Tiered", System.StringComparison.OrdinalIgnoreCase)>=0 || model.IndexOf("Volume", System.StringComparison.OrdinalIgnoreCase)>=0; } catch {}
          return new PlanIds { RatePlanId = (string)(rp?.id ?? rp?.productRatePlanId), ChargeId = (string)(ch?.id ?? ch?.productRatePlanChargeId), Currency = currency, Price = price, PerUser = perUser };
        }
      }
      return null;
    }

    [HttpGet]
    public ActionResult Step1(string sku="PREMIUM", int qty=1)
    {
      return View("~/Views/Checkout/Step1.cshtml", new Step1_BillingAddressModel { Sku = sku, Quantity = qty, Country = "US" });
    }

    [HttpPost]
    public async Task<ActionResult> Step1(Step1_BillingAddressModel m)
    {
      if (!ModelState.IsValid) return View("~/Views/Checkout/Step1.cshtml", m);
      // Create a Zuora Account (simplified: one call with billToContact)
      var body = new {
        name = $"{m.FirstName} {m.LastName}",
        currency = "USD",
        billToContact = new { firstName = m.FirstName, lastName = m.LastName, workEmail = m.Email, address1 = m.Address1, address2 = m.Address2, city = m.City, state = m.State, postalCode = m.PostalCode, country = m.Country }
      };
      dynamic res = await _zuora.CreateAccountAsync(body, Guid.NewGuid().ToString());
      string accountNumber = (string)(res?.accountNumber ?? res?.basicInfo?.accountNumber ?? "");
      string accountId = (string)(res?.id ?? res?.accountId ?? "");
      TempData["AccountNumber"] = accountNumber; TempData["AccountId"] = accountId;
      return RedirectToAction("Step2", new { sku = m.Sku, qty = m.Quantity });
    }

    [HttpGet]
    public async Task<ActionResult> Step2(string sku="PREMIUM", int qty=1)
    {
      var accNum = (string)TempData.Peek("AccountNumber"); var accId = (string)TempData.Peek("AccountId");
      if (string.IsNullOrEmpty(accNum)) return RedirectToAction("Step1", new { sku, qty });
      var plan = await ResolvePlanBySku(sku);
      if (plan == null) return new HttpStatusCodeResult(400, "Unknown plan SKU");
      TempData["RatePlanId"] = plan.RatePlanId; TempData["ChargeId"] = plan.ChargeId;
      ViewBag.Currency = plan.Currency; ViewBag.Price = plan.Price; ViewBag.PerUser = plan.PerUser;
      return View("~/Views/Checkout/Step2.cshtml", new Step2_PreviewModel{ PlanSku=sku, Quantity=qty, AccountNumber=accNum, AccountId=accId, PreviewPayload = null });
    }

    [HttpGet]
    public ActionResult Step3(string sku="PREMIUM", int qty=1)
    {
      var accNum = (string)TempData.Peek("AccountNumber"); var accId = (string)TempData.Peek("AccountId");
      var ratePlanId = (string)TempData.Peek("RatePlanId"); var chargeId = (string)TempData.Peek("ChargeId");
      return View("~/Views/Checkout/Step3_Payment.cshtml", new Step3_PaymentModel {
        AccountNumber = accNum, AccountId = accId,
        PublishableKey = Settings.GetSetting("Zuora.PaymentForm.PublishableKey"),
        Environment = Settings.GetSetting("Zuora.PaymentForm.Environment"),
        RatePlanId = ratePlanId, ChargeId = chargeId, Quantity = qty
      });
    }

    [HttpGet]
    public ActionResult Success() => View("~/Views/Checkout/Success.cshtml");
  }
}
