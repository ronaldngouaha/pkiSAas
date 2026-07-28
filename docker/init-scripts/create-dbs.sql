/*
  Local development SQL bootstrap script.
  Replace placeholders __PKI_APP_LOGIN__ and __PKI_APP_PASSWORD__ before execution
  if you want a dedicated application login.
*/

IF DB_ID('TenantsDb') IS NULL CREATE DATABASE [TenantsDb];
IF DB_ID('PkiCoreDb') IS NULL CREATE DATABASE [PkiCoreDb];
IF DB_ID('DomainDb') IS NULL CREATE DATABASE [DomainDb];
IF DB_ID('CertificatesDb') IS NULL CREATE DATABASE [CertificatesDb];
IF DB_ID('CrlDb') IS NULL CREATE DATABASE [CrlDb];
IF DB_ID('AdminDb') IS NULL CREATE DATABASE [AdminDb];
IF DB_ID('AuditDb') IS NULL CREATE DATABASE [AuditDb];
GO

IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = '__PKI_APP_LOGIN__')
BEGIN
    CREATE LOGIN [__PKI_APP_LOGIN__] WITH PASSWORD = '__PKI_APP_PASSWORD__', CHECK_POLICY = OFF;
END
GO

USE [TenantsDb]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '__PKI_APP_LOGIN__') CREATE USER [__PKI_APP_LOGIN__] FOR LOGIN [__PKI_APP_LOGIN__]; ALTER ROLE db_owner ADD MEMBER [__PKI_APP_LOGIN__];
USE [PkiCoreDb]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '__PKI_APP_LOGIN__') CREATE USER [__PKI_APP_LOGIN__] FOR LOGIN [__PKI_APP_LOGIN__]; ALTER ROLE db_owner ADD MEMBER [__PKI_APP_LOGIN__];
USE [DomainDb]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '__PKI_APP_LOGIN__') CREATE USER [__PKI_APP_LOGIN__] FOR LOGIN [__PKI_APP_LOGIN__]; ALTER ROLE db_owner ADD MEMBER [__PKI_APP_LOGIN__];
USE [CertificatesDb]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '__PKI_APP_LOGIN__') CREATE USER [__PKI_APP_LOGIN__] FOR LOGIN [__PKI_APP_LOGIN__]; ALTER ROLE db_owner ADD MEMBER [__PKI_APP_LOGIN__];
USE [CrlDb]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '__PKI_APP_LOGIN__') CREATE USER [__PKI_APP_LOGIN__] FOR LOGIN [__PKI_APP_LOGIN__]; ALTER ROLE db_owner ADD MEMBER [__PKI_APP_LOGIN__];
USE [AdminDb]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '__PKI_APP_LOGIN__') CREATE USER [__PKI_APP_LOGIN__] FOR LOGIN [__PKI_APP_LOGIN__]; ALTER ROLE db_owner ADD MEMBER [__PKI_APP_LOGIN__];
USE [AuditDb]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '__PKI_APP_LOGIN__') CREATE USER [__PKI_APP_LOGIN__] FOR LOGIN [__PKI_APP_LOGIN__]; ALTER ROLE db_owner ADD MEMBER [__PKI_APP_LOGIN__];
GO
