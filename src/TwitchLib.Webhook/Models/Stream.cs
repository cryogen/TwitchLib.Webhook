using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitchLib.Webhook.Models
{
    using System.Text.Json.Serialization;

    public class Stream
    {

        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonProperty("user_id")]
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonProperty("game_id")]
        [JsonPropertyName("game_id")]
        public string GameId { get; set; }

        [JsonProperty("community_ids")]
        [JsonPropertyName("community_ids")] 
        public IList<string> CommunityIds { get; set; }

        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("viewer_count")]
        [JsonPropertyName("viewer_count")]
        public int ViewerCount { get; set; }

        [JsonProperty("started_at")]
        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonProperty("language")]
        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonProperty("thumbnail_url")]
        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

    }
}
