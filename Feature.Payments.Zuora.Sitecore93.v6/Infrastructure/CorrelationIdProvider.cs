using System;
using System.Web;

namespace Your.Feature.Payments.Infrastructure
{
  public interface ICorrelationIdProvider
  {
    string GetOrCreate();
  }

  public class CorrelationIdProvider : ICorrelationIdProvider
  {
    private const string Key = "X-Correlation-Id";

    public string GetOrCreate()
    {
      var ctx = HttpContext.Current;
      if (ctx == null) return Guid.NewGuid().ToString();
      var id = ctx.Items[Key] as string;
      if (string.IsNullOrEmpty(id))
      {
        id = Guid.NewGuid().ToString();
        ctx.Items[Key] = id;
      }
      return id;
    }
  }
}
