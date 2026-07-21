using System.Reflection;
using AJC.Functions.Managers.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace AJC.Functions.Tests;

public sealed class CreateJobMatchesTests
{
    [Fact]
    public void FunctionClassIsInheritable()
    {
        Assert.False(typeof(CreateJobMatches).IsSealed);
    }

    [Fact]
    public async Task RunAsyncInvokesWorkflowManagerExactlyOnce()
    {
        var manager = new CountingJobRetrievalWorkflowManager();
        var function = new CreateJobMatches(
            manager,
            NullLogger<CreateJobMatches>.Instance);

        await function.RunAsync(null!, CancellationToken.None);

        Assert.Equal(1, manager.ExecutionCount);
    }

    [Fact]
    public void RunAsyncUsesConfiguredMonitoredSchedule()
    {
        var method = typeof(CreateJobMatches).GetMethod(
            nameof(CreateJobMatches.RunAsync),
            BindingFlags.Instance | BindingFlags.Public);

        var trigger = method!
            .GetParameters()[0]
            .GetCustomAttribute<TimerTriggerAttribute>();

        Assert.NotNull(trigger);
        Assert.Equal("%JobSearchSchedule%", trigger.Schedule);
        Assert.True(trigger.UseMonitor);
    }

    [Fact]
    public void RunManualAsyncUsesFunctionProtectedPostTrigger()
    {
        var method = typeof(CreateJobMatches).GetMethod(
            nameof(CreateJobMatches.RunManualAsync),
            BindingFlags.Instance | BindingFlags.Public);

        var trigger = method!
            .GetParameters()[0]
            .GetCustomAttribute<HttpTriggerAttribute>();

        Assert.NotNull(trigger);
        Assert.Equal(AuthorizationLevel.Function, trigger.AuthLevel);
        Assert.NotNull(trigger.Methods);
        Assert.Contains("post", trigger.Methods, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class CountingJobRetrievalWorkflowManager
        : IJobRetrievalWorkflowManager
    {
        public int ExecutionCount { get; private set; }

        public Task<Guid> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult(Guid.NewGuid());
        }
    }
}
