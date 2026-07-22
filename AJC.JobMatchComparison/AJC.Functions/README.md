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
- TODO(SCRUM-5/manual): When provider authentication is implemented in its
  separate story, configure the Function App's managed identity and grant it
  read access to the approved secret store.
