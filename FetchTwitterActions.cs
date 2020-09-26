using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitterContributions.Models;

namespace TwitterContributions.Function
{
    public static class FetchTwitterActions
    {
        [FunctionName("FetchTwitterActions")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# FetchTwitterActions trigger function processed a request.");

            string username = req.Query["username"];

            var result = await TwitterClient.FetchUserActivityInPastYear(username, log);
            var likes = await TwitterClient.FetchUserLikesInPastYear(username, log);

            var hashset = new Dictionary<string, DaySummary>();
            foreach(Status status in result)
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

            return new OkObjectResult(hashset.Values.ToList());
        }
    }
}
