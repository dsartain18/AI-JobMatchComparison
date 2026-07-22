using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AJC.Data.Models;
using AJC.Functions.Managers.Interfaces;
using AJC.Functions.Services;

namespace AJC.Functions.Tests;

public sealed class JobBoardProviderClientTests
{
    [Fact]
    public async Task RetrieveAsyncUsesUrlBuiltByManager()
    {
        Uri? requestedUri = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });
        var expectedUrl = "https://example.test/jobs/1?what=Software%20Engineer";
        var client = new JobBoardProviderClient(
            new HttpClient(handler),
            new FixedUrlManager(expectedUrl),
            new TestLogger<JobBoardProviderClient>());

        var result = await client.RetrieveAsync(
            Guid.NewGuid(),
            CreateProvider(),
            CreateSearchCriterion());

        Assert.Equal(expectedUrl, requestedUri?.AbsoluteUri);
        Assert.Equal(expectedUrl, result.RequestUrl);
    }

    [Fact]
    public async Task RetrieveAsyncPersistsFullBuiltUrl()
    {
        Uri? requestedUri = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });
        var builtUrl =
            "https://example.test/jobs/1?app_id=application-id&app_key=sensitive-key&results_per_page=25";
        var client = new JobBoardProviderClient(
            new HttpClient(handler),
            new FixedUrlManager(builtUrl),
            new TestLogger<JobBoardProviderClient>());

        var result = await client.RetrieveAsync(
            Guid.NewGuid(),
            CreateProvider(),
            CreateSearchCriterion());

        Assert.Equal(builtUrl, requestedUri?.AbsoluteUri);
        Assert.Equal(builtUrl, result.RequestUrl);
    }

    [Fact]
    public async Task RetrieveAsyncAddsAdzunaHeaders()
    {
        IReadOnlyCollection<string>? acceptedMediaTypes = null;
        string? requestContentType = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            acceptedMediaTypes = request.Headers.Accept
                .Select(value => value.MediaType ?? "")
                .ToArray();
            requestContentType = request.Content?.Headers.ContentType?.MediaType;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });
        var provider = CreateProvider();
        provider.JobBoardName = "Adzuna";
        var client = new JobBoardProviderClient(
            new HttpClient(handler),
            new PassthroughUrlManager(),
            new TestLogger<JobBoardProviderClient>());

        await client.RetrieveAsync(
            Guid.NewGuid(),
            provider,
            CreateSearchCriterion());

        Assert.Contains("*/*", acceptedMediaTypes!);
        Assert.Equal("application/json", requestContentType);
    }

    [Fact]
    public async Task RetrieveAsyncCapturesRawBodyAndResponseMetadata()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"jobs\":[]}")
            };
            response.Headers.Add("X-Request-Id", "request-123");
            return response;
        });
        var client = new JobBoardProviderClient(
            new HttpClient(handler),
            new PassthroughUrlManager(),
            new TestLogger<JobBoardProviderClient>());

        var result = await client.RetrieveAsync(
            Guid.NewGuid(),
            CreateProvider(),
            CreateSearchCriterion());

        Assert.True(result.WasSuccessful);
        Assert.Equal((short)200, result.HttpStatusCode);
        Assert.Equal("{\"jobs\":[]}", result.RawResponseBody);
        Assert.True(result.ResponseHeaders?.Contains(
            "X-Request-Id",
            StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(result.RequestCompletedDate);
        Assert.NotNull(result.DurationMilliseconds);
    }

    [Fact]
    public async Task RetrieveAsyncReadsJsonWhenResponseCharsetIsInvalid()
    {
        const string responseBody = "{\"jobs\":[{\"title\":\"Staff Engineer\"}]}";
        var handler = new StubHttpMessageHandler(_ =>
        {
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes(responseBody));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
            {
                CharSet = "invalid-charset"
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
        });
        var client = new JobBoardProviderClient(
            new HttpClient(handler),
            new PassthroughUrlManager(),
            new TestLogger<JobBoardProviderClient>());

        var result = await client.RetrieveAsync(
            Guid.NewGuid(),
            CreateProvider(),
            CreateSearchCriterion());

        Assert.True(result.WasSuccessful);
        Assert.Equal(responseBody, result.RawResponseBody);
        Assert.Equal("application/json; charset=invalid-charset", result.ResponseContentType);
    }

    [Fact]
    public async Task RetrieveAsyncCapturesNonSuccessResponseWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("temporarily unavailable")
            });
        var client = new JobBoardProviderClient(
            new HttpClient(handler),
            new PassthroughUrlManager(),
            new TestLogger<JobBoardProviderClient>());

        var result = await client.RetrieveAsync(
            Guid.NewGuid(),
            CreateProvider(),
            CreateSearchCriterion());

        Assert.False(result.WasSuccessful);
        Assert.Equal((short)503, result.HttpStatusCode);
        Assert.Equal("temporarily unavailable", result.RawResponseBody);
        Assert.Equal("HttpStatusCode", result.FailureType);
    }

    [Fact]
    public async Task RetrieveAsyncCapturesTransportFailureWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new HttpRequestException("Network unavailable."));
        var client = new JobBoardProviderClient(
            new HttpClient(handler),
            new PassthroughUrlManager(),
            new TestLogger<JobBoardProviderClient>());

        var result = await client.RetrieveAsync(
            Guid.NewGuid(),
            CreateProvider(),
            CreateSearchCriterion());

        Assert.False(result.WasSuccessful);
        Assert.Null(result.HttpStatusCode);
        Assert.Equal(nameof(HttpRequestException), result.FailureType);
        Assert.Equal("Network unavailable.", result.FailureMessage);
    }

    private static JobBoardProvider CreateProvider()
    {
        return new JobBoardProvider
        {
            JobBoardProviderId = 42,
            JobBoardName = "Test Board",
            FeedUrl = "https://example.test/jobs",
            ExpectedResponseType = "json",
            IsEnabled = true
        };
    }

    private static JobSearchCriterion CreateSearchCriterion()
    {
        return new JobSearchCriterion
        {
            JobSearchCriteriaId = 1,
            JobSearchCriteriaDescription = "Software Engineer"
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    private sealed class PassthroughUrlManager : IJobBoardUrlManager
    {
        public Task<string> BuildUrlAsync(
            JobBoardProvider provider,
            JobSearchCriterion searchCriterion,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(provider.FeedUrl);
        }
    }

    private sealed class FixedUrlManager : IJobBoardUrlManager
    {
        private readonly string _url;

        public FixedUrlManager(string url)
        {
            _url = url;
        }

        public Task<string> BuildUrlAsync(
            JobBoardProvider provider,
            JobSearchCriterion searchCriterion,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_url);
        }
    }
}
