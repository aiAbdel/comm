Feature.Payments v8 full build (2025-08-14T18:46:38.601014)
- Routing-driven, charge-later flow
- Type-aware UpdatePaymentMethodAddress
- Orders collect:true (atomic)

- v10: UI persists/reuses Idempotency-Key in sessionStorage tied to (account + plan + qty); removes on success.

- v11: Backend-managed Idempotency-Key via RedisIdempotencyService; UI no longer generates/handles keys.

- v12: Switched idempotency storage to CookieIdempotencyService (HttpOnly, Secure). Redis implementation remains available but is not registered.
