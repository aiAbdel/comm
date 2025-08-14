using System.Threading.Tasks;
namespace Your.Feature.Payments.Services
{
  public interface IIdempotencyService
  {
    Task<string> GetOrCreateKeyAsync(string accountNumber, string ratePlanId, string chargeId, int quantity);
    Task<bool> ClearKeyAsync(string accountNumber, string ratePlanId, string chargeId, int quantity);
  }
}
