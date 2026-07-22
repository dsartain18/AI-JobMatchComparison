using AJC.Data.Models;
using AJC.Functions.Managers.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AJC.Functions.Managers;

public sealed class JobBoardUrlManager : IJobBoardUrlManager
{
    private const int DefaultAdzunaSearchPage = 1;
    private const int DefaultAdzunaResultsPerPage = 25;

    private readonly IKeyVaultSecretManager _secretManager;
    private readonly IConfiguration _configuration;

    public JobBoardUrlManager(
        IKeyVaultSecretManager secretManager,
        IConfiguration configuration)
    {
        _secretManager = secretManager ?? throw new ArgumentNullException(nameof(secretManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<string> BuildUrlAsync(
        JobBoardProvider provider,
        JobSearchCriterion searchCriterion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(searchCriterion);

        if (!string.Equals(provider.JobBoardName, "Adzuna", StringComparison.OrdinalIgnoreCase))
        {
            return provider.FeedUrl;
        }

        return await BuildAdzunaUrlAsync(provider, searchCriterion, cancellationToken);
    }

    private async Task<string> BuildAdzunaUrlAsync(
        JobBoardProvider provider,
        JobSearchCriterion searchCriterion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(provider.JobBoardApplicationId))
        {
            throw new InvalidOperationException(
                "The Adzuna provider requires JobBoardApplicationId to contain the Adzuna app_id.");
        }

        if (string.IsNullOrWhiteSpace(provider.CredentialReference))
        {
            throw new InvalidOperationException(
                "The Adzuna provider requires CredentialReference to contain its Key Vault secret name.");
        }

        var appKey = await _secretManager.GetSecretValueAsync(
            provider.CredentialReference,
            cancellationToken);
        var page = GetPositiveInteger("Adzuna:SearchPage", DefaultAdzunaSearchPage);
        var resultsPerPage = GetPositiveInteger(
            "Adzuna:ResultsPerPage",
            DefaultAdzunaResultsPerPage);
        var searchText = searchCriterion.JobSearchCriteriaDescription?.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            throw new InvalidOperationException(
                $"Job search criterion {searchCriterion.JobSearchCriteriaId} requires a description.");
        }

        var uriBuilder = new UriBuilder(
            $"{provider.FeedUrl.TrimEnd('/')}/{page}")
        {
            Query = string.Join(
                "&",
                CreateQueryParameter("app_id", provider.JobBoardApplicationId),
                CreateQueryParameter("app_key", appKey),
                CreateQueryParameter("results_per_page", resultsPerPage.ToString()),
                CreateQueryParameter("what", searchText))
        };

        return uriBuilder.Uri.AbsoluteUri;
    }

    private int GetPositiveInteger(string configurationKey, int defaultValue)
    {
        var configuredValue = _configuration[configurationKey];

        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return defaultValue;
        }

        if (!int.TryParse(configuredValue, out var value) || value <= 0)
        {
            throw new InvalidOperationException(
                $"Configuration value '{configurationKey}' must be a positive integer.");
        }

        return value;
    }

    private static string CreateQueryParameter(string name, string value)
    {
        return $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
    }
}
