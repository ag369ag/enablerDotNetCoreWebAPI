using Newtonsoft.Json;

namespace testASPWebAPI
{
    public class APIResponse
    {
        [JsonProperty("statusCode")]
        public int statusCode { get; set; }
        [JsonProperty("statusDescription")]
        public string statusDescription { get; set; }
        [JsonProperty("data")]
        public string data { get; set; }
    }
}
