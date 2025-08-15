using Flurl.Http.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Your.Feature.Payments.Infrastructure;
using Your.Feature.Payments.Services;

namespace Your.Feature.Payments.Startup
{
  public class ServicesConfigurator : IServicesConfigurator
  {
    public void Configure(IServiceCollection services)
    {
      services.AddSingleton<IFlurlClientFactory, PerBaseUrlFlurlClientFactory>();
      services.AddSingleton<ILog, DatadogLog>();
      services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
      services.AddSingleton<IZuoraAuth, ZuoraAuth>();
      services.AddSingleton<ICatalogCache, RedisCatalogCache>();
      services.AddSingleton<IZuoraApi, ZuoraApi>();
      services.AddSingleton<IProvisioningService, ProvisioningService>();
      services.AddSingleton<IIdempotencyService, CookieIdempotencyService>();
    }
  }
}
