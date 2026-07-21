using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AJC.Functions
{
    public class CreateJobMatches
    {
        private readonly ILogger<CreateJobMatches> _logger;

        public CreateJobMatches(ILogger<CreateJobMatches> logger)
        {
            _logger = logger;
        }

        [Function("CreateJobMatches")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "get",
            Route = null)]
        HttpRequestData req)
        {
            _logger.LogInformation(
                "C# HTTP trigger function processed a request.");

            var query =
                QueryHelpers.ParseQuery(req.Url.Query);

            string? name = query.TryGetValue("name", out var queryName)
                ? queryName.FirstOrDefault()
                : null;

            if (string.IsNullOrEmpty(name))
            {
                string requestBody =
                    await new StreamReader(req.Body).ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    dynamic? data =
                        JsonConvert.DeserializeObject(requestBody);

                    name = data?.name;
                }
            }

            string message = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            var response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteStringAsync(message);

            return response;
        }

        [Function("CreateJobMatchesTimer")]
        public async Task RunAsync(
        [TimerTrigger("0 0 6 * * *")] TimerInfo timer)
        {
            _logger.LogInformation(
                "CreateJobMatches timer trigger executed at: {ExecutionTime}",
                DateTime.UtcNow);

            if (timer.IsPastDue)
            {
                _logger.LogWarning(
                    "CreateJobMatches timer trigger is running late.");
            }

            // Add the job-search workflow here.
            await Task.CompletedTask;
        }
    }
}
