using Microsoft.Azure.Cosmos.Table;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TwitterContributions.Models;
using System.Collections.Generic;
using System.Linq;
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

            // Check if data already exists in storage and is recent
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("users");
            table.CreateIfNotExists();

            var query = table.Execute(TableOperation.Retrieve<UserEntity>(username, username));
            if (query.Result is UserEntity entity && entity.Timestamp >= DateTime.Now.Subtract(Constants.UserExpiry))
            {
                var summary = new UserSummary
                {
                    RunTime = entity.Timestamp,
                    Summary = JsonConvert.DeserializeObject<List<DaySummary>>(entity.Summary)
                };
                return new OkObjectResult(summary);
            }

            // Add to queue
            if (!string.IsNullOrWhiteSpace(username))
            {
                queue.Add(username);
            }

            return (ActionResult)new AcceptedResult();
        }
    }
}
