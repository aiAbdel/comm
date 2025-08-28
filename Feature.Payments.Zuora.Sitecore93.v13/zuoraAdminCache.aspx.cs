using System;
using System.Linq;
using System.Runtime.Caching;
using System.Web.UI;
using Microsoft.Extensions.DependencyInjection;
using Sitecore;
using Sitecore.DependencyInjection;

// If you registered your cache behind an interface:
public interface ICatalogCache {
    System.Threading.Tasks.Task<string> GetAsync(string key);
    System.Threading.Tasks.Task SetAsync(string key, string json, int ttlSeconds);
    System.Threading.Tasks.Task<bool> AcquireLockAsync(string lockKey, string value, int lockSeconds);
    System.Threading.Tasks.Task<bool> ReleaseLockAsync(string lockKey, string value);
    System.Threading.Tasks.Task<int> ClearCatalogAsync(string keyPrefix);
}

namespace Admin
{
    public partial class ZuoraAdmin : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            RequireAdmin();

            // Show MemoryCache entry count (whole process)
            var count = MemoryCache.Default.GetCount();
            litCount.Text = count.ToString("N0");
        }

        protected async void btnClear_Click(object sender, EventArgs e)
        {
            RequireAdmin();

            int removed = 0;

            // Try your ICatalogCache first (best, respects your key prefix)
            var svc = ServiceLocator.ServiceProvider.GetService<ICatalogCache>();
            if (svc != null)
            {
                // Use the same prefix you use when setting the catalog entry.
                // In our sample we used $"{prefix}:catalog:v1"
                removed = await svc.ClearCatalogAsync("zuora"); // adjust prefix if different
            }
            else
            {
                // Fallback: nuke MemoryCache entries that look like our catalog keys
                var cache = MemoryCache.Default;
                var keys = cache.Select(kvp => kvp.Key)
                                .Where(k => k.Contains(":catalog:")) // matches our sample naming
                                .ToList();
                foreach (var k in keys)
                    if (cache.Remove(k) != null) removed++;
            }

            litResult.Text = $"Cleared {removed} catalog-related entries at {DateTime.UtcNow:u}.";
            litCount.Text = MemoryCache.Default.GetCount().ToString("N0");
        }

        private void RequireAdmin()
        {
            if (Context.User == null || !Context.User.IsAdministrator)
            {
                Response.StatusCode = 403;
                Response.End();
            }
        }
    }
}
