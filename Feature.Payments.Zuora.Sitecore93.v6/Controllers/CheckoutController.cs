using System;
using System.Web.Mvc;
using Sitecore.Configuration;
using Your.Feature.Payments.Infrastructure;
using Your.Feature.Payments.Models;
using Your.Feature.Payments.Services;
using System.Collections.Generic;
using System.Linq;

namespace Your.Feature.Payments.Controllers
{
  public class CheckoutController : Controller
  {
    private readonly IZuoraApi _zuora;
    private readonly ILog _log;
    public CheckoutController(IZuoraApi zuora, ILog log) { _zuora = zuora; _log = log; }

    [HttpGet]
    public ActionResult Step1(string sku = "PREMIUM", int qty = 1) { ViewBag.Sku = sku; ViewBag.Qty = qty; return View("~/Views/Checkout/Step1.cshtml", new Step1_BillingAddressModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<ActionResult> Step1(Step1_BillingAddressModel m)
    {
      if (m == null) { ModelState.AddModelError("", "Invalid form"); return View("~/Views/Checkout/Step1.cshtml", m); }
      if (!ModelState.IsValid) return View("~/Views/Checkout/Step1.cshtml", m);

      var body = new {
        name = $"{m.FirstName} {m.LastName}".Trim(),
        currency = "USD",
        batch = "Batch1",
        billToContact = new { firstName = m.FirstName, lastName = m.LastName, email = m.Email, address1 = m.Address1, city = m.City, state = m.State, postalCode = m.PostalCode, country = m.Country }
      };

      try {
        var created = await _zuora.CreateAccountAsync(body, Guid.NewGuid().ToString());
        string accountNumber = created?.accountNumber ?? "";
        string accountId = created?.id ?? created?.accountId ?? "";
        if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(accountId)) { ModelState.AddModelError("", "Could not create account."); return View("~/Views/Checkout/Step1.cshtml", m); }

        TempData["AccountNumber"] = accountNumber; TempData["AccountId"] = accountId;
        var sku = (Request["sku"] ?? "PREMIUM").ToString(); int qty = 1; int.TryParse(Request["qty"], out qty); if (qty <= 0) qty = 1;
        return RedirectToAction("Step2", new { sku = sku, qty = qty });
      }
      catch (Exception ex) {
        _log.Error("Step1 account creation failed", ex, new Dictionary<string, object> {{"email", m.Email}, {"firstname", m.FirstName}, {"lastname", m.LastName}});
        ModelState.AddModelError("", "Sorry, we couldn't create your account right now.");
        return View("~/Views/Checkout/Step1.cshtml", m);
      }
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<ActionResult> Step2(string sku = "PREMIUM", int qty = 1)
    {
      var accountNumber = (string)TempData.Peek("AccountNumber");
      var accountId = (string)TempData.Peek("AccountId");
      if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(accountId)) return RedirectToAction("Step1");

      var ids = await ResolveIdsFromCatalogAsync(sku);
      if (ids == null) return Content("Product not configured.");

      var order = new {
        order = new {
          accountNumber,
          orderActions = new object[] {
            new {
              type = "CreateSubscription",
              createSubscription = new {
                termType = "TERMED", initialTerm = 12,
                subscriptions = new [] {
                  new {
                    ratePlanData = new {
                      ratePlan = new { productRatePlanId = ids.Item1 },
                      ratePlanCharges = new [] { new { productRatePlanChargeId = ids.Item2, quantity = qty } }
                    }
                  }
                }
              }
            }
          }
        }
      };

      try {
        var preview = await _zuora.PreviewOrderAsync(order, Guid.NewGuid().ToString());
        var vm = new Step2_PreviewModel { PlanSku = sku, Quantity = qty, AccountNumber = accountNumber, AccountId = accountId, PreviewPayload = preview };
        TempData.Keep("AccountNumber"); TempData.Keep("AccountId");
        return View("~/Views/Checkout/Step2.cshtml", vm);
      } catch (Exception ex) {
        _log.Error("Step2 preview failed", ex, new Dictionary<string, object> { {"accountNumber", accountNumber}, {"sku", sku} });
        return Content("Unable to preview order at this time.");
      }
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<ActionResult> Step3(string sku = "PREMIUM", int qty = 1)
    {
      var accountNumber = (string)TempData.Peek("AccountNumber");
      var accountId = (string)TempData.Peek("AccountId");
      if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(accountId)) return RedirectToAction("Step1");

      var ids = await ResolveIdsFromCatalogAsync(sku);
      if (ids == null) return Content("Product not configured.");

      dynamic methods = null;
      try { methods = await _zuora.GetPaymentMethodsAsync(accountNumber); } catch { }

      var vm = new Step3_PaymentModel {
        AccountNumber = accountNumber, AccountId = accountId,
        PublishableKey = Settings.GetSetting("Zuora.PaymentForm.PublishableKey"),
        Environment = Settings.GetSetting("Zuora.PaymentForm.Environment"),
        RatePlanId = ids.Item1, ChargeId = ids.Item2, Quantity = qty,
        GatewayInstanceName = Settings.GetSetting("Zuora.GatewayInstanceName"),
        ExistingPaymentMethods = methods
      };
      TempData.Keep("AccountNumber"); TempData.Keep("AccountId");
      return View("~/Views/Checkout/Step3_Payment.cshtml", vm);
    }

    private async System.Threading.Tasks.Task<Tuple<string,string>> ResolveIdsFromCatalogAsync(string sku)
    {
      try {
        var normalized = (sku ?? "").ToUpperInvariant();
        dynamic cat = await _zuora.GetCatalogAsync();
        var products = (System.Collections.Generic.IEnumerable<dynamic>)(cat?.products ?? new dynamic[0]);
        foreach (var p in products)
        {
          string key = (string)(p?.sku ?? p?.name ?? "");
          if (!string.Equals((key ?? "").ToUpperInvariant(), normalized, StringComparison.OrdinalIgnoreCase)) continue;
          var ratePlans = (System.Collections.Generic.IEnumerable<dynamic>)(p?.productRatePlans ?? new dynamic[0]);
          foreach (var rp in ratePlans)
          {
            var charges = (System.Collections.Generic.IEnumerable<dynamic>)(rp?.productRatePlanCharges ?? new dynamic[0]);
            foreach (var ch in charges)
            {
              string type = (string)(ch?.chargeType ?? "");
              if (!string.Equals(type, "Recurring", StringComparison.OrdinalIgnoreCase)) continue;
              string rpId = (string)(rp?.id ?? rp?.productRatePlanId);
              string chId = (string)(ch?.id ?? ch?.productRatePlanChargeId);
              if (!string.IsNullOrEmpty(rpId) && !string.IsNullOrEmpty(chId)) return Tuple.Create(rpId, chId);
            }
          }
        }
      } catch { }
      return null;
    }
  }
}
