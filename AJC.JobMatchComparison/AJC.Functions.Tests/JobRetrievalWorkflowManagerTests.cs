using AJC.Functions.Managers;
using AJC.Functions.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AJC.Functions.Tests;

public sealed class JobRetrievalWorkflowManagerTests
{
    [Fact]
    public async Task ExecuteAsyncGeneratesUniqueExecutionIdentifiers()
    {
        var lockService = new TestJobExecutionLockService();
        var manager = new JobRetrievalWorkflowManager(
            lockService,
            new TestLogger<JobRetrievalWorkflowManager>());

        var firstExecutionId = await manager.ExecuteAsync();
        var secondExecutionId = await manager.ExecuteAsync();

        Assert.NotEqual(Guid.Empty, firstExecutionId);
        Assert.NotEqual(Guid.Empty, secondExecutionId);
        Assert.NotEqual(firstExecutionId, secondExecutionId);
        Assert.Equal(2, lockService.AcquisitionCount);
        Assert.Equal(2, lockService.DisposalCount);
    }

    [Fact]
    public async Task ExecuteAsyncSkipsWhenAnotherExecutionOwnsTheLock()
    {
        var lockService = new TestJobExecutionLockService(lockAvailable: false);
        var logger = new TestLogger<JobRetrievalWorkflowManager>();
        var manager = new JobRetrievalWorkflowManager(lockService, logger);

        var executionId = await manager.ExecuteAsync();

        Assert.NotEqual(Guid.Empty, executionId);
        Assert.Equal(1, lockService.AcquisitionCount);
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("SkippedConcurrent", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsyncLogsStartCompletionAndDuration()
    {
        var logger = new TestLogger<JobRetrievalWorkflowManager>();
        var manager = new JobRetrievalWorkflowManager(
            new TestJobExecutionLockService(),
            logger);

        await manager.ExecuteAsync();

        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("started at", StringComparison.Ordinal)
                && entry.Message.Contains("Central Time", StringComparison.Ordinal));
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("completed successfully", StringComparison.Ordinal)
                && entry.Message.Contains("Central Time", StringComparison.Ordinal));
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("milliseconds", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsyncLogsAndRethrowsFailures()
    {
        var expectedException = new InvalidOperationException("Lock service failure.");
        var logger = new TestLogger<JobRetrievalWorkflowManager>();
        var manager = new JobRetrievalWorkflowManager(
            new ThrowingJobExecutionLockService(expectedException),
            logger);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.ExecuteAsync());

        Assert.Same(expectedException, actualException);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Error
                && entry.Message.Contains("failed at", StringComparison.Ordinal)
                && entry.Message.Contains("Central Time", StringComparison.Ordinal)
                && ReferenceEquals(entry.Exception, expectedException));
    }

    private sealed class TestJobExecutionLockService : IJobExecutionLockService
    {
        private readonly bool _lockAvailable;

        public TestJobExecutionLockService(bool lockAvailable = true)
        {
            _lockAvailable = lockAvailable;
        }

        public int AcquisitionCount { get; private set; }

        public int DisposalCount { get; private set; }

        public Task<IAsyncDisposable?> TryAcquireAsync(
            CancellationToken cancellationToken = default)
        {
            AcquisitionCount++;

            IAsyncDisposable? executionLock = _lockAvailable
                ? new TestExecutionLock(() => DisposalCount++)
                : null;

            return Task.FromResult(executionLock);
        }
    }

    private sealed class ThrowingJobExecutionLockService : IJobExecutionLockService
    {
        private readonly Exception _exception;

        public ThrowingJobExecutionLockService(Exception exception)
        {
            _exception = exception;
        }

        public Task<IAsyncDisposable?> TryAcquireAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<IAsyncDisposable?>(_exception);
        }
    }

    private sealed class TestExecutionLock : IAsyncDisposable
    {
        private readonly Action _onDispose;

        public TestExecutionLock(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public ValueTask DisposeAsync()
        {
            _onDispose();
            return ValueTask.CompletedTask;
        }
    }
}

internal sealed record LogEntry(
    LogLevel Level,
    string Message,
    Exception? Exception);

internal sealed class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
