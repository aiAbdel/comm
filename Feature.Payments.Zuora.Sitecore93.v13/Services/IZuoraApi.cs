using System.Threading.Tasks;
namespace Your.Feature.Payments.Services
{
  public interface IZuoraApi
  {
    System.Threading.Tasks.Task<dynamic> CreateAccountAsync(object body, string idempotencyKey);
    System.Threading.Tasks.Task<dynamic> PreviewOrderAsync(object body, string idempotencyKey);
    System.Threading.Tasks.Task<dynamic> CreateOrderAsync(object body, string idempotencyKey);
    System.Threading.Tasks.Task<dynamic> CreatePaymentSessionAsync(object body, string idempotencyKey);
    System.Threading.Tasks.Task<dynamic> UpdateAccountAsync(string accountKey, object body, string idempotencyKey);
    System.Threading.Tasks.Task<dynamic> GetPaymentMethodsAsync(string accountKey);
    System.Threading.Tasks.Task<dynamic> GetPaymentMethodAsync(string paymentMethodId);
    System.Threading.Tasks.Task<dynamic> UpdatePaymentMethodAsync(string paymentMethodId, object body, string idempotencyKey);
    System.Threading.Tasks.Task<dynamic> GetCatalogAsync();
  }
}
