Feature.Payments v13 full build (2025-08-15T15:29:48.138135)
- CatalogController + rich Catalog view (cards for PREMIUM/BUSINESS/TEAMS from Zuora Catalog)
- Checkout Step2 preview page (AJAX -> /Orders/PreviewTotal) + totals display
- Step3 Payment uses real RatePlanId/ChargeId and Places Order (collect:true)
- Cookie-based idempotency service (HttpOnly, Secure)
- Redis-backed Catalog cache (10 minutes)
