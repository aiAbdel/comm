using System;
using System.Web.UI;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;

public partial class Checkout_Step1Billing : Page
{
  protected string Sku;
  protected int Quantity;
  protected string ExistingAccount; // account number or id

  // Prefill fields
  protected string FirstName, LastName, Address1, Address2, City, State, Postal, Country, Email, Phone;

  private IZuoraApi _zuora;

  protected async void Page_Load(object sender, EventArgs e)
  {
    var sp = ServiceLocator.ServiceProvider;
    _zuora = sp.GetService<IZuoraApi>();

    Sku = Request["sku"] ?? "";
    Quantity = Math.Max(1, int.TryParse(Request["qty"], out var q) ? q : 1);
    ExistingAccount = Request["account"] ?? "";

    if (!IsPostBack && !string.IsNullOrWhiteSpace(ExistingAccount) && _zuora != null)
    {
      try
      {
        dynamic acc = await _zuora.GetAccountAsync(ExistingAccount); // implement in your wrapper: GET /v1/accounts/{key}
        if (acc != null && acc.success == true)
        {
          var c = acc.billToContact;
          FirstName = (string)c?.firstName ?? "";
          LastName  = (string)c?.lastName  ?? "";
          Address1  = (string)c?.address1  ?? "";
          Address2  = (string)c?.address2  ?? "";
          City      = (string)c?.city      ?? "";
          State     = (string)c?.state     ?? "";
          Postal    = (string)c?.zipCode   ?? "";
          Country   = (string)c?.country   ?? "";
          Email     = (string)c?.workEmail ?? "";
          Phone     = (string)c?.workPhone ?? "";
        }
      }
      catch { /* swallow and render empty form */ }
    }
  }

  protected async void SubmitBtn_Click(object sender, EventArgs e)
  {
    // read posted fields
    FirstName = Request.Form["firstName"];
    LastName  = Request.Form["lastName"];
    Address1  = Request.Form["address1"];
    Address2  = Request.Form["address2"];
    City      = Request.Form["city"];
    State     = Request.Form["state"];
    Postal    = Request.Form["postal"];
    Country   = Request.Form["country"];
    Email     = Request.Form["email"];
    Phone     = Request.Form["phone"];

    // TODO: your internal AVS/export validation here

    string accountNumber = ExistingAccount;
    try
    {
      if (!string.IsNullOrWhiteSpace(ExistingAccount))
      {
        // UPDATE existing account Bill-To
        var body = new {
          billToContact = new {
            firstName = FirstName, lastName = LastName,
            address1 = Address1, address2 = Address2, city = City, state = State,
            zipCode = Postal, country = Country,
            workEmail = Email, workPhone = Phone
          }
        };
        var res = await _zuora.UpdateAccountAsync(ExistingAccount, body, Guid.NewGuid().ToString());
        accountNumber = (string)(res?.basicInfo?.accountNumber ?? ExistingAccount);
      }
      else
      {
        // CREATE new account with Bill-To
        var body = new {
          basicInfo = new { name = $"{FirstName} {LastName}".Trim() },
          billToContact = new {
            firstName = FirstName, lastName = LastName,
            address1 = Address1, address2 = Address2, city = City, state = State,
            zipCode = Postal, country = Country,
            workEmail = Email, workPhone = Phone
          }
        };
        var res = await _zuora.CreateAccountAsync(body, Guid.NewGuid().ToString());
        accountNumber = (string)(res?.basicInfo?.accountNumber);
      }
    }
    catch (Exception ex)
    {
      // surface a simple message; you can log ex via your logger
      Response.StatusCode = 502;
      Response.Write("Account save failed.");
      Response.End();
      return;
    }

    // go to Step 2 Preview (stateless across regions)
    Response.Redirect($"/Checkout/Step2Preview.aspx?account={Uri.EscapeDataString(accountNumber)}&sku={Uri.EscapeDataString(Sku)}&qty={Quantity}");
  }
}
