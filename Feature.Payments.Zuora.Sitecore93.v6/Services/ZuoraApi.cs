using Flurl.Http;
using Flurl.Http.Configuration;
using Sitecore.Configuration;
using System;
using System.Threading.Tasks;
using Your.Feature.Payments.Infrastructure;

namespace Your.Feature.Payments.Services
{
  public class ZuoraApi : IZuoraApi
  {
    private readonly string _baseUrl = Settings.GetSetting("Zuora.BaseUrl");
    private readonly IFlurlClient _client;
    private readonly IZuoraAuth _auth; private readonly ICatalogCache _catalogCache; private readonly string _cachePrefix = Sitecore.Configuration.Settings.GetSetting("Redis.Catalog.KeyPrefix", "sitecore:zuora");
    private readonly ILog _log;

    public ZuoraApi(IFlurlClientFactory factory, IZuoraAuth auth, ILog log, ICatalogCache catalogCache)
    {
      _client = factory.Get(_baseUrl);
      _auth = auth; _log = log; _catalogCache = catalogCache;
    }

    private async Task<IFlurlRequest> R(string path, string idem = null)
    {
      var req = _client.Request().AppendPathSegment(path).WithOAuthBearerToken(await _auth.GetTokenAsync());
      if (!string.IsNullOrWhiteSpace(idem)) req = req.WithHeader("Idempotency-Key", idem);
      return req;
    }

    public async Task<dynamic> CreateAccountAsync(object body, string idempotencyKey)
    {
      try {
        _log.Info("Zuora CreateAccount -> request", new System.Collections.Generic.Dictionary<string, object>{{"path","/v1/accounts"},{"body",body}});
        var res = await (await R("v1/accounts", idempotencyKey)).PostJsonAsync(body).ReceiveJson<dynamic>();
        _log.Info("Zuora CreateAccount <- response", new System.Collections.Generic.Dictionary<string, object>{{"result",res}});
        return res;
      } catch(FlurlHttpException ex) { await LogAndThrow("CreateAccount", ex); throw; }
    }

    public async Task<dynamic> PreviewOrderAsync(object body, string idempotencyKey)
    {
      try {
        _log.Info("Zuora Orders Preview -> request", new System.Collections.Generic.Dictionary<string, object>{{"path","/v1/orders/preview"},{"body",body}});
        var res = await (await R("v1/orders/preview", idempotencyKey)).PostJsonAsync(body).ReceiveJson<dynamic>();
        _log.Info("Zuora Orders Preview <- response", new System.Collections.Generic.Dictionary<string, object>{{"result",res}});
        return res;
      } catch(FlurlHttpException ex) { await LogAndThrow("OrdersPreview", ex); throw; }
    }

    public async Task<dynamic> CreateOrderAsync(object body, string idempotencyKey)
    {
      try {
        _log.Info("Zuora CreateOrder -> request", new System.Collections.Generic.Dictionary<string, object>{{"path","/v1/orders"},{"body",body}});
        var res = await (await R("v1/orders", idempotencyKey)).PostJsonAsync(body).ReceiveJson<dynamic>();
        _log.Info("Zuora CreateOrder <- response", new System.Collections.Generic.Dictionary<string, object>{{"result",res}});
        return res;
      } catch(FlurlHttpException ex) { await LogAndThrow("CreateOrder", ex); throw; }
    }

    public async Task<dynamic> CreatePaymentSessionAsync(object body, string idempotencyKey)
    {
      try {
        _log.Info("Zuora CreatePaymentSession -> request", new System.Collections.Generic.Dictionary<string, object>{{"path","/v1/web-payments/sessions"},{"body",body}});
        var res = await (await R("v1/web-payments/sessions", idempotencyKey)).PostJsonAsync(body).ReceiveJson<dynamic>();
        _log.Info("Zuora CreatePaymentSession <- response", new System.Collections.Generic.Dictionary<string, object>{{"result",res}});
        return res;
      } catch(FlurlHttpException ex) { await LogAndThrow("CreatePaymentSession", ex); throw; }
    }

    public async Task<dynamic> ApplyPaymentAsync(string paymentId, object body, string idempotencyKey)
    {
      try {
        var path = $"v1/payments/{paymentId}/apply";
        _log.Info("Zuora ApplyPayment -> request", new System.Collections.Generic.Dictionary<string, object>{{"path","/"+path},{"body",body}});
        var res = await (await R(path, idempotencyKey)).PutJsonAsync(body).ReceiveJson<dynamic>();
        _log.Info("Zuora ApplyPayment <- response", new System.Collections.Generic.Dictionary<string, object>{{"result",res}});
        return res;
      } catch(FlurlHttpException ex) { await LogAndThrow("ApplyPayment", ex); throw; }
    }

    public async Task<dynamic> UpdateAccountAsync(string accountKey, object body, string idempotencyKey)
    {
      try {
        var path = $"v1/accounts/{accountKey}";
        _log.Info("Zuora UpdateAccount -> request", new System.Collections.Generic.Dictionary<string, object>{{"path","/"+path},{"body",body}});
        var res = await (await R(path, idempotencyKey)).PutJsonAsync(body).ReceiveJson<dynamic>();
        _log.Info("Zuora UpdateAccount <- response", new System.Collections.Generic.Dictionary<string, object>{{"result",res}});
        return res;
      } catch(FlurlHttpException ex) { await LogAndThrow("UpdateAccount", ex); throw; }
    }

    public async Task<dynamic> GetPaymentMethodsAsync(string accountKey)
    {
      try {
        var path = $"v1/payment-methods/accounts/{accountKey}";
        _log.Info("Zuora GetPaymentMethods -> request", new System.Collections.Generic.Dictionary<string, object>{{"path","/"+path}});
        var res = await (await R(path)).GetJsonAsync<dynamic>();
        _log.Info("Zuora GetPaymentMethods <- response", new System.Collections.Generic.Dictionary<string, object>{{"result","ok"}});
        return res;
      } catch(FlurlHttpException ex) { await LogAndThrow("GetPaymentMethods", ex); throw; }
    }

    public async Task<dynamic> GetCatalogAsync()
    {
      try {
        _log.Info("Zuora GetCatalog -> request", new System.Collections.Generic.Dictionary<string, object>{{"path","/v1/catalog"}});
        var res = await (await R("v1/catalog")).GetJsonAsync<dynamic>();
        _log.Info("Zuora GetCatalog <- response", new System.Collections.Generic.Dictionary<string, object>{{"result","ok"}});
        return res;
      } catch(FlurlHttpException ex) { await LogAndThrow("GetCatalog", ex); throw; }
    }

    private async Task LogAndThrow(string op, FlurlHttpException ex)
    {
      string resp = null; try { resp = await ex.GetResponseStringAsync(); } catch {}
      _log.Error($"Zuora {op} failed", ex, new System.Collections.Generic.Dictionary<string, object>{{"response", resp}});
    }
  }
}
