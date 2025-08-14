using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Sitecore.Configuration;

namespace Your.Feature.Payments.Infrastructure
{
  public class DatadogLog : ILog
  {
    private static readonly object _sync = new object();
    private readonly string _service = Settings.GetSetting("Datadog.Service", "sitecore-payments");
    private readonly string _env = Settings.GetSetting("Datadog.Environment", "sandbox");
    private readonly string _source = Settings.GetSetting("Datadog.Source", "sitecore");
    private readonly bool _direct = Settings.GetSetting("Datadog.Logs.DirectHttp", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
    private readonly string _apiKey = Settings.GetSetting("Datadog.ApiKey", "");
    private readonly string _site = Settings.GetSetting("Datadog.Site", "datadoghq.com");
    private readonly string _dir = Settings.GetSetting("Datadog.Logs.Directory", @"C:\inetpub\logs\sitecore-payments");

    public void Info(string message, IDictionary<string, object> ctx = null) => Write("info", message, ctx);
    public void Warn(string message, IDictionary<string, object> ctx = null) => Write("warn", message, ctx);
    public void Error(string message, Exception ex = null, IDictionary<string, object> ctx = null)
    {
      var c = ctx ?? new Dictionary<string, object>();
      if (ex != null) c["exception"] = ex.ToString();
      Write("error", message, c);
    }

    private void Write(string level, string message, IDictionary<string, object> ctx)
    {
      var evt = new Dictionary<string, object> {
        { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
        { "service", _service },
        { "dd_env", _env },
        { "source", _source },
        { "level", level },
        { "message", message },
      };
      if (ctx != null) evt["context"] = ctx;
      if (_direct && !string.IsNullOrEmpty(_apiKey)) SendDirect(evt); else WriteToFile(evt);
    }

    private void WriteToFile(Dictionary<string, object> evt)
    {
      try {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, $"payments-{DateTime.UtcNow:yyyy-MM-dd}.log");
        lock (_sync) File.AppendAllText(path, JsonConvert.SerializeObject(evt) + Environment.NewLine);
      } catch {}
    }
    private static readonly HttpClient _http = new HttpClient();
    private void SendDirect(Dictionary<string, object> evt)
    {
      try {
        var url = $"https://http-intake.logs.{_site}/api/v2/logs";
        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("DD-API-KEY", _apiKey);
        req.Content = new StringContent(JsonConvert.SerializeObject(new[] { evt }), Encoding.UTF8, "application/json");
        _http.SendAsync(req).ConfigureAwait(false);
      } catch {}
    }
  }
}
