using System.Threading.Tasks;
namespace Your.Feature.Payments.Services
{
  public interface IIdempotencyService
  {
    System.Threading.Tasks.Task<string> GetOrCreateKeyAsync(string accountNumber, string ratePlanId, string chargeId, int quantity);
    System.Threading.Tasks.Task<bool> ClearKeyAsync(string accountNumber, string ratePlanId, string chargeId, int quantity);
  }
}
