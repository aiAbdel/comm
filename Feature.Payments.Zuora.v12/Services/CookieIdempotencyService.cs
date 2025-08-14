using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Sitecore.Configuration;

namespace Your.Feature.Payments.Services
{
  public class CookieIdempotencyService : IIdempotencyService
  {
    private static readonly string CookieName = Settings.GetSetting("Idem.Cookie.Name", "zuora_idem");
    private static readonly int    MaxEntries = int.TryParse(Settings.GetSetting("Idem.Cookie.MaxEntries", "10"), out var n) ? n : 10;
    private static readonly int    TtlSeconds = int.TryParse(Settings.GetSetting("Idem.Cookie.TtlSeconds", "7200"), out var t) ? t : 7200;
    private static readonly string CookieDomain = Settings.GetSetting("Idem.Cookie.Domain", "");

    class Payload { public int v = 1; public Dictionary<string,string> k = new Dictionary<string,string>(); }

    public Task<string> GetOrCreateKeyAsync(string accountNumber, string ratePlanId, string chargeId, int quantity)
    {
      var ctx = HttpContext.Current ?? throw new InvalidOperationException("No HttpContext");
      var hash = HashIntent(accountNumber, ratePlanId, chargeId, quantity);
      var p = Read(ctx) ?? new Payload();

      if (p.k.TryGetValue(hash, out var existing) && !string.IsNullOrWhiteSpace(existing))
        return Task.FromResult(existing);

      var key = Guid.NewGuid().ToString();
      if (p.k.Count >= MaxEntries) // prune oldest-ish (arbitrary) entries
      {
        var e = new List<string>(p.k.Keys);
        var toDrop = Math.Max(1, p.k.Count - MaxEntries + 1);
        for (int i = 0; i < toDrop && i < e.Count; i++) p.k.Remove(e[i]);
      }
      p.k[hash] = key;
      Write(ctx, p, TtlSeconds);
      return Task.FromResult(key);
    }

    public Task<bool> ClearKeyAsync(string accountNumber, string ratePlanId, string chargeId, int quantity)
    {
      var ctx = HttpContext.Current;
      if (ctx == null) return Task.FromResult(false);
      var p = Read(ctx);
      if (p == null) return Task.FromResult(false);
      var hash = HashIntent(accountNumber, ratePlanId, chargeId, quantity);
      var removed = p.k.Remove(hash);
      Write(ctx, p, TtlSeconds);
      return Task.FromResult(removed);
    }

    private static Payload Read(HttpContext ctx)
    {
      var c = ctx.Request.Cookies[CookieName];
      if (c == null || string.IsNullOrEmpty(c.Value)) return null;
      try { return JsonConvert.DeserializeObject<Payload>(c.Value); } catch { return null; }
    }

    private static void Write(HttpContext ctx, Payload p, int ttlSeconds)
    {
      var json = JsonConvert.SerializeObject(p);
      var cookie = new HttpCookie(CookieName, json)
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/"
      };
      if (!string.IsNullOrWhiteSpace(CookieDomain)) cookie.Domain = CookieDomain;
      if (ttlSeconds > 0) cookie.Expires = DateTime.UtcNow.AddSeconds(ttlSeconds);
      ctx.Response.Cookies.Set(cookie);
    }

    private static string HashIntent(string accountNumber, string ratePlanId, string chargeId, int quantity)
    {
      var s = $"{accountNumber}|{ratePlanId}|{chargeId}|{quantity}";
      using (var sha = SHA256.Create())
      {
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s ?? ""));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
      }
    }
  }
}
