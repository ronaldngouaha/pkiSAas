Service Tenants.Identity
Role: gestion des tenants, domaines et users.

Dev:
 - dotnet build src/Tenants.Identity
 - dotnet run --project src/Tenants.Identity
 - dotnet ef migrations add InitialCreate -p src/Tenants.Identity -s src/Tenants.Identity -o src/Tenants.Identity/Migrations
 - dotnet ef database update -p src/Tenants.Identity -s src/Tenants.Identity

Health: GET /health

Environment variables (auth and key management):
 - `JWT_ISSUER`: issuer for JWT access tokens
 - `JWT_AUDIENCE`: audience for JWT access tokens
 - `JWT_ACCESS_MINUTES`: access token lifetime in minutes (recommendation: `15`)
 - `JWT_REFRESH_DAYS`: refresh token lifetime in days
 - `JWT_PRIVATE_KEY_PEM`: development fallback only (do not use in production)
 - `VAULT_ADDR`: Vault endpoint for dev mode
 - `VAULT_TOKEN`: Vault token for dev mode
 - `SEED_SUPERADMIN`: set to `true` only during provisioning
 - `SEED_SUPERADMIN_EMAIL`: provisioning-only SuperAdmin email
 - `SEED_SUPERADMIN_PASSWORD`: provisioning-only SuperAdmin password

Provisioning notes:
 - Never commit real secrets in code, json, compose files, or scripts.
 - Use placeholders in repository files and inject values via secure secret stores.
 - In production, prefer Azure Key Vault HSM + managed identity, and run a secure provisioning script instead of env var seeding.

Security best practices (auth):
 - Use RSA keys stored in Key Vault/HSM; never store private keys in the repository.
 - Use short-lived access tokens (15 minutes) and refresh tokens with rotation.
 - Hash refresh tokens in database storage; rotate and revoke on use.
 - Use strong password hashing (BCrypt/Argon2).
 - Enforce MFA for SuperAdmin and TenantAdmin accounts.
 - Log authentication events to Audit.Events service.
 - Rate-limit login endpoints and keep account lockout protections enabled.
 - Use HTTPS everywhere and secure cookie flags if cookies are used.
 - Implement a JWKS endpoint or expose JWKS from the key provider so peer services can validate tokens.
 - Plan key rotation: publish new key with a new `kid`, keep previous public keys during token validation overlap.
 - Protect introspection endpoint; prefer short access tokens + refresh flow to reduce introspection frequency.

Domain validation notes:
 - DNS TXT validation uses `_acme-challenge.{domain}` and expects a TXT record that contains the generated token.
 - HTTP validation expects `http://{domain}/.well-known/acme-challenge/{token}` to return the token in the response body.
 - Validation tokens are purged after success and should never be exposed in public APIs after the domain is validated.
 - The background validation worker runs at a configurable interval via `DomainValidation:IntervalMinutes`.
 - Validate endpoints are rate-limited per domain via `DomainValidation:ValidateCooldownMinutes` and return `429` with `Retry-After`.
 - Wildcard domains and CNAME-based setups need explicit operational guidance before enabling automated validation.
 - For production issuance after validation, consider integrating a dedicated ACME client library instead of a custom flow.

Vault (dev) - JWT signing key seeding (no secrets committed):
 - Expected path: `secret/data/tenants-identity/jwt` (KV v2)
 - Expected keys: `privateKeyPem`, `keyId` (optional), `publicJwks` (optional)
 - Example (run locally, with your own key material):
	 `vault kv put secret/tenants-identity/jwt privateKeyPem=@/path/to/private_key.pem keyId=dev-k1`
