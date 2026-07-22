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
  Key Vault and grant the Function App's managed identity Key Vault Secrets
  User access. The application retrieves that secret when wiring up EF Core.
- TODO(SCRUM-5/manual): For local execution, either sign in with an identity
  that can read the deployed operations Key Vault or add
  `ConnectionStrings:JobMatchComparisonContext` to the untracked
  `local.settings.json` file.
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
`adzunaResultsPerPage`. Never log or persist the constructed URL because it
contains the Adzuna API key.
