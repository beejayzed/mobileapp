using System;
using Newtonsoft.Json;

namespace Toggl.Ultrawave.Models
{
    internal sealed class ResponseError
    {
        public string Message { get; set; }

        [JsonConstructor]
        public ResponseError(string message)
        {
            this.Message = message;
        }
    }
}
