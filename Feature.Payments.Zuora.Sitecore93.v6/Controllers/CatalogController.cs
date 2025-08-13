using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Your.Feature.Payments.Services;
using Your.Feature.Payments.Infrastructure;

namespace Your.Feature.Payments.Controllers
{
  public class CatalogController : Controller
  {
    private readonly IZuoraApi _zuora;
    private readonly ILog _log;
    public CatalogController(IZuoraApi zuora, ILog log) { _zuora = zuora; _log = log; }

    public class ProductCard {
      public string Sku { get; set; }
      public string Name { get; set; }
      public string Price { get; set; }
      public string Currency { get; set; }
      public string RatePlanId { get; set; }
      public string ChargeId { get; set; }
      public bool PerUser { get; set; }
      public int DefaultQty { get; set; } = 1;
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<ActionResult> Index()
    {
      var targetSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PREMIUM", "BUSINESS", "TEAMS" };
      var cards = new List<ProductCard>();

      try {
        dynamic cat = await _zuora.GetCatalogAsync();
        var products = (IEnumerable<dynamic>)(cat?.products ?? new dynamic[0]);
        foreach (var p in products)
        {
          string sku = (string)(p?.sku ?? p?.name ?? "");
          if (!targetSkus.Contains((sku ?? "").ToUpperInvariant())) continue;

          string ratePlanId = null, chargeId = null, currency = "USD";
          decimal price = 0m; bool perUser = false;
          var ratePlans = (IEnumerable<dynamic>)(p?.productRatePlans ?? new dynamic[0]);

          foreach (var rp in ratePlans)
          {
            var charges = (IEnumerable<dynamic>)(rp?.productRatePlanCharges ?? new dynamic[0]);
            foreach (var ch in charges)
            {
              string chargeType = (string)(ch?.chargeType ?? "");
              if (!string.Equals(chargeType, "Recurring", StringComparison.OrdinalIgnoreCase)) continue;
              decimal cand = 0m; string cur = "USD";
              try { if (ch.price != null) cand = (decimal)ch.price; } catch {}
              try { if (cand == 0m && ch.pricing != null && ch.pricing.price != null) cand = (decimal)ch.pricing.price; } catch {}
              try { if (cand == 0m && ch.tiers != null && ch.tiers.Count > 0 && ch.tiers[0].price != null) cand = (decimal)ch.tiers[0].price; } catch {}
              try { if (ch.currency != null) cur = (string)ch.currency; else if (ch.pricing != null && ch.pricing.currency != null) cur = (string)ch.pricing.currency; } catch {}

              if (cand > 0m) {
                ratePlanId = (string)(rp?.id ?? rp?.productRatePlanId);
                chargeId = (string)(ch?.id ?? ch?.productRatePlanChargeId);
                price = cand; currency = cur;
                var model = (string)(ch?.chargeModel ?? ch?.model ?? "");
                perUser = model.IndexOf("Per Unit", StringComparison.OrdinalIgnoreCase) >= 0
                       || model.IndexOf("PerUser", StringComparison.OrdinalIgnoreCase) >= 0
                       || model.IndexOf("Tiered", StringComparison.OrdinalIgnoreCase) >= 0;
                break;
              }
            }
            if (ratePlanId != null) break;
          }

          var name = (sku ?? "").Substring(0,1).ToUpperInvariant() + (sku ?? "").Substring(1).ToLowerInvariant();
          cards.Add(new ProductCard {
            Sku = sku.ToUpperInvariant(), Name = name,
            Price = price > 0m ? string.Format("{0} {1:0.00} / yr{2}", currency, price, perUser ? " / user" : "") : "—",
            Currency = currency, RatePlanId = ratePlanId, ChargeId = chargeId, PerUser = perUser
          });
        }
      } catch (Exception ex) {
        _log.Error("Catalog fetch failed", ex);
        cards.AddRange(new [] {
          new ProductCard{Sku="PREMIUM", Name="Premium", Price="—", Currency="USD"},
          new ProductCard{Sku="BUSINESS", Name="Business", Price="—", Currency="USD", PerUser=true},
          new ProductCard{Sku="TEAMS", Name="Teams", Price="—", Currency="USD", PerUser=true}
        });
      }
      return View("~/Views/Catalog/Index.cshtml", cards);
    }
  }
}
