Feature.Payments v4 rebuild at 2025-08-13T01:01:57.763088

- v5: Catalog cached in Redis (10 minutes). Settings: Redis.Catalog.ConnectionString, Redis.Catalog.KeyPrefix, Redis.Catalog.TtlSeconds.

- v6: Admin endpoint POST /Admin/ClearCatalogCache protected by Redis.Catalog.AdminSecret; clears keys with prefix `${KeyPrefix}:*`.
