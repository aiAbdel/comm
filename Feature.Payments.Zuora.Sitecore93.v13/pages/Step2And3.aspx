<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Step2And3.aspx.cs" Inherits="Website.Checkout.Step2And3" %>
<!doctype html>
<html>
<head runat="server">
  <meta charset="utf-8" />
  <title>Checkout</title>
  <style>
    body{font-family:Inter,system-ui,Arial,sans-serif;margin:24px}
    .wrap{display:grid;grid-template-columns:1fr 1fr;gap:24px;align-items:start}
    .panel{border:1px solid #ddd;border-radius:12px;padding:16px}
    .header{margin-bottom:8px}
    .table{width:100%;border-collapse:collapse}
    .table th,.table td{padding:6px 4px;border-bottom:1px solid #eee}
    .ta-right{text-align:right}
    .muted{color:#666}
    .totals td{font-weight:700}
    #receipt{margin-top:12px}
  </style>
</head>
<body>
  <h1>Checkout</h1>
  <div class="muted">
    <strong>Account:</strong> <%= AccountNumber %> &nbsp;|&nbsp;
    <strong>Plan:</strong> <%= Sku %> &nbsp;|&nbsp;
    <strong>Qty:</strong> <%= Quantity %>
  </div>

  <div class="wrap">
    <!-- LEFT: Payment Form -->
    <div class="panel">
      <div class="header"><h3 style="margin:0">Payment</h3></div>
      <div id="zuora-payment-form"></div>
      <div id="receipt"></div>
    </div>

    <!-- RIGHT: Order Preview -->
    <div class="panel">
      <div class="header"><h3 style="margin:0">Order Preview</h3></div>
      <div id="currencyLine" class="muted"></div>
      <table class="table">
        <thead><tr><th>Item</th><th class="ta-right">Qty</th><th class="ta-right">Amount</th></tr></thead>
        <tbody id="chargesBody"><tr><td colspan="3" class="muted">Loading…</td></tr></tbody>
        <tfoot>
          <tr><td>Subtotal</td><td></td><td id="subtotal" class="ta-right"></td></tr>
          <tr><td>Tax</td><td></td><td id="tax" class="ta-right"></td></tr>
          <tr class="totals"><td>Total due today</td><td></td><td id="total" class="ta-right"></td></tr>
        </tfoot>
      </table>
    </div>
  </div>

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
          accountId: accountId || undefined,   // if your server accepts number and resolves to id internally, you can also send accountNumber
          accountNumber: accountNumber,        // send both if your server supports it, else remove one
          currency: currency,
          amount: amount,                      // must be > 0 if gateway verifies on save
          processPayment: false,
          storePaymentMethod: true
        })
      });
      if (!resp.ok) throw new Error('Could not start payment session');
      return resp.json(); // token string (or {token})
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
      return resp.json(); // { success, orderNumber, subscriptionNumber, invoiceNumber, paymentId }
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
      r.innerHTML = '<div>'
        + '<div><strong>Order:</strong> ' + (done.orderNumber || '') + '</div>'
        + '<div><strong>Subscription:</strong> ' + (done.subscriptionNumber || '') + '</div>'
        + '<div><strong>Invoice:</strong> ' + (done.invoiceNumber || '') + '</div>'
        + (done.paymentId ? '<div><strong>PaymentId:</strong> ' + done.paymentId + '</div>' : '')
        + '</div>';
    }

    // ---- bootstrap: load preview, then init Zuora form ----
    (async function init(){
      try {
        const preview = await loadPreview();
        renderPreview(preview);

        // Hosted-style init exactly like the doc you shared
        const zuora = Zuora(publishableKey, { environment: environment });

        const configuration = {
          locale: "en",
          region: "US",
          currency: currency,
          amount: amount.toFixed ? amount.toFixed(2) : amount,
          // SDK will call this when the customer clicks "Pay" in the hosted form
          createPaymentSession: () => new Promise((resolve, reject) => {
            createPaymentSession().then(resolve).catch(reject);
          }),
          // Called after Zuora has saved the PM (no capture since processPayment:false)
          onComplete: async (result) => {
            try {
              if (!result || !result.success) {
                alert((result && result.error && result.error.message) || 'Saving payment method failed');
                return;
              }
              const done = await finalizeCheckout(result.paymentMethodId);
              if (done && done.success) {
                renderReceipt(done);
                // optional redirect:
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

        // Create & mount the hosted payment form into left column
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
