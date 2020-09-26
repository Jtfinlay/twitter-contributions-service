using Newtonsoft.Json;
using System;
using System.Globalization;

namespace TwitterContributions.Models
{
    public class Status
    {
        [JsonProperty("created_at")]
        public string CreatedAtStr { get; set; }

        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("in_reply_to_status_id")]
        public string InReplyToStatusId { get; set; }

        public DateTime CreatedAt
        {
            get
            {
                return DateTime.ParseExact(this.CreatedAtStr, "ddd MMM dd HH:mm:ss zzz yyyy", null);
            }
        }
    }
}
