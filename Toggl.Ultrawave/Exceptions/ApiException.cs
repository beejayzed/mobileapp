using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.Exceptions
{
    public class ApiException : Exception
    {
        internal IRequest Request { get; }

        internal IResponse Response { get; }

        public string LocalizedApiErrorMessage { get; }

        private readonly string message;

        internal ApiException(IRequest request, IResponse response, string defaultMessage)
        {
            Request = request;
            Response = response;
            LocalizedApiErrorMessage = getLocalizedMessageFromResponse(response) ?? defaultMessage;
            this.message = defaultMessage;
        }

        public override string ToString()
            => $"{GetType().Name} for request {Request.HttpMethod} {Request.Endpoint}: "
                + $"Response: "
                + $"(Status: [{(int)Response.StatusCode} {Response.StatusCode}]) "
                + $"(Headers: [{SerializeHeaders(Response.Headers)}]) "
                + $"(Body: {Response.RawData}) "
                + $"(Message: {message})";

        internal static string SerializeHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
            => String.Join(", ", headers.Select(pair => $"'{pair.Key}': [{String.Join(", ", pair.Value.Select(v => $"'{v}'").ToArray())}]").ToArray());

        public override string Message => ToString();

        private string getLocalizedMessageFromResponse(IResponse response)
        {
            if (response.IsJson)
            {
                try
                {
                    var serializer = new JsonSerializer();
                    var error = serializer.Deserialize<ResponseError>(response.RawData);
                    return error.Message;
                }
                catch (DeserializationException<ResponseError>)
                {
                    return null;
                }
            }
            else if (response.IsText)
            {
                return response.RawData;
            }
            return null;
        }
    }
}
