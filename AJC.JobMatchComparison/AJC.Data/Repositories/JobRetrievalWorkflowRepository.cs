using AJC.Data.Models;
using AJC.Functions.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AJC.Functions.Repositories;

public sealed class JobRetrievalWorkflowRepository : IJobRetrievalWorkflowRepository
{
    private readonly ApplicationDbContext _dbContext;

    public JobRetrievalWorkflowRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddExecutionAsync(
        JobRetrievalWorkflowExecution execution,
        CancellationToken cancellationToken = default)
    {
        _dbContext.JobRetrievalWorkflowExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobBoardProvider>> GetEnabledProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.JobBoardProviders
            .AsNoTracking()
            .Where(provider => provider.IsEnabled)
            .OrderBy(provider => provider.JobBoardProviderId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddProviderResponseAsync(
        JobBoardProviderResponse response,
        CancellationToken cancellationToken = default)
    {
        _dbContext.JobBoardProviderResponses.Add(response);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteExecutionAsync(
        Guid workflowExecutionId,
        DateTime completedDate,
        string executionStatus,
        int providersAttempted,
        int providersSucceeded,
        int providersFailed,
        string? failureMessage = null,
        CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.JobRetrievalWorkflowExecutions.FindAsync(
            [workflowExecutionId],
            cancellationToken);

        if (execution is null)
        {
            throw new InvalidOperationException(
                $"Workflow execution {workflowExecutionId} could not be found.");
        }

        execution.CompletedDate = completedDate;
        execution.ExecutionStatus = executionStatus;
        execution.ProvidersAttempted = providersAttempted;
        execution.ProvidersSucceeded = providersSucceeded;
        execution.ProvidersFailed = providersFailed;
        execution.FailureMessage = failureMessage;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
