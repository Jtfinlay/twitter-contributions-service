using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitterContributions.Models;

namespace TwitterContributions
{
    public static class FetchTwitterTimeline
    {
        [FunctionName("FetchTwitterTimeline")]
        public static async Task Run(
            [QueueTrigger("submissions", Connection = "AzureWebJobsStorage")] string username,
            [Table("users", "{queueTrigger}", "{queueTrigger}", Connection = "AzureWebJobsStorage")] UserEntity user,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {username}");

            // Check if already run in last x amount of time
            if (user?.Timestamp > DateTime.Now.Subtract(Constants.UserExpiry))
            {
                log.LogInformation($"Username '{username}' was updated in last hour. Exit early.");
                return;
            }

            // Check if we've hit rate limit

            // Lookup data from Twitter
            var result = await TwitterClient.FetchUserActivityInPastYear(username, log);
            var likes = await TwitterClient.FetchUserLikesInPastYear(username, log);

            var hashset = new Dictionary<string, DaySummary>();
            foreach (Status status in result)
            {
                string date = status.CreatedAt.ToString("yyyy:MM:dd");
                if (!hashset.TryGetValue(date, out DaySummary summary))
                {
                    summary = new DaySummary { Date = date };
                }
                summary.StatusCount++;
                hashset[date] = summary;
            }

            foreach (Status status in likes)
            {
                string date = status.CreatedAt.ToString("yyyy:MM:dd");
                if (!hashset.TryGetValue(date, out DaySummary summary))
                {
                    summary = new DaySummary { Date = date };
                }
                summary.LikeCount++;
                hashset[date] = summary;
            }

            // Write results to storage. Bindings don't support update, apparently :/

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("users");
            table.CreateIfNotExists();

            if (user == null)
            {
                user = new UserEntity();
                user.PartitionKey = username;
                user.RowKey = username;
            }

            user.Timestamp = DateTime.Now;
            user.Summary = JsonConvert.SerializeObject(hashset.Values.ToList());
            table.Execute(TableOperation.InsertOrReplace(user));
        }
    }
}
