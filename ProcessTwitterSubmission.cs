using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using TwitterContributions.Models;

namespace TwitterContributions
{
    public static class ProcessTwitterSubmission
    {
        [FunctionName("ProcessTwitterSubmission")]
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
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("rateLimit");
            table.CreateIfNotExists();

            RateLimitReset rateReset = null;
            var utc = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            var query = table.Execute(TableOperation.Retrieve<RateLimitReset>("pk", "rk"));
            if (query.Result is RateLimitReset tmp)
            {
                rateReset = tmp;
                if (utc.AddSeconds(rateReset.ResetTime) > DateTime.UtcNow)
                {
                    log.LogInformation($"Have not reached rate limit timestamp of {utc.AddSeconds(rateReset.ResetTime)}. Current: {DateTime.UtcNow}.");
                    throw new HttpResponseException((HttpStatusCode)429);
                }
            }

            // Lookup data from Twitter
            List<Status> result, likes;
            TwitterUser userDetails;
            try
            {
                result = await TwitterClient.FetchUserActivityInPastYear(username, log);
                likes = await TwitterClient.FetchUserLikesInPastYear(username, log);
                userDetails = await TwitterClient.FetchUserDetails(username, log);
            }
            catch (HttpResponseException e)
            {
                if (e.Response.StatusCode == (HttpStatusCode)429)
                {
                    if (rateReset == null)
                    {
                        rateReset = new RateLimitReset();
                        rateReset.PartitionKey = "pk";
                        rateReset.RowKey = "rk";
                    }
                    if (e.Response.Headers.TryGetValues("x-rate-limit-reset", out IEnumerable<string> resetTime))
                    {
                        var newTime = Double.Parse(resetTime.First());
                        if (newTime > rateReset.ResetTime)
                        {
                            rateReset.ResetTime = newTime;
                            table.Execute(TableOperation.InsertOrReplace(rateReset));
                            log.LogInformation("Updated rateLimit to " + newTime);
                        }
                    }
                }

                throw;
            }

            var hashset = new Dictionary<string, DaySummary>();
            foreach (Status status in result)
            {
                string date = status.CreatedAt.ToString("yyyy-MM-dd");
                if (!hashset.TryGetValue(date, out DaySummary summary))
                {
                    summary = new DaySummary { Date = date };
                }
                summary.StatusCount++;
                hashset[date] = summary;
            }

            foreach (Status status in likes)
            {
                string date = status.CreatedAt.ToString("yyyy-MM-dd");
                if (!hashset.TryGetValue(date, out DaySummary summary))
                {
                    summary = new DaySummary { Date = date };
                }
                summary.LikeCount++;
                hashset[date] = summary;
            }

            // Write results to storage. Bindings don't support update, apparently :/
            table = tableClient.GetTableReference("users");
            table.CreateIfNotExists();

            if (user == null)
            {
                user = new UserEntity();
                user.PartitionKey = username;
                user.RowKey = username;
            }

            user.Timestamp = DateTime.Now;
            user.Entity = JsonConvert.SerializeObject(new UserSummary
            {
                Summary = hashset.Values.ToList(),
                UserDetails = userDetails
            });
            table.Execute(TableOperation.InsertOrReplace(user));
        }
    }
}
