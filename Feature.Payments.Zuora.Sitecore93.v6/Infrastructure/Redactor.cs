using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Your.Feature.Payments.Infrastructure
{
  public static class Redactor
  {
    public static string MaskEmail(string email)
    {
      if (string.IsNullOrWhiteSpace(email)) return email;
      var parts = email.Split('@');
      if (parts.Length != 2) return "***";
      var name = parts[0];
      var domain = parts[1];
      var maskedName = name.Length <= 2 ? name + "***" : name[0] + new string('*', Math.Max(1, name.Length - 2)) + name[name.Length-1];
      return $"{maskedName}@{domain}";
    }

    public static string MaskName(string name)
    {
      if (string.IsNullOrWhiteSpace(name)) return name;
      var t = name.Trim();
      if (t.Length <= 2) return t[0] + "*";
      return t[0] + new string('*', t.Length - 2) + t[t.Length-1];
    }

    public static string MaskAddress(string address1)
    {
      if (string.IsNullOrWhiteSpace(address1)) return address1;
      return Regex.Replace(address1, @"\S", "*");
    }

    public static Dictionary<string, object> RedactContext(IDictionary<string, object> ctx)
    {
      if (ctx == null) return null;
      var res = new Dictionary<string, object>();
      foreach (var kv in ctx)
      {
        var key = kv.Key.ToLowerInvariant();
        var val = kv.Value;
        if (val == null) { res[kv.Key] = null; continue; }

        if (key.Contains("email")) res[kv.Key] = MaskEmail(val.ToString());
        else if (key.Contains("firstname") || key.Contains("lastname") || key.Contains("name")) res[kv.Key] = MaskName(val.ToString());
        else if (key.Contains("address1")) res[kv.Key] = MaskAddress(val.ToString());
        else res[kv.Key] = val;
      }
      return res;
    }
  }
}
