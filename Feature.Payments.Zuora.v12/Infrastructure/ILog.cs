using System;
using System.Collections.Generic;

namespace Your.Feature.Payments.Infrastructure
{
  public interface ILog
  {
    void Info(string message, IDictionary<string, object> ctx = null);
    void Warn(string message, IDictionary<string, object> ctx = null);
    void Error(string message, Exception ex = null, IDictionary<string, object> ctx = null);
  }
}
