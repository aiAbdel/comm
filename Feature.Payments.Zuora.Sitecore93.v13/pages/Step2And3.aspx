<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Step2And3.aspx.cs" Inherits="Website.Checkout.Step2And3" %>
<!doctype html>
<html>
<head runat="server">
  <meta charset="utf-8" />
  <title>Checkout</title>

  <!-- Use your uploaded CSS bundles -->
  <link rel="stylesheet" href="/assets/css/main.css" />
  <link rel="stylesheet" href="/assets/css/styles.css" />

  <style>
    /* Tiny page-scoped tweaks only */
    .card { background:#fff; border:1px solid var(--cardBorder,#eaeaea); border-radius:12px; box-shadow:var(--cardBoxShadow,0 2px 8px rgba(0,0,0,.1)); padding:16px; }
    .muted { color: var(--content-secondary-positive, rgba(5,6,6,.6)); }
    .table { width:100%; border-collapse:collapse }
    .table th,.table td { padding:8px 6px; border-bottom:1px solid #edeef0 }
    .ta-right { text-align:right }
    .totals td { font-weight:700 }
    @media(max-width:961px){ .stack-mobile { margin-top:16px; } }
  </style>
</head>
<body>
  <main class="content">
    <div class="grid grid--margin-top">
      <div class="grid__col grid__col--12">
        <h1>Checkout</h1>
        <p class="muted">
          <strong>Account:</strong> <%= AccountNumber %> &nbsp;|&nbsp;
          <strong>Plan:</strong> <%= Sku %> &nbsp;|&nbsp;
          <strong>Qty:</strong> <%= Quantity %>
        </p>
      </div>
    </div>

    <!-- Two columns -->
    <div class="grid grid--margin-top grid--align-top grid--no-wrap grid--reverse-mobile">
      <!-- LEFT: Payment -->
      <section class="grid__col grid__col--6 grid__col--left">
        <div class="card">
          <h3>Payment</h3>
          <div id="zuora-payment-form"></div>
          <div id="receipt" class="grid--margin-top"></div>
        </div>
      </section>

      <!-- RIGHT: Preview -->
      <aside class="grid__col grid__col--6 grid__col--right stack-mobile">
        <div class="card">
          <h3>Order Preview</h3>
          <p id="currencyLine" class="muted"></p>
          <table class="table">
            <thead>
              <tr><th>Item</th><th class="ta-right">Qty</th><th class="ta-right">Amount</th></tr>
            </thead>
            <tbody id="chargesBody"><tr><td colspan="3" class="muted">Loading…</td></tr></tbody>
            <tfoot>
              <tr><td>Subtotal</td><td></td><td id="subtotal" class="ta-right"></td></tr>
              <tr><td>Tax</td><td></td><td id="tax" class="ta-right"></td></tr>
              <tr class="totals"><td>Total due today</td><td></td><td id="total" class="ta-right"></td></tr>
            </tfoot>
          </table>
          <!-- Optional: a manual continue button if you want (the hosted form already renders its own) -->
          <!-- <button class="lp-button lp-button__red lp-button--price" type="button">Pay now</button> -->
        </div>
      </aside>
    </div>
  </main>

  <!-- Zuora Payment Form v3 (Hosted-style) -->
  <script src="https://js.zuora.com/payment/v3/zuora.js"></script>
  <script>
  (function(){
    // ---- inputs from server ----
    var accountNumber = "<%= AccountNumber %>";
    var accountId     = "<%= AccountId %>";
    var publishableKey= "<%= PublishableKey %>";
    var environment   = "<%= EnvironmentName %>"; // 'sandbox' | 'production'
    var sku           = "<%= Sku %>";
    var qty           = <%= Quantity %>;

    // ---- state filled by preview ----
    var currency, amount, ratePlanId, chargeId;

    // ---- helpers to your backend ----
    async function loadPreview(){
      const resp = await fetch('/Orders/Preview', {
        method:'POST',
        headers:{'Content-Type':'application/json'},
        body: JSON.stringify({ accountNumber: accountNumber, sku: sku, quantity: qty })
      });
      if (!resp.ok) throw new Error('Preview failed');
      return resp.json();
    }

    async function createPaymentSession(){
      const resp = await fetch('/Payments/CreateSession', {
        method:'POST',
        headers:{'Content-Type':'application/json'},
        body: JSON.stringify({
          accountId: accountId || undefined,
          accountNumber: accountNumber,
          currency: currency,
          amount: amount,              // must be > 0 for most verifications
          processPayment: false,
          storePaymentMethod: true
        })
      });
      if (!resp.ok) throw new Error('Could not start payment session');
      return resp.json();
    }

    async function finalizeCheckout(paymentMethodId){
      const resp = await fetch('/Payments/Finalize', {
        method:'POST',
        headers:{'Content-Type':'application/json'},
        body: JSON.stringify({
          accountNumber: accountNumber,
          paymentMethodId: paymentMethodId,
          productRatePlanId: ratePlanId,
          productRatePlanChargeId: chargeId,
          quantity: qty
        })
      });
      if (!resp.ok) throw new Error(await resp.text() || 'Finalize failed');
      return resp.json();
    }

    // ---- renderers ----
    function renderPreview(data){
      currency   = data.currency || 'USD';
      amount     = Number(data.amount || 0);
      ratePlanId = data.ratePlanId || data.ratePlan?.id || '';
      chargeId   = data.chargeId || data.ratePlanChargeId || '';

      document.getElementById('currencyLine').innerText = 'All amounts in ' + currency;

      var tbody = document.getElementById('chargesBody');
      tbody.innerHTML = '';
      (data.chargeLines || []).forEach(function(l){
        var tr = document.createElement('tr');
        tr.innerHTML =
          '<td>' + (l.name || '') + '</td>' +
          '<td class="ta-right">' + (l.quantity != null ? l.quantity : '') + '</td>' +
          '<td class="ta-right">' + Number(l.amount || 0).toFixed(2) + '</td>';
        tbody.appendChild(tr);
      });

      document.getElementById('subtotal').innerText = Number(data.subtotal || 0).toFixed(2);
      document.getElementById('tax').innerText      = Number(data.taxTotal || 0).toFixed(2);
      document.getElementById('total').innerText    = Number(data.amount || 0).toFixed(2);
    }

    function renderReceipt(done){
      var r = document.getElementById('receipt');
      r.innerHTML = '<div class="grid grid--margin-top">'
        + '<div class="grid__col grid__col--full">'
        +   '<p><strong>Order:</strong> ' + (done.orderNumber || '') + '</p>'
        +   '<p><strong>Subscription:</strong> ' + (done.subscriptionNumber || '') + '</p>'
        +   '<p><strong>Invoice:</strong> ' + (done.invoiceNumber || '') + '</p>'
        +   (done.paymentId ? '<p><strong>PaymentId:</strong> ' + done.paymentId + '</p>' : '')
        + '</div></div>';
    }

    // ---- bootstrap: load preview, then init Zuora form ----
    (async function init(){
      try {
        const preview = await loadPreview();
        renderPreview(preview);

        // Hosted-style init per Zuora docs
        const zuora = Zuora(publishableKey, { environment: environment });

        const configuration = {
          locale: "en",
          region: "US",
          currency: currency,
          amount: amount.toFixed ? amount.toFixed(2) : amount,
          createPaymentSession: () => new Promise((resolve, reject) => {
            createPaymentSession().then(resolve).catch(reject);
          }),
          onComplete: async (result) => {
            try {
              if (!result || !result.success) {
                alert((result && result.error && result.error.message) || 'Saving payment method failed');
                return;
              }
              const done = await finalizeCheckout(result.paymentMethodId);
              if (done && done.success) {
                renderReceipt(done);
                // Optionally redirect:
                // window.location = '/Checkout/Success.aspx?order=' + encodeURIComponent(done.orderNumber || '');
              } else {
                alert('Finalize did not return success.');
              }
            } catch (e) {
              console.error(e);
              alert(e.message || 'Unexpected error after saving payment method');
            }
          }
        };

        zuora.createPaymentForm(configuration)
          .then(form => form.mount("#zuora-payment-form"))
          .catch(e => { console.error(e); alert('Failed to initialize payment form'); });

      } catch (e) {
        console.error(e);
        document.getElementById('chargesBody').innerHTML = '<tr><td colspan="3" class="muted">Failed to load preview.</td></tr>';
        alert(e.message || 'Could not load order preview');
      }
    })();
  })();
  </script>
</body>
</html>
