using AJC.Functions.Managers;
using AJC.Functions.Managers.Interfaces;
using AJC.Functions.Repositories;
using AJC.Functions.Repositories.Interfaces;
using AJC.Functions.Services;
using AJC.Functions.Services.Interfaces;
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
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(_ => CreateBlobServiceClient(context.Configuration));
        services.AddSingleton(_ => new Lazy<string>(
            () => GetDatabaseConnectionString(context.Configuration),
            LazyThreadSafetyMode.ExecutionAndPublication));
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            options.UseSqlServer(serviceProvider.GetRequiredService<Lazy<string>>().Value));
        services.AddHttpClient<IJobBoardProviderClient, JobBoardProviderClient>();
        services.AddScoped<IJobExecutionLockService, BlobJobExecutionLockService>();
        services.AddScoped<IJobRetrievalWorkflowManager, JobRetrievalWorkflowManager>();
        services.AddScoped<IJobRetrievalWorkflowRepository, JobRetrievalWorkflowRepository>();

    })
    .Build();

host.Run();

static string GetDatabaseConnectionString(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("JobMatchComparisonContext");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        return connectionString;
    }

    var vaultUri = configuration["OperationsKeyVault:vaultUri"];
    var secretName = configuration["Sql:connectionStringSecretName"];

    if (string.IsNullOrWhiteSpace(vaultUri) || string.IsNullOrWhiteSpace(secretName))
    {
        throw new InvalidOperationException(
            "Configure either ConnectionStrings__JobMatchComparisonContext for local execution " +
            "or both OperationsKeyVault__vaultUri and Sql__connectionStringSecretName for Azure.");
    }

    var secretClient = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
    var secret = secretClient.GetSecret(secretName);

    if (string.IsNullOrWhiteSpace(secret.Value.Value))
    {
        throw new InvalidOperationException(
            $"The Key Vault secret '{secretName}' does not contain a SQL connection string.");
    }

    return secret.Value.Value;
}

static BlobServiceClient CreateBlobServiceClient(IConfiguration configuration)
{
    var connectionString = configuration["AzureWebJobsStorage"];

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        return new BlobServiceClient(connectionString);
    }

    var accountName = configuration["AzureWebJobsStorage:accountName"];

    if (string.IsNullOrWhiteSpace(accountName))
    {
        throw new InvalidOperationException(
            "AzureWebJobsStorage or AzureWebJobsStorage__accountName must be configured.");
    }

    return new BlobServiceClient(
        new Uri($"https://{accountName}.blob.core.windows.net"),
        new DefaultAzureCredential());
}
