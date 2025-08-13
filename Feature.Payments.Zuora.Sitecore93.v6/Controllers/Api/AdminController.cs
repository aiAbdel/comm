using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Sitecore.Configuration;
using Your.Feature.Payments.Infrastructure;
using Your.Feature.Payments.Services;

namespace Your.Feature.Payments.Controllers.Api
{
  public class AdminController : Controller
  {
    private readonly ICatalogCache _cache;
    private readonly ILog _log;
    public AdminController(ICatalogCache cache, ILog log) { _cache = cache; _log = log; }

    [HttpPost]
    public async Task<ActionResult> ClearCatalogCache()
    {
      var configuredSecret = Settings.GetSetting("Redis.Catalog.AdminSecret", "");
      if (string.IsNullOrWhiteSpace(configuredSecret))
        return new HttpStatusCodeResult(403, "Admin endpoint disabled");

      var provided = Request.Headers["X-Admin-Secret"] ?? Request["secret"];
      if (!SlowEquals(configuredSecret, provided))
        return new HttpStatusCodeResult(401, "Unauthorized");

      var prefix = Settings.GetSetting("Redis.Catalog.KeyPrefix", "sitecore:zuora");
      try
      {
        int n = await _cache.ClearCatalogAsync(prefix);
        _log.Info("Admin cleared catalog cache", new System.Collections.Generic.Dictionary<string, object>{{"prefix", prefix},{"removed", n}});
        return Json(new { cleared = n, prefix });
      }
      catch (Exception ex)
      {
        _log.Error("Admin clear catalog cache failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"prefix", prefix}});
        return new HttpStatusCodeResult(500, "Failed to clear cache");
      }
    }

    // Constant-time compare to avoid timing attacks
    private static bool SlowEquals(string a, string b)
    {
      if (a == null || b == null) return false;
      uint diff = (uint)a.Length ^ (uint)b.Length;
      for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
        diff |= (uint)(a[i] ^ b[i]);
      return diff == 0;
    }
  }
}
