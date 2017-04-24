﻿
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Toggl.Ultrawave.Clients;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Ultrawave.Tests
{
    public class UserClientTests
    {
        public class TheGetMethod
        {
            private readonly IApiClient apiClient = Substitute.For<IApiClient>();
            private readonly IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

            [Fact]
            public async Task CreatesACallWithAppropriateHeaders()
            {
                const string username = "susancalvin@psychohistorian.museum";
                const string password = "theirobotmoviesucked123";
                const string expectedHeader = "c3VzYW5jYWx2aW5AcHN5Y2hvaGlzdG9yaWFuLm11c2V1bTp0aGVpcm9ib3Rtb3ZpZXN1Y2tlZDEyMw==";

                var userEndpoints = new Endpoints(ApiEnvironment.Staging).User;
                var client = new UserClient(userEndpoints, apiClient, serializer);

                await client.Get(username, password).Execute();
                await apiClient.Received().Send(Arg.Is<Request>(request => verifyAuthHeader(request, expectedHeader)));
            }

            private static bool verifyAuthHeader(Request request, string expectedHeader)
            {
                var authHeader = request.Headers.ToList().Single();

                if (authHeader.Type != HttpHeader.HeaderType.Auth) return false;
                if (authHeader.Value != expectedHeader) return false;

                return true;
            }
       }
    }
}
