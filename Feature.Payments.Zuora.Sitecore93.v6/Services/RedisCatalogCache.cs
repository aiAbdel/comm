using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sitecore.Configuration;
using StackExchange.Redis;
using Your.Feature.Payments.Infrastructure;

namespace Your.Feature.Payments.Services
{
  public class RedisCatalogCache : ICatalogCache, IDisposable
  {
    private readonly string _connStr = Settings.GetSetting("Redis.Catalog.ConnectionString", "localhost:6379,defaultDatabase=0");
    private readonly ConnectionMultiplexer _muxer;
    private readonly ILog _log;

    public RedisCatalogCache(ILog log)
    {
      _log = log;
      _muxer = ConnectionMultiplexer.Connect(_connStr);
    }

    private IDatabase Db => _muxer.GetDatabase();

    public async Task<string> GetAsync(string key)
    {
      try { return await Db.StringGetAsync(key); }
      catch (Exception ex) { _log.Warn("Redis GetAsync failed", new System.Collections.Generic.Dictionary<string, object>{{"key", key},{"ex", ex.Message}}); return null; }
    }

    public async Task SetAsync(string key, string json, int ttlSeconds)
    {
      try { await Db.StringSetAsync(key, json, TimeSpan.FromSeconds(ttlSeconds)); }
      catch (Exception ex) { _log.Warn("Redis SetAsync failed", new System.Collections.Generic.Dictionary<string, object>{{"key", key},{"ex", ex.Message}}); }
    }

    public async Task<bool> AcquireLockAsync(string lockKey, string value, int lockSeconds)
    {
      try { return await Db.StringSetAsync(lockKey, value, TimeSpan.FromSeconds(lockSeconds), When.NotExists); }
      catch (Exception ex) { _log.Warn("Redis AcquireLock failed", new System.Collections.Generic.Dictionary<string, object>{{"lockKey", lockKey},{"ex", ex.Message}}); return false; }
    }

    public async Task<bool> ReleaseLockAsync(string lockKey, string value)
    {
      try {
        var tran = Db.CreateTransaction();
        tran.AddCondition(Condition.StringEqual(lockKey, value));
        _ = tran.KeyDeleteAsync(lockKey);
        return await tran.ExecuteAsync();
      } catch (Exception ex) { _log.Warn("Redis ReleaseLock failed", new System.Collections.Generic.Dictionary<string, object>{{"lockKey", lockKey},{"ex", ex.Message}}); return false; }
    }

    public void Dispose() { try { _muxer?.Dispose(); } catch {} }
  }
}

    public async Task<int> ClearCatalogAsync(string keyPrefix)
    {
      int removed = 0;
      try
      {
        // Try to enumerate keys (requires allowAdmin=true in connection string)
        foreach (var ep in _muxer.GetEndPoints())
        {
          try
          {
            var server = _muxer.GetServer(ep);
            foreach (var key in server.Keys(pattern: keyPrefix + ":*"))
            {
              if (await Db.KeyDeleteAsync(key)) removed++;
            }
          }
          catch (Exception ex)
          {
            _log.Warn("Redis ClearCatalog: enumeration failed on endpoint", new System.Collections.Generic.Dictionary<string, object>{{"endpoint", ep.ToString()}, {"ex", ex.Message}});
          }
        }
      }
      catch (Exception ex)
      {
        _log.Warn("Redis ClearCatalog: enumeration top-level failed", new System.Collections.Generic.Dictionary<string, object>{{"ex", ex.Message}});
      }

      // Fallback to known keys (catalog key and lock)
      try { if (await Db.KeyDeleteAsync(keyPrefix + ":catalog:v1")) removed++; } catch {}
      try { if (await Db.KeyDeleteAsync(keyPrefix + ":catalog:v1:lock")) removed++; } catch {}

      return removed;
    }
