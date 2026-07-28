// Bicep skeleton for service: Pki.Core
// This template is intentionally incomplete and should be adapted before any deployment.

targetScope = 'resourceGroup'

@description('Azure location for the service resources.')
param location string = resourceGroup().location

@description('Environment suffix (dev/test/prod).')
param environmentName string = 'dev'

@description('Base name for resources in this service.')
param serviceName string = 'pki.core'

// TODO: Define compute resource (App Service / Container Apps / AKS workload)
// TODO: Define dedicated SQL database resources or references
// TODO: Add managed identity, diagnostics, and networking
// TODO: Export outputs needed by other modules

output status string = 'Skeleton for Pki.Core generated. Fill resources before deployment.'
