# PkiSaas IaC & Deployment

Ce dossier contient le socle d'infrastructure Azure et la chaîne CI/CD pour déployer les microservices PkiSaas.

## 1) Ressources créées par `iac/main.bicep`

- Azure Container Registry (ACR)
- Log Analytics Workspace
- Application Insights (workspace-based)
- Azure Key Vault (RBAC activé, purge protection activée)
- Azure SQL Server + base `TenantsDb`
- App Service Plan Linux
- 7 Web Apps Linux containerisées:
  - tenants-identity
  - pki-core
  - domain-validation
  - certificates-lifecycle
  - crl-publish
  - admin-pki
  - audit-events
- RBAC automatique:
  - `AcrPull` pour les identités managées des Web Apps
  - `Key Vault Secrets User` pour accès aux secrets

## 2) Pipeline GitHub Actions

### CI
- Fichier: `.github/workflows/ci.yml`
- Exécute:
  - restore
  - build
  - tests
  - validation syntaxe Bicep

### CD
- Fichier: `.github/workflows/deploy-azure.yml`
- Exécute:
  - validation build/tests/Bicep
  - login Azure OIDC
  - `what-if` Bicep
  - provisionnement infra bootstrap
  - build & push des 7 images Docker vers ACR
  - redéploiement Bicep avec le tag d'image final

## 3) Variables et secrets GitHub requis

### Repository Variables (`Settings > Secrets and variables > Actions > Variables`)
- `AZURE_RESOURCE_GROUP`
- `AZURE_LOCATION`
- `ACR_NAME`
- `KEYVAULT_NAME`
- `SQL_SERVER_NAME`

### Repository Secrets (`Settings > Secrets and variables > Actions > Secrets`)
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `SQL_ADMIN_LOGIN`
- `SQL_ADMIN_PASSWORD`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `AUTH_REFRESH_TOKEN_HASH_KEY`

## 4) Permissions Azure recommandées pour l'identité GitHub OIDC

Au minimum sur le Resource Group cible:
- Contributor
- User Access Administrator (pour créer les role assignments RBAC)

## 5) Déploiement manuel local (optionnel)

Exemple:

```bash
az deployment group what-if \
  --resource-group <rg> \
  --template-file iac/main.bicep \
  --parameters \
    location=<location> \
    environmentName=dev \
    projectName=pkisaas \
    acrName=<acrName> \
    keyVaultName=<kvName> \
    sqlServerName=<sqlServerName> \
    sqlAdminLogin=<sqlAdminLogin> \
    sqlAdminPassword=<sqlAdminPassword> \
    jwtIssuer=<issuer> \
    jwtAudience=<audience> \
    authRefreshTokenHashKey=<refreshKey> \
    imageTag=latest
```

## 6) Notes importantes

- Les secrets applicatifs (JWT + connexion SQL) sont stockés dans Key Vault puis injectés dans les Web Apps via références Key Vault.
- Le template active `httpsOnly` et TLS minimum 1.2.
- En production, adapter le SKU App Service, la stratégie SQL, les règles réseau et les diagnostics selon vos exigences de sécurité.
