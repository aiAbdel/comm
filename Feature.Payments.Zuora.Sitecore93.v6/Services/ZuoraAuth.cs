using Flurl.Http;
using Sitecore.Configuration;
using System;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Your.Feature.Payments.Infrastructure;

namespace Your.Feature.Payments.Services
{
  public class ZuoraAuth : IZuoraAuth
  {
    private readonly string _baseUrl = Settings.GetSetting("Zuora.BaseUrl");
    private readonly string _clientId = Settings.GetSetting("Zuora.ClientId");
    private readonly string _clientSecret = Settings.GetSetting("Zuora.ClientSecret");
    private readonly MemoryCache _cache = MemoryCache.Default;
    private readonly ILog _log;
    public ZuoraAuth(ILog log) { _log = log; }

    public async Task<string> GetTokenAsync()
    {
      var cacheKey = "zuora_oauth_token";
      if (_cache.Get(cacheKey) is string t) return t;

      try {
        var resp = await $"{_baseUrl}/oauth/token".PostUrlEncodedAsync(new {
          client_id = _clientId,
          client_secret = _clientSecret,
          grant_type = "client_credentials"
        }).ReceiveJson<dynamic>();
        string token = resp.access_token;
        int expires = resp.expires_in;
        _cache.Set(cacheKey, token, DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expires - 60)));
        _log.Info("Zuora OAuth token acquired");
        return token;
      } catch (FlurlHttpException ex) {
        var body = await ex.GetResponseStringAsync();
        _log.Error("Zuora OAuth token fetch failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"response", body}});
        throw;
      }
    }
  }
}
