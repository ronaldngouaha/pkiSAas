// Main Bicep skeleton for PKI SaaS microservices deployment.
// This file is intentionally non-deployable by default.
// Fill parameters, modules, identities, networking, and policy assignments before deployment.

targetScope = 'resourceGroup'

@description('Azure location for all resources.')
param location string = resourceGroup().location

@description('Environment name (dev/test/prod).')
param environmentName string = 'dev'

// TODO: Add shared infrastructure modules (ACR, Key Vault, Service Bus/Rabbit replacement, App Insights, etc.)
// TODO: Add each service module reference (Tenants.Identity, Pki.Core, Domain.Validation, Certificates.Lifecycle, Crl.Publish, Admin.Pki, Audit.Events)

output scaffold string = 'PkiSaas IaC skeleton generated. Add resources before deployment.'
