using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AJC.Functions
{
    public static class CreateJobMatches
    {
        [FunctionName("CreateJobMatches")]
        public static async Task<IActionResult> RunHttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        [FunctionName("CreateJobMatchesTimer")]
        public static async Task RunTimerTrigger(
            [TimerTrigger("0 0 6 * * *")] TimerInfo timer, ILogger log)
        {
            log.LogInformation(
                "CreateJobMatches timer trigger executed at: {ExecutionTime}",
                DateTime.UtcNow);

            if (timer.IsPastDue)
            {
                log.LogWarning("CreateJobMatches timer trigger is running late.");
            }

            // Add the job-search workflow here.
            await Task.CompletedTask;
        }
    }
}
