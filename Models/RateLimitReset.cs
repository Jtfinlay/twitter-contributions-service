using Microsoft.Azure.Cosmos.Table;

namespace TwitterContributions.Models
{
    public class RateLimitReset : TableEntity
    {
        public double ResetTime { get; set; }
    }
}
