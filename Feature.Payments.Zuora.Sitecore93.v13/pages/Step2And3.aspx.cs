using System;
using System.Web.UI;
using Sitecore.Configuration;

namespace Website.Checkout
{
  public partial class Step2And3 : Page
  {
    protected string AccountNumber, AccountId, Sku, PublishableKey, EnvironmentName;
    protected int Quantity;

    protected void Page_Load(object sender, EventArgs e)
    {
      // Input carried via querystring for stateless flow
      AccountNumber   = Request["account"] ?? "";
      AccountId       = Request["accountId"] ?? ""; // optional; not required for CreateSession if accountNumber→id is done server-side
      Sku             = Request["sku"] ?? "";
      Quantity        = Math.Max(1, int.TryParse(Request["qty"], out var q) ? q : 1);

      // From config
      PublishableKey  = Settings.GetSetting("Zuora.PublishableKey");
      EnvironmentName = Settings.GetSetting("Zuora.Environment", "sandbox");
    }
  }
}
