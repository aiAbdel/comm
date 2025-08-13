using System.Threading.Tasks;
namespace Your.Feature.Payments.Services
{
  public interface IZuoraApi
  {
    Task<dynamic> CreateAccountAsync(object body, string idempotencyKey);
    Task<dynamic> PreviewOrderAsync(object body, string idempotencyKey);
    Task<dynamic> CreateOrderAsync(object body, string idempotencyKey);
    Task<dynamic> CreatePaymentSessionAsync(object body, string idempotencyKey);
    Task<dynamic> ApplyPaymentAsync(string paymentId, object body, string idempotencyKey);
    Task<dynamic> UpdateAccountAsync(string accountKey, object body, string idempotencyKey);
    Task<dynamic> GetPaymentMethodsAsync(string accountKey);
    Task<dynamic> GetCatalogAsync();
  }
}
