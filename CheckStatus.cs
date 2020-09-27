using Microsoft.Azure.Cosmos.Table;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using TwitterContributions.Models;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitterContributions
{
    public static class CheckStatus
    {
        [FunctionName("CheckStatus")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# CheckStatus HTTP trigger function processed a request.");

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
                var summary = JsonConvert.DeserializeObject<UserSummary>(entity.Entity);
                summary.RunTime = entity.Timestamp;
                return new OkObjectResult(summary);
            }

            return (ActionResult)new NotFoundResult();
        }
    }
}
