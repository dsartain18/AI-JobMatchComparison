using AJC.Data.Models;

namespace AJC.Functions.Services.Interfaces;

public interface IJobBoardProviderClient
{
    Task<JobBoardProviderResponse> RetrieveAsync(
        Guid workflowExecutionId,
        JobBoardProvider provider,
        JobSearchCriterion searchCriterion,
        CancellationToken cancellationToken = default);
}
