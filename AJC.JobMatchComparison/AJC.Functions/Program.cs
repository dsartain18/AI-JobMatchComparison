using AJC.Functions.Managers;
using AJC.Functions.Managers.Interfaces;
using AJC.Functions.Services;
using AJC.Functions.Services.Interfaces;
using Azure.Identity;
using Azure.Storage.Blobs;
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
        services.AddSingleton<IJobExecutionLockService, BlobJobExecutionLockService>();
        services.AddSingleton<IJobRetrievalWorkflowManager, JobRetrievalWorkflowManager>();
    })
    .Build();

host.Run();

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
