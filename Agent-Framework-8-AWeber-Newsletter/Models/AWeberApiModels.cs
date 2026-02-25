using System.Text.Json.Serialization;

namespace Agent_Framework_8_AWeber_Newsletter.Models
{
    public class AWeberAccountsResponse
    {
        [JsonPropertyName("entries")]
        public List<AWeberAccount> Entries { get; set; } = new();
    }

    public class AWeberAccount
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("lists_collection_link")]
        public string ListsCollectionLink { get; set; } = string.Empty;
    }

    public class AWeberListsResponse
    {
        [JsonPropertyName("entries")]
        public List<AWeberList> Entries { get; set; } = new();
    }

    public class AWeberList
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class AWeberBroadcastResponse
    {
        [JsonPropertyName("broadcast_id")]
        public int BroadcastId { get; set; }

        [JsonPropertyName("self_link")]
        public string SelfLink { get; set; } = string.Empty;

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
