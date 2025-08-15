using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Your.Feature.Payments.Services;

namespace Your.Feature.Payments.Controllers
{
  public class CatalogController : Controller
  {
    private readonly IZuoraApi _zuora;
    public CatalogController(IZuoraApi z){ _zuora = z; }

    public class ProductCard {
      public string Sku { get; set; }
      public string Name { get; set; }
      public string Currency { get; set; }
      public decimal Price { get; set; }
      public bool PerUser { get; set; }
      public string RatePlanId { get; set; }
      public string ChargeId { get; set; }
      public int DefaultQty { get; set; } = 1;
    }

    public async Task<ActionResult> Index()
    {
      var want = new HashSet<string>(StringComparer.OrdinalIgnoreCase){ "PREMIUM", "BUSINESS", "TEAMS" };
      var cards = new List<ProductCard>();
      dynamic cat = await _zuora.GetCatalogAsync();
      var products = (IEnumerable<dynamic>)(cat?.products ?? new dynamic[0]);
      foreach (var p in products)
      {
        string sku = (string)(p?.sku ?? p?.name ?? "");
        if (string.IsNullOrEmpty(sku) || !want.Contains(sku.ToUpperInvariant())) continue;
        foreach (var rp in (IEnumerable<dynamic>)(p?.productRatePlans ?? new dynamic[0]))
        foreach (var ch in (IEnumerable<dynamic>)(rp?.productRatePlanCharges ?? new dynamic[0]))
        {
          var type = (string)(ch?.chargeType ?? "");
          if (!string.Equals(type, "Recurring", StringComparison.OrdinalIgnoreCase)) continue;
          decimal price = 0m; string currency = "USD";
          try { if (ch.price != null) price = (decimal)ch.price; } catch {}
          if (price <= 0m && ch.pricing != null && ch.pricing.price != null) try { price = (decimal)ch.pricing.price; } catch {}
          if (price <= 0m && ch.tiers != null && ch.tiers.Count > 0 && ch.tiers[0].price != null) try { price = (decimal)ch.tiers[0].price; } catch {}
          try { if (ch.currency != null) currency = (string)ch.currency; else if (ch.pricing?.currency != null) currency = (string)ch.pricing.currency; } catch {}
          bool perUser = false;
          try {
            var model = (string)(ch?.chargeModel ?? ch?.model ?? "");
            perUser = model.IndexOf("Per Unit", StringComparison.OrdinalIgnoreCase)>=0 || model.IndexOf("Tiered", StringComparison.OrdinalIgnoreCase)>=0 || model.IndexOf("Volume", StringComparison.OrdinalIgnoreCase)>=0;
          } catch {}
          if (price > 0m) {
            cards.Add(new ProductCard {
              Sku = sku.ToUpperInvariant(),
              Name = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(sku.ToLowerInvariant()),
              Currency = currency, Price = price, PerUser = perUser,
              RatePlanId = (string)(rp?.id ?? rp?.productRatePlanId),
              ChargeId = (string)(ch?.id ?? ch?.productRatePlanChargeId),
              DefaultQty = perUser ? 5 : 1
            });
            break;
          }
        }
      }
      return View("~/Views/Catalog/Index.cshtml", cards);
    }
  }
}
