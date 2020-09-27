using Microsoft.Azure.Cosmos.Table;

namespace TwitterContributions.Models
{
    public class UserEntity : TableEntity
    {
        public string Entity { get; set; }
    }
}
