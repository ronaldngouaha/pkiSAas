using './main.bicep'

param location = 'westeurope'
param environmentName = 'dev'
param projectName = 'pkisaas'

// Ces noms doivent etre uniques globalement
param acrName = 'pkisaasdevacr001'
param keyVaultName = 'pkisaas-dev-kv-001'
param sqlServerName = 'pkisaas-dev-sql-001'

// Parametres sensibles: fournis par pipeline via secrets
// - sqlAdminLogin
// - sqlAdminPassword
// - jwtIssuer
// - jwtAudience
// - authRefreshTokenHashKey

param imageTag = 'latest'
