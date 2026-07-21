using AJC.Functions.Services.Interfaces;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;

namespace AJC.Functions.Services;

public sealed class BlobJobExecutionLockService : IJobExecutionLockService
{
    internal const string LockContainerName = "job-search-scheduler-locks";

    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan LeaseRenewalInterval = TimeSpan.FromSeconds(30);

    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobJobExecutionLockService> _logger;

    public BlobJobExecutionLockService(
        BlobServiceClient blobServiceClient,
        ILogger<BlobJobExecutionLockService> logger)
    {
        ArgumentNullException.ThrowIfNull(blobServiceClient);

        _containerClient = blobServiceClient.GetBlobContainerClient(LockContainerName);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var leaseClient = _containerClient.GetBlobLeaseClient();

        try
        {
            await leaseClient.AcquireAsync(LeaseDuration, cancellationToken: cancellationToken);

            return new RenewableBlobLease(
                leaseClient,
                LeaseRenewalInterval,
                _logger);
        }
        catch (RequestFailedException exception) when (
            exception.Status == 409
            && exception.ErrorCode == BlobErrorCode.LeaseAlreadyPresent.ToString())
        {
            return null;
        }
    }

    private sealed class RenewableBlobLease : IAsyncDisposable
    {
        private readonly BlobLeaseClient _leaseClient;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _renewalCancellation = new();
        private readonly Task _renewalTask;

        public RenewableBlobLease(
            BlobLeaseClient leaseClient,
            TimeSpan renewalInterval,
            ILogger logger)
        {
            _leaseClient = leaseClient;
            _logger = logger;
            _renewalTask = RenewLeaseAsync(renewalInterval, _renewalCancellation.Token);
        }

        public async ValueTask DisposeAsync()
        {
            await _renewalCancellation.CancelAsync();

            try
            {
                await _renewalTask;
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected when the workflow completes.
            }

            try
            {
                await _leaseClient.ReleaseAsync();
            }
            catch (RequestFailedException exception)
            {
                _logger.LogWarning(
                    exception,
                    "The job-search execution lease could not be released and will expire automatically.");
            }
            finally
            {
                _renewalCancellation.Dispose();
            }
        }

        private async Task RenewLeaseAsync(
            TimeSpan renewalInterval,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(renewalInterval, cancellationToken);

                try
                {
                    await _leaseClient.RenewAsync(cancellationToken: cancellationToken);
                }
                catch (RequestFailedException exception)
                {
                    _logger.LogError(
                        exception,
                        "The job-search execution lease could not be renewed.");
                    return;
                }
            }
        }
    }
}
