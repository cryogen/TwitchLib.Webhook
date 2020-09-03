using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitchLib.Webhook.Models
{
    using System.Text.Json.Serialization;

    public class StreamData
    {

        [JsonProperty("data")]
        [JsonPropertyName("data")]
        public IList<Stream> Data { get; set; }
    }
}
