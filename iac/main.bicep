targetScope = 'resourceGroup'

@description('Azure location for all resources.')
param location string = resourceGroup().location

@description('Environment name (dev/test/prod).')
param environmentName string = 'dev'

@description('Project prefix used to name resources.')
param projectName string = 'pkisaas'

@description('Azure Container Registry name (global unique, alphanumeric only).')
param acrName string

@description('Key Vault name (global unique).')
param keyVaultName string

@description('Azure SQL server name (global unique).')
param sqlServerName string

@description('SQL database name used by Tenants.Identity.')
param sqlDatabaseName string = 'TenantsDb'

@description('SQL administrator login.')
param sqlAdminLogin string

@secure()
@description('SQL administrator password.')
param sqlAdminPassword string

@secure()
@description('JWT issuer secret value stored in Key Vault and injected in apps.')
param jwtIssuer string

@secure()
@description('JWT audience secret value stored in Key Vault and injected in apps.')
param jwtAudience string

@secure()
@description('Refresh token hash key stored in Key Vault and injected in apps.')
param authRefreshTokenHashKey string

@description('Default container image tag applied to all services (for example latest or a commit SHA).')
param imageTag string = 'latest'

@description('Optional per-image tag override map. Example: { "tenants-identity": "abc123" }.')
param imageTags object = {}

@description('App Service Plan SKU name.')
param appServicePlanSku string = 'P1v3'

@description('Resource tags.')
param tags object = {}

var normalizedProject = toLower(replace(projectName, '.', '-'))

var services = [
	{
		imageName: 'tenants-identity'
		suffix: 'ti'
		healthPath: '/health'
	}
	{
		imageName: 'pki-core'
		suffix: 'pc'
		healthPath: '/health'
	}
	{
		imageName: 'domain-validation'
		suffix: 'dv'
		healthPath: '/health'
	}
	{
		imageName: 'certificates-lifecycle'
		suffix: 'cl'
		healthPath: '/health'
	}
	{
		imageName: 'crl-publish'
		suffix: 'cp'
		healthPath: '/health'
	}
	{
		imageName: 'admin-pki'
		suffix: 'ap'
		healthPath: '/health'
	}
	{
		imageName: 'audit-events'
		suffix: 'ae'
		healthPath: '/health'
	}
]

var appServicePlanName = '${normalizedProject}-${environmentName}-asp'
var workspaceName = '${normalizedProject}-${environmentName}-law'
var appInsightsName = '${normalizedProject}-${environmentName}-appi'

var sqlConnectionString = 'Server=tcp:${sqlServerName}.database.windows.net,1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
	name: workspaceName
	location: location
	tags: tags
	properties: {
		sku: {
			name: 'PerGB2018'
		}
		retentionInDays: 30
	}
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
	name: appInsightsName
	location: location
	kind: 'web'
	tags: tags
	properties: {
		Application_Type: 'web'
		WorkspaceResourceId: logAnalytics.id
	}
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
	name: acrName
	location: location
	tags: tags
	sku: {
		name: 'Standard'
	}
	properties: {
		adminUserEnabled: false
		policies: {
			quarantinePolicy: {
				status: 'disabled'
			}
			trustPolicy: {
				type: 'Notary'
				status: 'disabled'
			}
			retentionPolicy: {
				days: 7
				status: 'enabled'
			}
		}
	}
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
	name: keyVaultName
	location: location
	tags: tags
	properties: {
		enableRbacAuthorization: true
		enablePurgeProtection: true
		enabledForTemplateDeployment: true
		tenantId: subscription().tenantId
		sku: {
			family: 'A'
			name: 'standard'
		}
		softDeleteRetentionInDays: 90
		publicNetworkAccess: 'Enabled'
	}
}

resource jwtIssuerSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
	name: 'JwtIssuer'
	parent: keyVault
	properties: {
		value: jwtIssuer
	}
}

resource jwtAudienceSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
	name: 'JwtAudience'
	parent: keyVault
	properties: {
		value: jwtAudience
	}
}

resource refreshHashKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
	name: 'AuthRefreshTokenHashKey'
	parent: keyVault
	properties: {
		value: authRefreshTokenHashKey
	}
}

resource sqlConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
	name: 'TenantsIdentitySqlConnectionString'
	parent: keyVault
	properties: {
		value: sqlConnectionString
	}
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
	name: sqlServerName
	location: location
	tags: tags
	properties: {
		administratorLogin: sqlAdminLogin
		administratorLoginPassword: sqlAdminPassword
		version: '12.0'
		publicNetworkAccess: 'Enabled'
		minimalTlsVersion: '1.2'
	}
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
	name: sqlDatabaseName
	parent: sqlServer
	location: location
	sku: {
		name: 'Basic'
		tier: 'Basic'
	}
	properties: {
		collation: 'SQL_Latin1_General_CP1_CI_AS'
	}
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
	name: appServicePlanName
	location: location
	kind: 'linux'
	tags: tags
	sku: {
		name: appServicePlanSku
		tier: startsWith(appServicePlanSku, 'B') ? 'Basic' : (startsWith(appServicePlanSku, 'S') ? 'Standard' : 'PremiumV3')
		size: appServicePlanSku
		capacity: 1
	}
	properties: {
		reserved: true
	}
}

resource webApps 'Microsoft.Web/sites@2023-12-01' = [for (service, i) in services: {
	name: '${normalizedProject}-${service.suffix}-${environmentName}'
	location: location
	kind: 'app,linux,container'
	tags: union(tags, {
		service: service.imageName
		environment: environmentName
	})
	identity: {
		type: 'SystemAssigned'
	}
	properties: {
		serverFarmId: appServicePlan.id
		httpsOnly: true
		siteConfig: {
			linuxFxVersion: 'DOCKER|${acr.properties.loginServer}/${service.imageName}:${contains(imageTags, service.imageName) ? imageTags[service.imageName] : imageTag}'
			alwaysOn: true
			ftpsState: 'Disabled'
			minTlsVersion: '1.2'
			acrUseManagedIdentityCreds: true
			appSettings: [
				{
					name: 'ASPNETCORE_URLS'
					value: 'http://+:80'
				}
				{
					name: 'WEBSITES_PORT'
					value: '80'
				}
				{
					name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
					value: appInsights.properties.ConnectionString
				}
				{
					name: 'Jwt__Issuer'
					value: '@Microsoft.KeyVault(SecretUri=${jwtIssuerSecret.properties.secretUriWithVersion})'
				}
				{
					name: 'Jwt__Audience'
					value: '@Microsoft.KeyVault(SecretUri=${jwtAudienceSecret.properties.secretUriWithVersion})'
				}
				{
					name: 'Auth__RefreshTokenHashKey'
					value: '@Microsoft.KeyVault(SecretUri=${refreshHashKeySecret.properties.secretUriWithVersion})'
				}
				{
					name: 'ConnectionStrings__Default'
					value: '@Microsoft.KeyVault(SecretUri=${sqlConnectionStringSecret.properties.secretUriWithVersion})'
				}
				{
					name: 'SEED_SUPERADMIN'
					value: 'false'
				}
			]
			healthCheckPath: service.healthPath
		}
	}
}]

resource acrPullAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for (service, i) in services: {
	name: guid(acr.id, webApps[i].id, 'AcrPull')
	scope: acr
	properties: {
		principalId: webApps[i].identity.principalId
		principalType: 'ServicePrincipal'
		roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
	}
}]

resource keyVaultSecretUserAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for (service, i) in services: {
	name: guid(keyVault.id, webApps[i].id, 'KeyVaultSecretsUser')
	scope: keyVault
	properties: {
		principalId: webApps[i].identity.principalId
		principalType: 'ServicePrincipal'
		roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
	}
}]

output containerRegistryLoginServer string = acr.properties.loginServer
output webAppNames array = [for (service, i) in services: webApps[i].name]
output webAppUrls array = [for (service, i) in services: 'https://${webApps[i].properties.defaultHostName}']
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
