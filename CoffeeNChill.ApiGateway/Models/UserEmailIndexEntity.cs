
using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.ApiGateway.Models
{
    public class UserEmailIndexEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Email";

        public string RowKey { get; set; } = string.Empty;

        public string Role { get; set; } = "Customer";

        public string State { get; set; } = "Pending";

        public DateTimeOffset CreatedDate { get; set; }
            = DateTimeOffset.UtcNow;

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}
