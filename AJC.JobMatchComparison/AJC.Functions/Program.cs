using AJC.Functions.Managers;
using AJC.Functions.Managers.Interfaces;
using AJC.Functions.Repositories;
using AJC.Functions.Repositories.Interfaces;
using AJC.Functions.Services;
using AJC.Functions.Services.Interfaces;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using AJC.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<TokenCredential>(_ =>
            CreateAppRegistrationCredential(context.Configuration));
        services.AddSingleton(serviceProvider => CreateBlobServiceClient(
            context.Configuration,
            serviceProvider.GetRequiredService<TokenCredential>()));
        services.AddSingleton(serviceProvider => new Lazy<string>(
            () => GetDatabaseConnectionString(
                context.Configuration,
                serviceProvider.GetRequiredService<TokenCredential>()),
            LazyThreadSafetyMode.ExecutionAndPublication));
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            options.UseSqlServer(serviceProvider.GetRequiredService<Lazy<string>>().Value));
        services.AddHttpClient<IJobBoardProviderClient, JobBoardProviderClient>();
        services.AddSingleton<IKeyVaultSecretManager, KeyVaultSecretManager>();
        services.AddSingleton<IJobBoardUrlManager, JobBoardUrlManager>();
        services.AddScoped<IJobExecutionLockService, BlobJobExecutionLockService>();
        services.AddScoped<IJobRetrievalWorkflowManager, JobRetrievalWorkflowManager>();
        services.AddScoped<IJobRetrievalWorkflowRepository, JobRetrievalWorkflowRepository>();

    })
    .Build();

host.Run();

static TokenCredential CreateAppRegistrationCredential(IConfiguration configuration)
{
    var tenantId = configuration["AZURE_TENANT_ID"]
        ?? configuration["AzureAd:TenantId"];
    var clientId = configuration["AZURE_CLIENT_ID"]
        ?? configuration["AzureAd:ClientId"];
    var clientSecret = configuration["AZURE_CLIENT_SECRET"]
        ?? configuration["AzureAd:ClientSecret"];

    if (string.IsNullOrWhiteSpace(tenantId)
        || string.IsNullOrWhiteSpace(clientId)
        || string.IsNullOrWhiteSpace(clientSecret))
    {
        throw new InvalidOperationException(
            "Configure the shared app registration with AZURE_TENANT_ID, AZURE_CLIENT_ID, " +
            "and AZURE_CLIENT_SECRET, or with AzureAd:TenantId, AzureAd:ClientId, and " +
            "AzureAd:ClientSecret.");
    }

    return new ClientSecretCredential(tenantId, clientId, clientSecret);
}

static string GetDatabaseConnectionString(
    IConfiguration configuration,
    TokenCredential credential)
{
    var connectionString = configuration.GetConnectionString("JobMatchComparisonContext");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        return connectionString;
    }

    var vaultUri = configuration["OperationsKeyVault:VaultUri"];
    var secretName = configuration["Sql:ConnectionStringSecretName"];

    var clientSecret = configuration["AzureAd:ClientSecret"];

    if (string.IsNullOrWhiteSpace(vaultUri) || string.IsNullOrWhiteSpace(secretName))
    {
        throw new InvalidOperationException(
            "Configure either ConnectionStrings__JobMatchComparisonContext for local execution " +
            "or both OperationsKeyVault__vaultUri and Sql__connectionStringSecretName for Azure.");
    }

    var secretClient = new SecretClient(new Uri(vaultUri), credential);
    var secret = secretClient.GetSecret(secretName);

    if (string.IsNullOrWhiteSpace(secret.Value.Value))
    {
        throw new InvalidOperationException(
            $"The Key Vault secret '{secretName}' does not contain a SQL connection string.");
    }

    return secret.Value.Value;
}

static BlobServiceClient CreateBlobServiceClient(
    IConfiguration configuration,
    TokenCredential credential)
{
    var connectionString = configuration.GetConnectionString("JobMatchStorage")
        ?? configuration["JobMatchStorage:ConnectionString"];

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        return new BlobServiceClient(connectionString);
    }

    var serviceUri = configuration["JobMatchStorage:ServiceUri"];

    if (!string.IsNullOrWhiteSpace(serviceUri))
    {
        return new BlobServiceClient(new Uri(serviceUri), credential);
    }

    var accountName = configuration["JobMatchStorage:AccountName"];

    if (!string.IsNullOrWhiteSpace(accountName))
    {
        return new BlobServiceClient(
            new Uri($"https://{accountName}.blob.core.windows.net"),
            credential);
    }

    var hostStorageConnectionString = configuration["AzureWebJobsStorage"];

    if (!string.IsNullOrWhiteSpace(hostStorageConnectionString))
    {
        return new BlobServiceClient(hostStorageConnectionString);
    }

    throw new InvalidOperationException(
        "ConnectionStrings__JobMatchStorage, JobMatchStorage__serviceUri, or " +
        "JobMatchStorage__accountName must be configured.");
}
