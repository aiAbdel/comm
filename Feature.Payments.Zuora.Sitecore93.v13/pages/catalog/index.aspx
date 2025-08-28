<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="Catalog_Index" %>
<!doctype html>
<html>
<head runat="server">
  <meta charset="utf-8" />
  <title>Choose your plan</title>
  <style>
    body{font-family:Inter,system-ui,Arial,sans-serif;margin:24px}
    .lead{color:#444}
    .grid{display:flex;gap:16px;flex-wrap:wrap}
    .card{border:1px solid #ddd;border-radius:12px;padding:16px;width:260px}
    .sku{font-size:12px;color:#777}
    .name{font-size:20px;font-weight:600;margin:6px 0}
    .price{margin:6px 0}
    .per{color:#777;font-size:12px}
    .cta{padding:8px 12px;border:1px solid #888;border-radius:8px;background:#f7f7f7;cursor:pointer}
    .panel{margin:14px 0;padding:12px;border:1px solid #eee;border-radius:10px}
  </style>
</head>
<body>
  <h1>Choose your plan</h1>
  <p class="lead">Pick <strong>Premium</strong>, <strong>Business</strong>, or <strong>Teams</strong>. You can change quantity for per-user plans.</p>

  <!-- One global form to carry account/sku/qty -->
  <div class="panel">
    <label>Have a Zuora account? (optional)</label><br/>
    <input type="text" id="existingAccount" placeholder="A00000001 or user@example.com" style="width:320px" />
  </div>

  <form id="accForm" method="get" action="/Checkout/Step1Billing.aspx">
    <input type="hidden" name="sku" id="accSku" />
    <input type="hidden" name="qty" id="accQty" />
    <input type="hidden" name="account" id="accAccount" />
  </form>

  <div class="grid">
    <% foreach (var p in Items) { %>
      <div class="card">
        <div class="sku"><%= p.Sku %></div>
        <div class="name"><%= p.Name %></div>
        <div class="price"><%= p.Currency %> <%= p.Price.ToString("0.00") %>/yr <% if (p.PerUser) { %><span class="per">/ user</span><% } %></div>
        <div class="qty">
          <% if (p.PerUser) { %>
            <label>Users:&nbsp;<input type="number" min="1" id="qty-<%= p.Sku %>" value="<%= p.DefaultQty %>" /></label>
          <% } else { %>
            <input type="hidden" id="qty-<%= p.Sku %>" value="1" />
          <% } %>
        </div>
        <button type="button" class="cta" onclick="goToCheckout('<%= p.Sku %>')">Select</button>
      </div>
    <% } %>
  </div>

  <script>
    function goToCheckout(sku){
      var qty = document.getElementById('qty-' + sku).value || 1;
      document.getElementById('accSku').value = sku;
      document.getElementById('accQty').value = qty;
      document.getElementById('accAccount').value = document.getElementById('existingAccount').value;
      document.getElementById('accForm').submit();
    }
  </script>
</body>
</html>
