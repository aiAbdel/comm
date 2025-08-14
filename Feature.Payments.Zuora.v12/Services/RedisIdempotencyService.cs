using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Configuration;
using StackExchange.Redis;

namespace Your.Feature.Payments.Services
{
  public class RedisIdempotencyService : IIdempotencyService, IDisposable
  {
    private readonly string _connStr = Settings.GetSetting("Redis.Idem.ConnectionString", "localhost:6379,defaultDatabase=1");
    private readonly string _prefix = Settings.GetSetting("Redis.Idem.KeyPrefix", "sitecore:zuora:idem");
    private readonly int _ttlSeconds = int.Parse(Settings.GetSetting("Redis.Idem.TtlSeconds", "7200"));
    private readonly ConnectionMultiplexer _muxer;

    public RedisIdempotencyService(){ _muxer = ConnectionMultiplexer.Connect(_connStr); }
    private IDatabase Db => _muxer.GetDatabase();

    public async Task<string> GetOrCreateKeyAsync(string accountNumber, string ratePlanId, string chargeId, int quantity)
    {
      var intent = $"{accountNumber}|{ratePlanId}|{chargeId}|{quantity}";
      var hash = Sha256(intent);
      var key = $"{_prefix}:{hash}";
      var val = await Db.StringGetAsync(key);
      if (val.HasValue) return val.ToString();
      var newKey = Guid.NewGuid().ToString();
      bool ok = await Db.StringSetAsync(key, newKey, TimeSpan.FromSeconds(_ttlSeconds), When.NotExists);
      if (!ok) { val = await Db.StringGetAsync(key); if (val.HasValue) return val.ToString(); }
      return newKey;
    }

    public async Task<bool> ClearKeyAsync(string accountNumber, string ratePlanId, string chargeId, int quantity)
    {
      var intent = $"{accountNumber}|{ratePlanId}|{chargeId}|{quantity}";
      var hash = Sha256(intent);
      var key = $"{_prefix}:{hash}";
      return await Db.KeyDeleteAsync(key);
    }

    private static string Sha256(string input)
    {
      using (var sha = SHA256.Create())
      {
        var b = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? ""));
        var sb = new StringBuilder(b.Length*2);
        foreach (var x in b) sb.Append(x.ToString("x2"));
        return sb.ToString();
      }
    }

    public void Dispose(){ try { _muxer?.Dispose(); } catch{} }
  }
}
