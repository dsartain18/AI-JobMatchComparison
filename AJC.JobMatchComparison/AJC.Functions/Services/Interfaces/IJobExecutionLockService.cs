namespace AJC.Functions.Services.Interfaces;

public interface IJobExecutionLockService
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        CancellationToken cancellationToken = default);
}
