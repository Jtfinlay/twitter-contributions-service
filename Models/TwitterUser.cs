using Newtonsoft.Json;

namespace TwitterContributions.Models
{
    public class TwitterUser
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("profile_image_url_https")]
        public string ProfileImage { get; set; }
    }
}
