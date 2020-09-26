using Newtonsoft.Json;
using System;

namespace TwitterContributions.Models
{
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
