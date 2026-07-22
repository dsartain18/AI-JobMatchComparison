using System.Diagnostics;
using AJC.Functions.Managers.Interfaces;
using AJC.Functions.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AJC.Functions.Managers;

public sealed class JobRetrievalWorkflowManager : IJobRetrievalWorkflowManager
{
    private static readonly TimeZoneInfo CentralTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    private readonly IJobExecutionLockService _jobExecutionLockService;
    private readonly ILogger<JobRetrievalWorkflowManager> _logger;

    public JobRetrievalWorkflowManager(
        IJobExecutionLockService jobExecutionLockService,
        ILogger<JobRetrievalWorkflowManager> logger)
    {
        _jobExecutionLockService = jobExecutionLockService
            ?? throw new ArgumentNullException(nameof(jobExecutionLockService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid();
        var startedAtCentral = GetCentralTime();
        var startedTimestamp = Stopwatch.GetTimestamp();

        using var loggingScope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["JobSearchExecutionId"] = executionId
            });

        _logger.LogInformation(
            "Job retrieval workflow execution {JobSearchExecutionId} started at {StartedAtCentral} Central Time.",
            executionId,
            startedAtCentral);

        try
        {
            await using var executionLock =
                await _jobExecutionLockService.TryAcquireAsync(cancellationToken);

            if (executionLock is null)
            {
                _logger.LogWarning(
                    "Job retrieval workflow execution {JobSearchExecutionId} did not start because another execution is active.",
                    executionId);

                _logger.LogInformation(
                    "Job retrieval workflow execution {JobSearchExecutionId} completed with status SkippedConcurrent at {CompletedAtCentral} Central Time after {DurationMilliseconds} milliseconds.",
                    executionId,
                    GetCentralTime(),
                    Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds);

                return executionId;
            }

            // SCRUM-5 extends this workflow entry point with provider retrieval.
            // SCRUM-4 intentionally contains no provider-specific behavior.

            _logger.LogInformation(
                "Job retrieval workflow execution {JobSearchExecutionId} completed successfully at {CompletedAtCentral} Central Time after {DurationMilliseconds} milliseconds.",
                executionId,
                GetCentralTime(),
                Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds);

            return executionId;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Job retrieval workflow execution {JobSearchExecutionId} failed at {FailedAtCentral} Central Time after {DurationMilliseconds} milliseconds.",
                executionId,
                GetCentralTime(),
                Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds);

            throw;
        }
    }

    private static DateTimeOffset GetCentralTime()
    {
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, CentralTimeZone);
    }
}
