using AJC.Data.Models;
using AJC.Functions.Managers;
using AJC.Functions.Managers.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AJC.Functions.Tests;

public sealed class JobBoardUrlManagerTests
{
    [Fact]
    public async Task BuildUrlAsyncBuildsEncodedAdzunaUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Adzuna:SearchPage"] = "2",
                ["Adzuna:ResultsPerPage"] = "25"
            })
            .Build();
        var secretManager = new TestSecretManager("secret/value");
        var manager = new JobBoardUrlManager(secretManager, configuration);
        var provider = CreateAdzunaProvider();

        var result = await manager.BuildUrlAsync(
            provider,
            CreateSearchCriterion("Software Engineer"));

        Assert.Equal(
            "https://api.adzuna.com/v1/api/jobs/us/search/2" +
            "?app_id=application-id&app_key=secret%2Fvalue" +
            "&results_per_page=25&what=Software%20Engineer",
            result);
        Assert.Equal("Adzuna-ApiKey", secretManager.RequestedSecretName);
    }

    [Fact]
    public async Task BuildUrlAsyncReturnsConfiguredUrlForOtherProviders()
    {
        var configuration = new ConfigurationBuilder().Build();
        var manager = new JobBoardUrlManager(
            new TestSecretManager("unused"),
            configuration);
        var provider = new JobBoardProvider
        {
            JobBoardName = "Public Feed",
            FeedUrl = "https://example.test/jobs",
            ExpectedResponseType = "json"
        };

        var result = await manager.BuildUrlAsync(
            provider,
            CreateSearchCriterion("Software Engineer"));

        Assert.Equal(provider.FeedUrl, result);
    }

    [Fact]
    public async Task BuildUrlAsyncRequiresAdzunaCredentialReference()
    {
        var manager = new JobBoardUrlManager(
            new TestSecretManager("unused"),
            new ConfigurationBuilder().Build());
        var provider = CreateAdzunaProvider();
        provider.CredentialReference = null;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.BuildUrlAsync(
                provider,
                CreateSearchCriterion("Software Engineer")));

        Assert.Contains("CredentialReference", exception.Message);
    }

    private static JobBoardProvider CreateAdzunaProvider()
    {
        return new JobBoardProvider
        {
            JobBoardName = "Adzuna",
            JobBoardApplicationId = "application-id",
            FeedUrl = "https://api.adzuna.com/v1/api/jobs/us/search",
            CredentialReference = "Adzuna-ApiKey",
            ExpectedResponseType = "json",
            IsEnabled = true
        };
    }

    private static JobSearchCriterion CreateSearchCriterion(string description)
    {
        return new JobSearchCriterion
        {
            JobSearchCriteriaId = 1,
            JobSearchCriteriaDescription = description
        };
    }

    private sealed class TestSecretManager : IKeyVaultSecretManager
    {
        private readonly string _secretValue;

        public TestSecretManager(string secretValue)
        {
            _secretValue = secretValue;
        }

        public string? RequestedSecretName { get; private set; }

        public Task<string> GetSecretValueAsync(
            string secretName,
            CancellationToken cancellationToken = default)
        {
            RequestedSecretName = secretName;
            return Task.FromResult(_secretValue);
        }
    }
}
