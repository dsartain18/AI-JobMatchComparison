using AJC.Data.Models;
using AJC.Functions.Managers;
using AJC.Functions.Repositories.Interfaces;
using AJC.Functions.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AJC.Functions.Tests;

public sealed class JobRetrievalWorkflowManagerTests
{
    [Fact]
    public async Task ExecuteAsyncRetrievesAndPersistsEveryEnabledProvider()
    {
        var providers = new[]
        {
            CreateProvider(1, "Board One"),
            CreateProvider(2, "Board Two")
        };
        var repository = new TestRepository(providers);
        var client = new TestProviderClient();
        var manager = CreateManager(repository: repository, providerClient: client);

        var executionId = await manager.ExecuteAsync();

        Assert.Equal(providers, client.RequestedProviders);
        Assert.Equal(2, repository.Responses.Count);
        Assert.All(repository.Responses, response =>
        {
            Assert.Equal(executionId, response.WorkflowExecutionId);
            Assert.False(string.IsNullOrWhiteSpace(response.JobBoardName));
        });
        Assert.Equal(("Completed", 2, 2, 0), repository.Completion);
    }

    [Fact]
    public async Task ExecuteAsyncContinuesAfterProviderFailure()
    {
        var providers = new[]
        {
            CreateProvider(1, "Failing Board"),
            CreateProvider(2, "Healthy Board")
        };
        var repository = new TestRepository(providers);
        var client = new TestProviderClient(failingProviderId: 1);
        var manager = CreateManager(repository: repository, providerClient: client);

        await manager.ExecuteAsync();

        Assert.Equal(providers, client.RequestedProviders);
        Assert.Equal(2, repository.Responses.Count);
        Assert.Equal(("CompletedWithErrors", 2, 1, 1), repository.Completion);
        Assert.Contains(repository.Responses, response =>
            response.JobBoardProviderId == 1 && !response.WasSuccessful);
        Assert.Contains(repository.Responses, response =>
            response.JobBoardProviderId == 2 && response.WasSuccessful);
    }

    [Fact]
    public async Task ExecuteAsyncCallsEveryProviderForEverySearchCriterion()
    {
        var providers = new[] { CreateProvider(1, "Board One") };
        var criteria = new[]
        {
            CreateSearchCriterion(1, "Software Engineer"),
            CreateSearchCriterion(2, "Cloud Architect")
        };
        var repository = new TestRepository(providers, criteria);
        var client = new TestProviderClient();
        var manager = CreateManager(repository: repository, providerClient: client);

        await manager.ExecuteAsync();

        Assert.Equal(criteria, client.RequestedSearchCriteria);
        Assert.Equal(("Completed", 2, 2, 0), repository.Completion);
    }

    [Fact]
    public async Task ExecuteAsyncGeneratesUniqueExecutionIdentifiers()
    {
        var lockService = new TestJobExecutionLockService();
        var manager = CreateManager(lockService);

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
        var repository = new TestRepository();
        var manager = CreateManager(lockService, repository, logger: logger);

        var executionId = await manager.ExecuteAsync();

        Assert.NotEqual(Guid.Empty, executionId);
        Assert.Equal(1, lockService.AcquisitionCount);
        Assert.Empty(repository.Executions);
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("SkippedConcurrent", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsyncLogsStartCompletionAndDuration()
    {
        var logger = new TestLogger<JobRetrievalWorkflowManager>();
        var manager = CreateManager(logger: logger);

        await manager.ExecuteAsync();

        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("started at", StringComparison.Ordinal)
                && entry.Message.Contains("Central Time", StringComparison.Ordinal));
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("completed with status Completed", StringComparison.Ordinal)
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
        var manager = CreateManager(
            new ThrowingJobExecutionLockService(expectedException),
            logger: logger);

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

    private static JobRetrievalWorkflowManager CreateManager(
        IJobExecutionLockService? lockService = null,
        TestRepository? repository = null,
        IJobBoardProviderClient? providerClient = null,
        TestLogger<JobRetrievalWorkflowManager>? logger = null)
    {
        return new JobRetrievalWorkflowManager(
            lockService ?? new TestJobExecutionLockService(),
            repository ?? new TestRepository(),
            providerClient ?? new TestProviderClient(),
            logger ?? new TestLogger<JobRetrievalWorkflowManager>());
    }

    private static JobBoardProvider CreateProvider(int id, string name)
    {
        return new JobBoardProvider
        {
            JobBoardProviderId = id,
            JobBoardName = name,
            FeedUrl = $"https://example.test/{id}",
            ExpectedResponseType = "json",
            IsEnabled = true
        };
    }

    private static JobSearchCriterion CreateSearchCriterion(int id, string description)
    {
        return new JobSearchCriterion
        {
            JobSearchCriteriaId = id,
            JobSearchCriteriaDescription = description
        };
    }

    private sealed class TestRepository : IJobRetrievalWorkflowRepository
    {
        private readonly IReadOnlyList<JobBoardProvider> _providers;
        private readonly IReadOnlyList<JobSearchCriterion> _searchCriteria;

        public TestRepository(
            IReadOnlyList<JobBoardProvider>? providers = null,
            IReadOnlyList<JobSearchCriterion>? searchCriteria = null)
        {
            _providers = providers ?? [];
            _searchCriteria = searchCriteria ??
                [CreateSearchCriterion(1, "Software Engineer")];
        }

        public List<JobRetrievalWorkflowExecution> Executions { get; } = [];

        public List<JobBoardProviderResponse> Responses { get; } = [];

        public (string Status, int Attempted, int Succeeded, int Failed)? Completion { get; private set; }

        public Task AddExecutionAsync(
            JobRetrievalWorkflowExecution execution,
            CancellationToken cancellationToken = default)
        {
            Executions.Add(execution);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<JobBoardProvider>> GetEnabledProvidersAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_providers);
        }

        public Task<IReadOnlyList<JobSearchCriterion>> GetSearchCriteriaAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_searchCriteria);
        }

        public Task AddProviderResponseAsync(
            JobBoardProviderResponse response,
            CancellationToken cancellationToken = default)
        {
            Responses.Add(response);
            return Task.CompletedTask;
        }

        public Task CompleteExecutionAsync(
            Guid workflowExecutionId,
            DateTime completedDate,
            string executionStatus,
            int providersAttempted,
            int providersSucceeded,
            int providersFailed,
            string? failureMessage = null,
            CancellationToken cancellationToken = default)
        {
            Completion = (executionStatus, providersAttempted, providersSucceeded, providersFailed);
            return Task.CompletedTask;
        }
    }

    private sealed class TestProviderClient : IJobBoardProviderClient
    {
        private readonly int? _failingProviderId;

        public TestProviderClient(int? failingProviderId = null)
        {
            _failingProviderId = failingProviderId;
        }

        public List<JobBoardProvider> RequestedProviders { get; } = [];

        public List<JobSearchCriterion> RequestedSearchCriteria { get; } = [];

        public Task<JobBoardProviderResponse> RetrieveAsync(
            Guid workflowExecutionId,
            JobBoardProvider provider,
            JobSearchCriterion searchCriterion,
            CancellationToken cancellationToken = default)
        {
            RequestedProviders.Add(provider);
            RequestedSearchCriteria.Add(searchCriterion);
            var successful = provider.JobBoardProviderId != _failingProviderId;
            return Task.FromResult(new JobBoardProviderResponse
            {
                WorkflowExecutionId = workflowExecutionId,
                JobBoardProviderId = provider.JobBoardProviderId,
                JobBoardName = provider.JobBoardName,
                RequestUrl = provider.FeedUrl,
                RequestStartedDate = DateTime.Now,
                RequestCompletedDate = DateTime.Now,
                WasSuccessful = successful,
                RawResponseBody = successful ? "{}" : null,
                FailureType = successful ? null : "TestFailure"
            });
        }
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

    public bool IsEnabled(LogLevel logLevel) => true;

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
