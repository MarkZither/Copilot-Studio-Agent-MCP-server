using System.Net;
using System.Net.Http;
using CopilotStudioAgent.Mcp.Server.Config;
using CopilotStudioAgent.Mcp.Server.Services;
using CopilotStudioAgent.Mcp.Server.Tools.BusinessContext;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TUnit.Assertions;

namespace CopilotStudioAgent.Mcp.Server.Tests.Tools.BusinessContext;

public sealed class BusinessContextHandlerTests
{
    [Test]
    public async Task SearchAsync_UsesConfiguredOAuthTokenAndEndpoint()
    {
        var handler = new StubHttpMessageHandler();
        var client = new CopilotStudioAgentClient(
            new HttpClient(handler),
            Options.Create(new CopilotStudioAgentOptions
            {
                BaseUrl = "https://contoso.test",
                TenantId = "tenant-id",
                TokenId = "client-id",
                TokenSecret = "client-secret",
                Scope = "https://api.botframework.com/.default",
                AgentEndpoint = "/api/messages"
            }),
            new StubAccessTokenProvider("token-123"),
            NullLogger<CopilotStudioAgentClient>.Instance);

        var result = await client.SearchAsync("sales context", CancellationToken.None).ConfigureAwait(false);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Authorization.Should().NotBeNull();
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("token-123");
        handler.LastRequest.RequestUri.Should().Be(new Uri("https://contoso.test/api/messages"));
        handler.LastBody.Should().Contain("sales context");
        result.Should().Contain("sales context");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"text\":\"sales context\"}")
            };
        }
    }

    private sealed class StubAccessTokenProvider : IAccessTokenProvider
    {
        private readonly string _token;

        public StubAccessTokenProvider(string token)
        {
            _token = token;
        }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_token);
        }
    }
}
