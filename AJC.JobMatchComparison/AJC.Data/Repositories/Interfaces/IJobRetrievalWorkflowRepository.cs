using AJC.Data.Models;

namespace AJC.Functions.Repositories.Interfaces;

public interface IJobRetrievalWorkflowRepository
{
    Task AddExecutionAsync(
        JobRetrievalWorkflowExecution execution,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobBoardProvider>> GetEnabledProvidersAsync(
        CancellationToken cancellationToken = default);

    Task AddProviderResponseAsync(
        JobBoardProviderResponse response,
        CancellationToken cancellationToken = default);

    Task CompleteExecutionAsync(
        Guid workflowExecutionId,
        DateTime completedDate,
        string executionStatus,
        int providersAttempted,
        int providersSucceeded,
        int providersFailed,
        string? failureMessage = null,
        CancellationToken cancellationToken = default);
}
