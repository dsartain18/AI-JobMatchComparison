using System.Net;
using AJC.Functions.Managers.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AJC.Functions;

public class CreateJobMatches
{
    private readonly IJobRetrievalWorkflowManager _jobRetrievalWorkflowManager;
    private readonly ILogger<CreateJobMatches> _logger;

    public CreateJobMatches(
        IJobRetrievalWorkflowManager jobRetrievalWorkflowManager,
        ILogger<CreateJobMatches> logger)
    {
        _jobRetrievalWorkflowManager = jobRetrievalWorkflowManager
            ?? throw new ArgumentNullException(nameof(jobRetrievalWorkflowManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function("CreateJobMatches")]
    public async Task<HttpResponseData> RunManualAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var executionId =
            await _jobRetrievalWorkflowManager.ExecuteAsync(cancellationToken);

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(executionId.ToString(), cancellationToken);

        return response;
    }

    [Function("CreateJobMatchesTimer")]
    public async Task RunAsync(
        [TimerTrigger("%JobSearchSchedule%", UseMonitor = true)] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        if (timer?.IsPastDue == true)
        {
            _logger.LogWarning(
                "CreateJobMatchesTimer is running later than its configured schedule.");
        }

        await _jobRetrievalWorkflowManager.ExecuteAsync(cancellationToken);
    }
}
