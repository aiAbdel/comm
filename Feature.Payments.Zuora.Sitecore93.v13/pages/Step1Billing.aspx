<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Step1Billing.aspx.cs" Inherits="Checkout_Step1Billing" %>
<!doctype html>
<html>
<head runat="server">
  <meta charset="utf-8" />
  <title>Billing address</title>
  <style>
    body{font-family:Inter,system-ui,Arial,sans-serif;margin:24px}
    .grid{display:grid;grid-template-columns:1fr 1fr;gap:12px;max-width:760px}
    .full{grid-column:1 / span 2}
    .cta{padding:10px 14px;border:1px solid #888;border-radius:8px;background:#f7f7f7}
    .muted{color:#666}
  </style>
</head>
<body>
  <h1>Billing address</h1>
  <div class="muted">
    <strong>SKU:</strong> <%= Sku %> &nbsp;&nbsp; <strong>Qty:</strong> <%= Quantity %>
    <% if (!string.IsNullOrWhiteSpace(ExistingAccount)) { %>
      &nbsp;&nbsp; <strong>Account:</strong> <%= ExistingAccount %>
    <% } %>
  </div>

  <form id="f" runat="server">
    <input type="hidden" name="sku" value="<%= Sku %>" />
    <input type="hidden" name="qty" value="<%= Quantity %>" />
    <input type="hidden" name="account" value="<%= ExistingAccount %>" />

    <div class="grid">
      <div><label>First name<br/><input name="firstName" value="<%= FirstName %>" required /></label></div>
      <div><label>Last name<br/><input name="lastName" value="<%= LastName %>" required /></label></div>

      <div class="full"><label>Address 1<br/><input name="address1" value="<%= Address1 %>" required style="width:100%" /></label></div>
      <div class="full"><label>Address 2<br/><input name="address2" value="<%= Address2 %>" style="width:100%" /></label></div>

      <div><label>City<br/><input name="city" value="<%= City %>" required /></label></div>
      <div><label>State/Province<br/><input name="state" value="<%= State %>" /></label></div>
      <div><label>Postal code<br/><input name="postal" value="<%= Postal %>" required /></label></div>
      <div><label>Country<br/><input name="country" value="<%= Country %>" required /></label></div>

      <div><label>Email<br/><input type="email" name="email" value="<%= Email %>" required /></label></div>
      <div><label>Phone<br/><input name="phone" value="<%= Phone %>" /></label></div>

      <div class="full">
        <asp:Button runat="server" Text="Continue" CssClass="cta" OnClick="SubmitBtn_Click" />
      </div>
    </div>
  </form>
</body>
</html>
