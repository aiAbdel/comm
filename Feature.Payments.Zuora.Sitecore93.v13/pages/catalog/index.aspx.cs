using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;

// Simple view model for the page
public class CatalogItemVM {
  public string Sku { get; set; } 
  public string Name { get; set; }
  public string Currency { get; set; }
  public decimal Price { get; set; }
  public bool PerUser { get; set; }
  public int DefaultQty { get; set; } = 1;
}

public partial class Catalog_Index : Page
{
  protected List<CatalogItemVM> Items;

  protected void Page_Load(object sender, EventArgs e)
  {
    if (IsPostBack) return;

    // Get catalog from your service (cached 10 min per v13)
    // You likely already have ICatalogService from the v13 package.
    var sp = ServiceLocator.ServiceProvider;
    var catalog = sp.GetService<ICatalogService>(); // your existing service
    var products = catalog != null ? catalog.GetDisplaySkus(new[] { "PREMIUM", "TEAMS", "BUSINESS" }) 
                                   : Fallback();

    Items = products.ToList(); // bound in markup via inline data-bind
  }

  private IEnumerable<CatalogItemVM> Fallback()
  {
    // Fallback hard-coded in case DI not wired yet
    return new[] {
      new CatalogItemVM{ Sku="PREMIUM", Name="Premium", Currency="USD", Price=34m, PerUser=false, DefaultQty=1 },
      new CatalogItemVM{ Sku="TEAMS",   Name="Teams",   Currency="USD", Price=7m,  PerUser=true,  DefaultQty=3 },
      new CatalogItemVM{ Sku="BUSINESS",Name="Business",Currency="USD", Price=7m,  PerUser=true,  DefaultQty=5 }
    };
  }
}
