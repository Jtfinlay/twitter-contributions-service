using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using TwitterContributions.Models;

namespace TwitterContributions
{
    public class TwitterClient
    {
        // This singleton could be done better...
        private static HttpClient _priv_client;

        private static HttpClient client
        {
            get
            {
                if (_priv_client == null)
                {
                    string bearerToken = Environment.GetEnvironmentVariable("TwitterBearerToken", EnvironmentVariableTarget.Process);

                    _priv_client = new HttpClient();
                    _priv_client.BaseAddress = new Uri("https://api.twitter.com/1.1/");
                    _priv_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                }
                return _priv_client;
            }
        }

        public static async Task<List<Status>> FetchUserActivityInPastYear(string userName, ILogger log)
        {
            List<Status> statuses = new List<Status>();
            DateTime targetDate = DateTime.Now.AddYears(-1);

            ulong? nextId = null;
            List<Status> result;
            do
            {
                result = await FetchUserTimeline(userName, log, nextId);
                result = result.Where(s => s.CreatedAt >= targetDate).ToList();

                if (result.Count > 0)
                {
                    statuses.AddRange(result);
                    nextId = result.Last().Id - 1;
                }
                else
                {
                    nextId = null;
                }
            } while (nextId != null);

            return statuses;
        }

        public static async Task<List<Status>> FetchUserLikesInPastYear(string userName, ILogger log)
        {
            List<Status> statuses = new List<Status>();
            DateTime targetDate = DateTime.Now.AddYears(-1);

            ulong? nextId = null;
            List<Status> result;
            do
            {
                result = await FetchUserLikes(userName, log, nextId);
                result = result.Where(s => s.CreatedAt >= targetDate).ToList();

                if (result.Count > 0)
                {
                    statuses.AddRange(result);
                    nextId = result.Last().Id - 1;
                }
                else
                {
                    nextId = null;
                }
            } while (nextId != null);

            return statuses;
        }

        private static async Task<List<Status>> FetchUserTimeline(string userName, ILogger log, ulong? maxId = null)
        {
            try
            {
                using var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                string maxIdParam = (maxId == null) ? string.Empty : $"max_id={maxId}";
                HttpResponseMessage response = await client.GetAsync(
                    $"statuses/user_timeline.json?screen_name={userName}&count=200&include_rts=true&trim_user=true&{maxIdParam}",
                    cancellationToken.Token
                );
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<List<Status>>();
            }
            catch (Exception e)
            {
                log.LogError("FetchUserTimeline failed.", e);
                throw;
            }
        }

        private static async Task<List<Status>> FetchUserLikes(string userName, ILogger log, ulong? maxId = null)
        {
            try
            {
                using var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                string maxIdParam = (maxId == null) ? string.Empty : $"max_id={maxId}";
                HttpResponseMessage response = await client.GetAsync(
                    $"favorites/list.json?screen_name={userName}&count=200&include_entities=true&{maxIdParam}",
                    cancellationToken.Token
                );
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<List<Status>>();
            }
            catch (Exception e)
            {
                log.LogError("FetchUserLikes failed.", e);
                throw;
            }
        }
    }
}
