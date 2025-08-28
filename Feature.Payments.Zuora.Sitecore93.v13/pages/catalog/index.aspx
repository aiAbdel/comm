<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="Catalog_Index" %>
<!doctype html>
<html>
<head runat="server">
  <meta charset="utf-8" />
  <title>Choose your plan</title>
  <link rel="stylesheet" href="/assets/css/main.css" />
  <link rel="stylesheet" href="/assets/css/styles.css" />
  <style>
    .card{background:#fff;border:var(--cardBorder,1px solid #eaeaea);border-radius:12px;
          box-shadow:var(--cardBoxShadow,0 2px 8px rgba(0,0,0,.1));padding:16px}
    .sku{font-size:12px;color:#777}
    .name{font-size:20px;font-weight:600;margin:6px 0}
    .price{margin:6px 0}
    .per{color:#777;font-size:12px}
  </style>
</head>
<body>
  <main class="content">
    <div class="grid grid--margin-top">
      <div class="grid__col grid__col--12">
        <h1>Choose your plan</h1>
        <p class="large">Pick <strong>Premium</strong>, <strong>Business</strong>, or <strong>Teams</strong>. You can change quantity for per-user plans.</p>
      </div>
    </div>

    <!-- Existing account (optional) -->
    <div class="grid grid--margin-top">
      <div class="grid__col grid__col--8 grid__col--left">
        <label class="small-eyebrow">Existing Zuora account (optional)</label>
        <input type="text" id="existingAccount" placeholder="A00000001 or user@example.com" />
        <p class="small" style="color:var(--content-secondary-positive)">If provided, we’ll prefill billing and update that account.</p>
      </div>
    </div>

    <form id="toStep1" method="get" action="/Checkout/Step1Billing.aspx">
      <input type="hidden" name="sku" id="fSku" />
      <input type="hidden" name="qty" id="fQty" />
      <input type="hidden" name="account" id="fAccount" />
    </form>

    <!-- Cards -->
    <div class="grid grid--margin-top grid--align-top">
      <% foreach (var p in Items) { %>
        <div class="grid__col grid__col--4">
          <div class="card">
            <div class="sku"><%= p.Sku %></div>
            <div class="name"><%= p.Name %></div>
            <div class="price"><%= p.Currency %> <%= p.Price.ToString("0.00") %>/yr <% if (p.PerUser) { %><span class="per">/ user</span><% } %></div>
            <div>
              <% if (p.PerUser) { %>
                <label class="small-eyebrow">Users</label>
                <input type="number" min="1" id="qty-<%= p.Sku %>" value="<%= p.DefaultQty %>" />
              <% } else { %>
                <input type="hidden" id="qty-<%= p.Sku %>" value="1" />
              <% } %>
            </div>
            <div style="margin-top:12px">
              <button type="button" class="lp-button lp-button__red lp-button--price"
                      onclick="goToStep1('<%= p.Sku %>')">Select</button>
            </div>
          </div>
        </div>
      <% } %>
    </div>
  </main>

  <script>
    function goToStep1(sku){
      var qty = document.getElementById('qty-' + sku).value || 1;
      document.getElementById('fSku').value = sku;
      document.getElementById('fQty').value = qty;
      document.getElementById('fAccount').value = document.getElementById('existingAccount').value || '';
      document.getElementById('toStep1').submit();
    }
  </script>
</body>
</html>
