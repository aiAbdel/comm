using System.Threading.Tasks;
namespace Your.Feature.Payments.Services
{
  public interface ICatalogCache
  {
    System.Threading.Tasks.Task<string> GetAsync(string key);
    System.Threading.Tasks.Task SetAsync(string key, string json, int ttlSeconds);
    System.Threading.Tasks.Task<bool> AcquireLockAsync(string lockKey, string value, int lockSeconds);
    System.Threading.Tasks.Task<bool> ReleaseLockAsync(string lockKey, string value);
    System.Threading.Tasks.Task<int> ClearCatalogAsync(string keyPrefix);
  }
}
