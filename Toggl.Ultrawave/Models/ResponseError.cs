using Newtonsoft.Json;

namespace Toggl.Ultrawave.Models
{
    internal sealed class ResponseError
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
