namespace AJC.Functions.Managers.Interfaces;

public interface IJobRetrievalWorkflowManager
{
    Task<Guid> ExecuteAsync(CancellationToken cancellationToken = default);
}
