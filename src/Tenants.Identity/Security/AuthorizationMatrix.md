# Matrice d'autorisation (route -> policy)

## Principes
- TenantScope: le claim tid du token doit correspondre au tenant de la route.
- RequireMfa: claim amr=mfa ou mfa=true pour les actions sensibles.
- OwnResource: le claim sub doit correspondre au userId de la route.
- ReadOnly: uniquement GET/HEAD/OPTIONS.

## Endpoints existants

| Route | Methode | Policy | Notes |
|---|---|---|---|
| /api/v1/auth/login | POST | AllowAnonymous | Auth locale par credentials |
| /api/v1/auth/refresh | POST | AllowAnonymous | Renouvellement via refresh token |
| /api/v1/auth/revoke | POST | Authorize | Token utilisateur requis |
| /api/v1/auth/register | POST | Authorize + controle role/scope | SuperAdmin global, sinon TenantAdmin/UserManager du meme tenant |
| /api/v1/auth/seed-superadmin | POST | AllowAnonymous conditionnel | Si un SuperAdmin existe: SuperAdmin requis |
| /api/v1/auth/me | GET | Authorize | Lecture claims utilisateur courant |
| /api/v1/auth/introspect | POST | AllowAnonymous | Endpoint debug/introspection |
| /api/v1/tenants/* | * | SuperAdminOnly | Gouvernance globale tenant |
| /api/v1/superadmins/* | * | SuperAdminOnly (sauf create bootstrap) | Bootstrap initial sans bearer si aucun SuperAdmin |
| /api/v1/tenants/{tenantId}/users | GET/POST/PUT/PATCH/POST | TenantAdminOrUserManagerPolicy | Gestion utilisateurs tenant |
| /api/v1/tenants/{tenantId}/users/{userId}/role | PATCH | TenantAdminSensitivePolicy | MFA requis pour action sensible |
| /api/v1/tenants/{tenantId}/domains/* | POST | TenantAdminPolicy | Administration domaines tenant |
| /api/v1/mfa/{userId}/* | POST/GET | EndUserOwnResourcePolicy | Utilisateur sur sa propre ressource |
| /api/v1/resolve | GET | Public | Resolution host -> tenant |

## Endpoints de reference role-based

Le controller SecureExampleController expose un endpoint de reference pour chaque policy:
- TenantOwnerPolicy
- TenantAdminPolicy
- TenantAdminSensitivePolicy
- SecurityAdminPolicy
- AppAdminPolicy
- UserManagerPolicy
- SupportAgentPolicy
- EndUserOwnResourcePolicy
- ServiceAccountPolicy
- ViewerPolicy
- ReadOnlyAdminPolicy
- SuperAdminOnly
