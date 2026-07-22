using System.Diagnostics;
using AJC.Data.Models;
using AJC.Functions.Managers.Interfaces;
using AJC.Functions.Repositories.Interfaces;
using AJC.Functions.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AJC.Functions.Managers;

public sealed class JobRetrievalWorkflowManager : IJobRetrievalWorkflowManager
{
    private static readonly TimeZoneInfo CentralTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    private readonly IJobExecutionLockService _jobExecutionLockService;
    private readonly IJobRetrievalWorkflowRepository _repository;
    private readonly IJobBoardProviderClient _providerClient;
    private readonly ILogger<JobRetrievalWorkflowManager> _logger;

    public JobRetrievalWorkflowManager(
        IJobExecutionLockService jobExecutionLockService,
        IJobRetrievalWorkflowRepository repository,
        IJobBoardProviderClient providerClient,
        ILogger<JobRetrievalWorkflowManager> logger)
    {
        _jobExecutionLockService = jobExecutionLockService
            ?? throw new ArgumentNullException(nameof(jobExecutionLockService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _providerClient = providerClient ?? throw new ArgumentNullException(nameof(providerClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid();
        var startedAtCentral = GetCentralTime();
        var startedTimestamp = Stopwatch.GetTimestamp();
        var executionWasCreated = false;
        var attempted = 0;
        var succeeded = 0;
        var failed = 0;

        using var loggingScope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["WorkflowExecutionId"] = executionId
            });

        _logger.LogInformation(
            "Job retrieval workflow execution {WorkflowExecutionId} started at {StartedAtCentral} Central Time.",
            executionId,
            startedAtCentral);

        try
        {
            await using var executionLock =
                await _jobExecutionLockService.TryAcquireAsync(cancellationToken);

            if (executionLock is null)
            {
                _logger.LogWarning(
                    "Job retrieval workflow execution {WorkflowExecutionId} did not start because another execution is active.",
                    executionId);

                _logger.LogInformation(
                    "Job retrieval workflow execution {WorkflowExecutionId} completed with status SkippedConcurrent at {CompletedAtCentral} Central Time after {DurationMilliseconds} milliseconds.",
                    executionId,
                    GetCentralTime(),
                    Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds);

                return executionId;
            }

            await _repository.AddExecutionAsync(
                new JobRetrievalWorkflowExecution
                {
                    WorkflowExecutionId = executionId,
                    StartedDate = startedAtCentral,
                    ExecutionStatus = "Started"
                },
                cancellationToken);
            executionWasCreated = true;

            var providers = await _repository.GetEnabledProvidersAsync(cancellationToken);

            _logger.LogInformation(
                "Loaded {ProviderCount} enabled job-board providers for workflow {WorkflowExecutionId}.",
                providers.Count,
                executionId);

            foreach (var provider in providers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempted++;

                try
                {
                    var response = await _providerClient.RetrieveAsync(
                        executionId,
                        provider,
                        cancellationToken);

                    await _repository.AddProviderResponseAsync(response, cancellationToken);

                    if (response.WasSuccessful)
                    {
                        succeeded++;
                    }
                    else
                    {
                        failed++;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failed++;
                    _logger.LogError(
                        exception,
                        "Provider {JobBoardName} ({JobBoardProviderId}) failed independently in workflow {WorkflowExecutionId}; remaining providers will still be called.",
                        provider.JobBoardName,
                        provider.JobBoardProviderId,
                        executionId);
                }
            }

            var executionStatus = failed == 0 ? "Completed" : "CompletedWithErrors";
            var completedAtCentral = GetCentralTime();

            await _repository.CompleteExecutionAsync(
                executionId,
                completedAtCentral,
                executionStatus,
                attempted,
                succeeded,
                failed,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Job retrieval workflow execution {WorkflowExecutionId} completed with status {ExecutionStatus} at {CompletedAtCentral} Central Time after {DurationMilliseconds} milliseconds. Providers attempted: {ProvidersAttempted}; succeeded: {ProvidersSucceeded}; failed: {ProvidersFailed}.",
                executionId,
                executionStatus,
                completedAtCentral,
                Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds,
                attempted,
                succeeded,
                failed);

            return executionId;
        }
        catch (Exception exception)
        {
            if (executionWasCreated)
            {
                try
                {
                    await _repository.CompleteExecutionAsync(
                        executionId,
                        GetCentralTime(),
                        "Failed",
                        attempted,
                        succeeded,
                        failed,
                        Truncate(exception.Message, 4000),
                        CancellationToken.None);
                }
                catch (Exception persistenceException)
                {
                    _logger.LogError(
                        persistenceException,
                        "Could not persist the failed status for workflow {WorkflowExecutionId}.",
                        executionId);
                }
            }

            _logger.LogError(
                exception,
                "Job retrieval workflow execution {WorkflowExecutionId} failed at {FailedAtCentral} Central Time after {DurationMilliseconds} milliseconds.",
                executionId,
                GetCentralTime(),
                Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds);

            throw;
        }
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }

    private static DateTime GetCentralTime()
    {
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, CentralTimeZone).DateTime;
    }
}
