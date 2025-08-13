using System.Threading.Tasks;

namespace Your.Feature.Payments.Services
{
  public interface ICatalogCache
  {
    Task<string> GetAsync(string key);
    Task SetAsync(string key, string json, int ttlSeconds);
    Task<bool> AcquireLockAsync(string lockKey, string value, int lockSeconds);
    Task<bool> ReleaseLockAsync(string lockKey, string value);
    Task<int> ClearCatalogAsync(string keyPrefix);
  }
}
