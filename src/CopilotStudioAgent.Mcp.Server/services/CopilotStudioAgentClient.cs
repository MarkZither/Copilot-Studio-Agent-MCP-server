using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CopilotStudioAgent.Mcp.Server.Config;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopilotStudioAgent.Mcp.Server.Services;

internal sealed class CopilotStudioAgentClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly HttpClient _httpClient;
    private readonly CopilotStudioAgentOptions _options;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILogger<CopilotStudioAgentClient> _logger;

    public CopilotStudioAgentClient(
        HttpClient httpClient,
        IOptions<CopilotStudioAgentOptions> options,
        IAccessTokenProvider accessTokenProvider,
        ILogger<CopilotStudioAgentClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _accessTokenProvider = accessTokenProvider;
        _logger = logger;
    }

    public async Task<string> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var endpoint = _options.AgentEndpoint.StartsWith('/')
            ? _options.AgentEndpoint
            : $"/{_options.AgentEndpoint}";

        var uri = new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), endpoint.TrimStart('/'));
        var accessToken = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        var activity = new Activity
        {
            Type = "message",
            Text = query,
            ChannelId = "msteams",
            ServiceUrl = _options.BaseUrl,
        };

        var payload = JsonSerializer.Serialize(activity, _jsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Copilot Studio agent request failed with status {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"Copilot Studio agent request failed with status {(int)response.StatusCode}: {responseBody}");
        }

        return JsonSerializer.Serialize(new
        {
            query,
            response = responseBody,
            endpoint = uri.ToString()
        }, _jsonOptions);
    }
}
