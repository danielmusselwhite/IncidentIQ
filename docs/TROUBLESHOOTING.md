# Troubleshooting

Common IncidentIQ development gotchas. For full local setup, see [Development](./DEVELOPMENT.md).

## Scoped service used by Worker singleton

Cannot consume scoped service 'AnalyseIncidentHandler' from singleton 'IHostedService'

- Register AnalyseIncidentHandler as scoped.
- Resolve it from a new DI scope for each Service Bus message.

## Local Worker tries Managed Identity

ManagedIdentityCredential authentication failed ... 169.254.169.254

- Ensure Docker sets DOTNET_ENVIRONMENT=Development.
- Use the development AI analyzer locally.
- Ensure COSMOS_EMULATOR_KEY is set in .env and passed as Cosmos__Key.
- See [Development](./DEVELOPMENT.md).

## Service Bus emulator will not start

Connection refused
Name or service not known
Login failed for user 'sa'

- Check the Service Bus emulator and SQL container logs.
- Ensure .env contains a valid SERVICEBUS_SQL_PASSWORD and both containers use the same value.
- If the password changed, delete the servicebus-sql-data volume and recreate the containers.

## SQL password rejected

Password validation failed. The password must be at least 8 characters.

- Replace the placeholder password in .env with a strong local password.
- Recreate the Service Bus SQL volume afterwards.

## Cosmos container has the wrong partition key

Partition key path '/incidentId' does not match the existing container

- The Incidents container must use /incidentId.
- Delete/recreate the local Incidents container if an older /id version still exists.

## Docker Compose ignores launchSettings environment

Worker environment: Production

- launchSettings.json does not configure Docker Compose containers.
- Set DOTNET_ENVIRONMENT=Development in docker-compose.override.yml.

## Local environment variables are still placeholders

COSMOS_EMULATOR_KEY=<COSMOS_EMULATOR_KEY>
SERVICEBUS_SQL_PASSWORD=<LOCAL_SQL_PASSWORD>

- Replace placeholders in .env with real local values.
- See [Development](./DEVELOPMENT.md).