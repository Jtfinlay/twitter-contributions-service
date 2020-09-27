using Microsoft.Azure.Cosmos.Table;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using TwitterContributions.Models;
using Newtonsoft.Json;

namespace TwitterContributions
{
    public static class SubmitFetchRequest
    {
        [FunctionName("SubmitFetchRequest")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Queue("submissions"), StorageAccount("AzureWebJobsStorage")] ICollector<string> queue,
            ILogger log)
        {
            log.LogInformation("C# SubmitFetchRequest HTTP trigger function processed a request.");

            string username = req.Query["username"];

            if (string.IsNullOrWhiteSpace(username))
            {
                return new BadRequestObjectResult("Please pass a username on the query string");
            }

            username = username.ToLower();

            // Check if data already exists in storage and is recent
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("users");
            table.CreateIfNotExists();

            var query = table.Execute(TableOperation.Retrieve<UserEntity>(username, username));
            if (query.Result is UserEntity entity && entity.Timestamp >= DateTime.Now.Subtract(Constants.UserExpiry))
            {
                var summary = JsonConvert.DeserializeObject<UserSummary>(entity.Entity);
                summary.RunTime = entity.Timestamp;
                return new OkObjectResult(summary);
            }

            // Add to queue
            if (!string.IsNullOrWhiteSpace(username))
            {
                queue.Add(username);
            }

            // Check if we've hit rate limit
            table = tableClient.GetTableReference("rateLimit");
            table.CreateIfNotExists();

            var utc = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            query = table.Execute(TableOperation.Retrieve<RateLimitReset>("pk", "rk"));
            if (query.Result is RateLimitReset rateReset && utc.AddSeconds(rateReset.ResetTime) > DateTime.UtcNow)
            {
                return new RateLimitedActionResult(rateReset.ResetTime);
            }

            return (ActionResult)new AcceptedResult();
        }
    }
}
