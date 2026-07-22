# Job retrieval scheduler

`CreateJobMatchesTimer` starts the job retrieval workflow according to the
six-field NCRONTAB expression in `JobSearchSchedule`.

Automatic execution can be disabled with:

```text
AzureWebJobs.CreateJobMatchesTimer.Disabled=true
```

The same workflow can be started manually through the HTTP function:

```http
POST http://localhost:7095/api/CreateJobMatches
```

Azure-hosted calls require a function or host key in the `x-functions-key`
header. The local Functions host does not enforce authorization by default.

## SCRUM-5 configuration TODOs

- TODO(SCRUM-5/manual): Rotate the database credential that was previously
  committed in `appsettings.json`, then remove it from repository history if
  this repository has been shared. The application no longer reads a database
  password from that tracked file.
- Azure deployments create the `Sql-ConnectionString` secret in the operations
  Key Vault and grant the configured app registration Key Vault Secrets User
  access. The application retrieves that secret when wiring up EF Core.
- Application-owned Azure SDK clients authenticate as the same app registration
  locally and in Azure. The tracked `appsettings.json` contains the non-secret
  `AzureAd:TenantId` and `AzureAd:ClientId` values. Configure
  `AzureAd:ClientSecret` in the top-level `AzureAd` section of the untracked
  `local.settings.json`; the application explicitly loads that file for local
  development.
  The standard `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, and `AZURE_CLIENT_SECRET`
  environment-variable names are also supported and take precedence.
  Azure deployment uses the tenant and client ID from the published
  `appsettings.json`, and uses
  `appRegistrationObjectId` only for RBAC assignments.
- The Functions host continues to use the Function App's system-assigned
  identity for `AzureWebJobsStorage`; this platform connection is established
  before application dependency injection runs. Application Blob Storage,
  Service Bus, and both Key Vault role assignments use the app registration.
- TODO(SCRUM-5/manual): Add the required rows to `JobBoardProvider`, using a
  secret-store reference in `CredentialReference` rather than a plaintext API
  key. Disabled rows are ignored by the workflow.

### Adzuna provider

For Adzuna, configure the provider row as follows:

- `JobBoardName`: `Adzuna`
- `JobBoardApplicationId`: the Adzuna `app_id`
- `FeedUrl`: `https://api.adzuna.com/v1/api/jobs/us/search`
- `CredentialReference`: the name of the secret in the application Key Vault
  (`ajc-keyvault3-{environment}`) containing only the Adzuna `app_key`
- `ExpectedResponseType`: `json`
- `IsEnabled`: `true`

The workflow loads nonblank `JobSearchCriteria` records and issues one request
per enabled provider and criterion. The URL manager appends the configured
search page and adds `app_id`, `app_key`, `results_per_page`, and `what`; the
`what` value comes from `JobSearchCriteriaDescription`. Azure deployments
configure the other non-secret values through `adzunaSearchPage` and
`adzunaResultsPerPage`. The complete constructed URL is persisted in
`JobBoardProviderResponse.RequestUrl`, including the Adzuna API key, so access
to that table and its backups must be restricted accordingly.
