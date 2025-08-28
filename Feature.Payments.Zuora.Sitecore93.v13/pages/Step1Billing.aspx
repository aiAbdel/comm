<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Step1Billing.aspx.cs" Inherits="Checkout_Step1Billing" %>
<!doctype html>
<html>
<head runat="server">
  <meta charset="utf-8" />
  <title>Billing address</title>
  <link rel="stylesheet" href="/assets/css/main.css" />
  <link rel="stylesheet" href="/assets/css/styles.css" />
</head>
<body>
  <main class="content">
    <div class="grid grid--margin-top">
      <div class="grid__col grid__col--12">
        <h1>Billing address</h1>
        <p class="body">
          <strong>SKU:</strong> <%= Sku %> &nbsp;&nbsp; <strong>Qty:</strong> <%= Quantity %>
          <% if (!string.IsNullOrWhiteSpace(ExistingAccount)) { %>
            &nbsp;&nbsp; <strong>Account:</strong> <%= ExistingAccount %>
          <% } %>
        </p>
      </div>
    </div>

    <form id="f" runat="server">
      <input type="hidden" name="sku" value="<%= Sku %>" />
      <input type="hidden" name="qty" value="<%= Quantity %>" />
      <input type="hidden" name="account" value="<%= ExistingAccount %>" />

      <div class="grid grid--margin-top grid--align-top">
        <div class="grid__col grid__col--6 grid__col--left">
          <label class="small-eyebrow">First name</label>
          <input name="firstName" value="<%= FirstName %>" required />

          <label class="small-eyebrow" style="margin-top:12px">Address 1</label>
          <input name="address1" value="<%= Address1 %>" required />

          <label class="small-eyebrow" style="margin-top:12px">City</label>
          <input name="city" value="<%= City %>" required />

          <label class="small-eyebrow" style="margin-top:12px">Postal code</label>
          <input name="postal" value="<%= Postal %>" required />

          <label class="small-eyebrow" style="margin-top:12px">Email</label>
          <input type="email" name="email" value="<%= Email %>" required />
        </div>

        <div class="grid__col grid__col--6 grid__col--right">
          <label class="small-eyebrow">Last name</label>
          <input name="lastName" value="<%= LastName %>" required />

          <label class="small-eyebrow" style="margin-top:12px">Address 2</label>
          <input name="address2" value="<%= Address2 %>" />

          <label class="small-eyebrow" style="margin-top:12px">State / Province</label>
          <input name="state" value="<%= State %>" />

          <label class="small-eyebrow" style="margin-top:12px">Country</label>
          <input name="country" value="<%= Country %>" required />

          <label class="small-eyebrow" style="margin-top:12px">Phone</label>
          <input name="phone" value="<%= Phone %>" />
        </div>
      </div>

      <div class="grid grid--margin-top">
        <div class="grid__col grid__col--6 grid__col--left">
          <asp:Button runat="server" CssClass="lp-button lp-button__red" Text="Continue to review & pay"
                      OnClick="ContinueBtn_Click" />
        </div>
      </div>
    </form>
  </main>
</body>
</html>
