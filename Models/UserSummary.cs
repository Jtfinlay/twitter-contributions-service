using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TwitterContributions.Models
{
    public class UserSummary
    {
        [JsonProperty("run_time")]
        public DateTimeOffset RunTime { get; set; }

        [JsonProperty("summary")]
        public List<DaySummary> Summary { get; set; }

        [JsonProperty("user_details")]
        public TwitterUser UserDetails { get; set; }
    }

    public class DaySummary
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("status_count")]
        public int StatusCount { get; set; }

        [JsonProperty("like_count")]
        public int LikeCount { get; set; }
    }
}
