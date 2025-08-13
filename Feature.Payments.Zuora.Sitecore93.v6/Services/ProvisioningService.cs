using System.Threading.Tasks;
using Your.Feature.Payments.Infrastructure;

namespace Your.Feature.Payments.Services
{
  public class ProvisioningService : IProvisioningService
  {
    private readonly ILog _log;
    public ProvisioningService(ILog log) { _log = log; }

    public Task GrantAccessAsync(string accountNumber, string subscriptionNumber)
    {
      _log.Info("Provisioning: Grant access", new System.Collections.Generic.Dictionary<string, object>{{"accountNumber", accountNumber},{"subscriptionNumber", subscriptionNumber}});
      return Task.CompletedTask;
    }

    public Task SuspendAccessAsync(string accountNumber, string reason)
    {
      _log.Info("Provisioning: Suspend access", new System.Collections.Generic.Dictionary<string, object>{{"accountNumber", accountNumber},{"reason", reason}});
      return Task.CompletedTask;
    }

    public Task RecordInvoiceAsync(string accountNumber, string invoiceNumber)
    {
      _log.Info("Provisioning: Record invoice", new System.Collections.Generic.Dictionary<string, object>{{"accountNumber", accountNumber},{"invoiceNumber", invoiceNumber}});
      return Task.CompletedTask;
    }
  }
}
