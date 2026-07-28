# PkiSaas Local Development

1. Copier docker/.env.example vers docker/.env.
2. Renseigner MSSQL_SA_PASSWORD et VAULT_DEV_ROOT_TOKEN_ID.
3. Executer:

```bash
docker compose --env-file docker/.env -f docker-compose.local.yml up --build
```
