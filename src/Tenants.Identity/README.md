Service Tenants.Identity
Role: gestion des tenants, domaines et users.

Dev:
 - dotnet build src/Tenants.Identity
 - dotnet run --project src/Tenants.Identity
 - dotnet ef migrations add InitialCreate -p src/Tenants.Identity -s src/Tenants.Identity -o src/Tenants.Identity/Migrations
 - dotnet ef database update -p src/Tenants.Identity -s src/Tenants.Identity

Health: GET /health
