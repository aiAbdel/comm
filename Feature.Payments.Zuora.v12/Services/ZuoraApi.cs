using Flurl.Http;
using Flurl.Http.Configuration;
using Sitecore.Configuration;
using System;
using System.Net;
using System.Threading.Tasks;
using Your.Feature.Payments.Infrastructure;

namespace Your.Feature.Payments.Services
{
  public class ZuoraApi : IZuoraApi
  {
    private readonly string _baseUrl = Settings.GetSetting("Zuora.BaseUrl");
    private readonly IFlurlClient _client;
    private readonly IZuoraAuth _auth;
    private readonly ILog _log;
    private readonly ICatalogCache _catalogCache;
    private readonly string _cachePrefix = Settings.GetSetting("Redis.Catalog.KeyPrefix", "sitecore:zuora");

    public ZuoraApi(IFlurlClientFactory factory, IZuoraAuth auth, ILog log, ICatalogCache catalogCache)
    { _client = factory.Get(_baseUrl); _auth = auth; _log = log; _catalogCache = catalogCache; }

    private async Task<IFlurlRequest> R(string path, string idem = null)
    {
      var req = _client.Request().AppendPathSegment(path).WithOAuthBearerToken(await _auth.GetTokenAsync());
      if (!string.IsNullOrWhiteSpace(idem)) req = req.WithHeader("Idempotency-Key", idem);
      return req.WithTimeout(TimeSpan.FromSeconds(45));
    }

    public async Task<dynamic> CreateAccountAsync(object body, string idempotencyKey)
    { try { return await (await R("v1/accounts", idempotencyKey)).PostJsonAsync(body).ReceiveJson<dynamic>(); } catch (FlurlHttpException ex) { await LogAndThrow("CreateAccount", ex); throw; } }
    public async Task<dynamic> PreviewOrderAsync(object body, string idempotencyKey)
    { try { return await (await R("v1/orders/preview", idempotencyKey)).PostJsonAsync(body).ReceiveJson<dynamic>(); } catch (FlurlHttpException ex) { await LogAndThrow("OrdersPreview", ex); throw; } }
    public async Task<dynamic> CreateOrderAsync(object body, string idempotencyKey)
    { try { return await (await R("v1/orders", idempotencyKey)).PostJsonAsync(body).ReceiveJson<dynamic>(); } catch (FlurlHttpException ex) { await LogAndThrow("CreateOrder", ex); throw; } }
    public async Task<dynamic> CreatePaymentSessionAsync(object body, string idempotencyKey)
    { try { return await (await R("v1/web-payments/sessions", idempotencyKey)).PostJsonAsync(body).ReceiveJson<dynamic>(); } catch (FlurlHttpException ex) { await LogAndThrow("CreatePaymentSession", ex); throw; } }
    public async Task<dynamic> UpdateAccountAsync(string accountKey, object body, string idempotencyKey)
    { try { return await (await R($"v1/accounts/{accountKey}", idempotencyKey)).PutJsonAsync(body).ReceiveJson<dynamic>(); } catch (FlurlHttpException ex) { await LogAndThrow("UpdateAccount", ex); throw; } }
    public async Task<dynamic> GetPaymentMethodsAsync(string accountKey)
    { try { return await (await R($"v1/payment-methods/accounts/{accountKey}")).GetJsonAsync<dynamic>(); } catch (FlurlHttpException ex) { await LogAndThrow("GetPaymentMethods", ex); throw; } }
    public async Task<dynamic> GetPaymentMethodAsync(string paymentMethodId)
    { try { return await (await R($"v1/payment-methods/{paymentMethodId}")).GetJsonAsync<dynamic>(); } catch (FlurlHttpException ex) { await LogAndThrow("GetPaymentMethod", ex); throw; } }
    public async Task<dynamic> UpdatePaymentMethodAsync(string paymentMethodId, object body, string idempotencyKey)
    { try { return await (await R($"v1/payment-methods/{paymentMethodId}", idempotencyKey)).PutJsonAsync(body).ReceiveJson<dynamic>(); } catch (FlurlHttpException ex) { await LogAndThrow("UpdatePaymentMethod", ex); throw; } }
    public async Task<dynamic> GetCatalogAsync()
    {
      var key = $"{_cachePrefix}:catalog:v1"; var lockKey = $"{key}:lock";
      int ttl = 600; try { ttl = int.Parse(Settings.GetSetting("Redis.Catalog.TtlSeconds", "600")); } catch {}
      var cached = await _catalogCache.GetAsync(key);
      if (!string.IsNullOrEmpty(cached)) { try { return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(cached); } catch {} }
      var lockVal = Guid.NewGuid().ToString(); var hasLock = await _catalogCache.AcquireLockAsync(lockKey, lockVal, 30);
      if (hasLock) {
        try { var res = await (await R("v1/catalog")).GetJsonAsync<dynamic>(); var json = Newtonsoft.Json.JsonConvert.SerializeObject(res); await _catalogCache.SetAsync(key, json, ttl); return res; }
        catch (FlurlHttpException ex) { await LogAndThrow("GetCatalog", ex); throw; }
        finally { await _catalogCache.ReleaseLockAsync(lockKey, lockVal); }
      } else {
        for (int i=0;i<4;i++){ await System.Threading.Tasks.Task.Delay(500); var again = await _catalogCache.GetAsync(key); if (!string.IsNullOrEmpty(again)) { try { return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(again); } catch {} } }
        return await (await R("v1/catalog")).GetJsonAsync<dynamic>();
      }
    }

    
    private async Task<dynamic> SendWithRetry(Func<Task<dynamic>> send, string op, string idempotencyKey)
    {
      int attempts = 0;
      Exception last = null;
      while (attempts < 2) // 1 retry on transients
      {
        attempts++;
        try { return await send(); }
        catch (FlurlHttpException ex)
        {
          int? sc = ex.Call?.Response?.StatusCode;
          bool transient = sc == null || sc == 408 || sc == 429 || (sc >= 500 && sc < 600);
          if (!transient || attempts >= 2) { await LogAndThrow(op, ex); throw; }
          _log.Warn($"Transient {op} error, retrying once", new System.Collections.Generic.Dictionary<string, object>{{"status", sc},{"idempotencyKey", idempotencyKey}});
          await Task.Delay(1500);
          last = ex;
        }
        catch (TaskCanceledException ex)
        {
          if (attempts >= 2) { _log.Error($"{op} timeout", ex, new System.Collections.Generic.Dictionary<string, object>{{"idempotencyKey", idempotencyKey}}); throw; }
          _log.Warn($"{op} timed out, retrying once", new System.Collections.Generic.Dictionary<string, object>{{"idempotencyKey", idempotencyKey}});
          await Task.Delay(1500);
          last = ex;
        }
      }
      throw last ?? new Exception($"{op} failed");
    }

    private async Task LogAndThrow(string op, FlurlHttpException ex)
    {
      string resp = null; try { resp = await ex.GetResponseStringAsync(); } catch {}
      _log.Error($"Zuora {op} failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"response", resp}});
    }
  }
}
