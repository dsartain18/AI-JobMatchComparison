using System.Diagnostics;
using System.Text.Json;
using AJC.Data.Models;
using AJC.Functions.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AJC.Functions.Services;

public sealed class JobBoardProviderClient : IJobBoardProviderClient
{
    private static readonly TimeZoneInfo CentralTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    private readonly HttpClient _httpClient;
    private readonly ILogger<JobBoardProviderClient> _logger;

    public JobBoardProviderClient(
        HttpClient httpClient,
        ILogger<JobBoardProviderClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<JobBoardProviderResponse> RetrieveAsync(
        Guid workflowExecutionId,
        JobBoardProvider provider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var startedDate = GetCentralTime();
        var startedTimestamp = Stopwatch.GetTimestamp();

        using var loggingScope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["WorkflowExecutionId"] = workflowExecutionId,
                ["JobBoardProviderId"] = provider.JobBoardProviderId,
                ["JobBoardName"] = provider.JobBoardName
            });

        _logger.LogInformation(
            "Provider request started for {JobBoardName} ({JobBoardProviderId}) in workflow {WorkflowExecutionId} at {RequestStartedDate} Central Time.",
            provider.JobBoardName,
            provider.JobBoardProviderId,
            workflowExecutionId,
            startedDate);

        var result = new JobBoardProviderResponse
        {
            WorkflowExecutionId = workflowExecutionId,
            JobBoardProviderId = provider.JobBoardProviderId,
            JobBoardName = provider.JobBoardName,
            RequestUrl = provider.FeedUrl,
            RequestStartedDate = startedDate
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, provider.FeedUrl);

            // TODO(SCRUM-5/manual): Resolve provider.CredentialReference from the approved
            // secret store and apply it to this request when authentication is implemented.
            // Never replace the reference in JobBoardProvider with a plaintext API key.

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            result.RawResponseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            result.HttpStatusCode = (short)response.StatusCode;
            result.ResponseContentType = response.Content.Headers.ContentType?.ToString();
            result.ResponseHeaders = SerializeHeaders(response);
            result.WasSuccessful = response.IsSuccessStatusCode;

            if (!response.IsSuccessStatusCode)
            {
                result.FailureType = "HttpStatusCode";
                result.FailureMessage =
                    $"The provider returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            result.WasSuccessful = false;
            result.FailureType = exception.GetType().Name;
            result.FailureMessage = Truncate(exception.Message, 4000);
        }
        finally
        {
            result.RequestCompletedDate = GetCentralTime();
            result.DurationMilliseconds =
                (long)Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds;
        }

        if (result.WasSuccessful)
        {
            _logger.LogInformation(
                "Provider request completed for {JobBoardName} ({JobBoardProviderId}) in workflow {WorkflowExecutionId} with HTTP status {HttpStatusCode} after {DurationMilliseconds} milliseconds.",
                provider.JobBoardName,
                provider.JobBoardProviderId,
                workflowExecutionId,
                result.HttpStatusCode,
                result.DurationMilliseconds);
        }
        else
        {
            _logger.LogWarning(
                "Provider request failed for {JobBoardName} ({JobBoardProviderId}) in workflow {WorkflowExecutionId} with HTTP status {HttpStatusCode} after {DurationMilliseconds} milliseconds. Failure: {FailureType}: {FailureMessage}",
                provider.JobBoardName,
                provider.JobBoardProviderId,
                workflowExecutionId,
                result.HttpStatusCode,
                result.DurationMilliseconds,
                result.FailureType,
                result.FailureMessage);
        }

        return result;
    }

    private static string SerializeHeaders(HttpResponseMessage response)
    {
        var headers = response.Headers
            .Concat(response.Content.Headers)
            .GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(header => header.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(headers);
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }

    private static DateTime GetCentralTime()
    {
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, CentralTimeZone).DateTime;
    }
}
