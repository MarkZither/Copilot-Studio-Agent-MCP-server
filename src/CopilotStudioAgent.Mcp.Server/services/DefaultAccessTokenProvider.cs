using System.Net.Http.Headers;
using CopilotStudioAgent.Mcp.Server.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopilotStudioAgent.Mcp.Server.Services;

public sealed class DefaultAccessTokenProvider : IAccessTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly CopilotStudioAgentOptions _options;
    private readonly ILogger<DefaultAccessTokenProvider> _logger;

    public DefaultAccessTokenProvider(
        HttpClient httpClient,
        IOptions<CopilotStudioAgentOptions> options,
        ILogger<DefaultAccessTokenProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            return _options.AccessToken;
        }

        var tenant = string.IsNullOrWhiteSpace(_options.TenantId) ? "organizations" : _options.TenantId;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["client_id"] = _options.TokenId,
            ["client_secret"] = _options.TokenSecret,
            ["scope"] = _options.Scope,
            ["grant_type"] = "client_credentials"
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var payload = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var document = await System.Text.Json.JsonDocument.ParseAsync(payload, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            throw new InvalidOperationException("The OAuth token response did not contain an access_token property.");
        }

        return tokenElement.GetString() ?? throw new InvalidOperationException("The OAuth token response did not contain a usable access token.");
    }
}
