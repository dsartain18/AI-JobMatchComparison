using AJC.Data.Models;

namespace AJC.Functions.Managers.Interfaces;

public interface IJobBoardUrlManager
{
    Task<string> BuildUrlAsync(
        JobBoardProvider provider,
        JobSearchCriterion searchCriterion,
        CancellationToken cancellationToken = default);
}
