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
