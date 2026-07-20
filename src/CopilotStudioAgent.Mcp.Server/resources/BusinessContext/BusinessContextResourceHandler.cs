using System.ComponentModel;
using System.Text.Json;
using CopilotStudioAgent.Mcp.Server.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace CopilotStudioAgent.Mcp.Server.Resources.BusinessContext;

[McpServerResourceType]
internal sealed class BusinessContextResourceHandler(
    ILogger<BusinessContextResourceHandler> logger,
    IOptions<CopilotStudioAgentOptions> scopeOptions)
{
    private readonly ILogger<BusinessContextResourceHandler> _logger = logger;
    private readonly IOptions<CopilotStudioAgentOptions> _copilotStudioAgentOptions = scopeOptions;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    [McpServerResource(UriTemplate = "copilotstudioagent://businesscontext", Name = "businesscontext")]
    [Description("Search for business context.")]
    public async Task<string> GetBusinessContextAsync(CancellationToken ct = default)
    {
        try
        {
            var result = "";

            return JsonSerializer.Serialize(result, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Terrible Error: {Message}", ex.Message);
            return JsonSerializer.Serialize(new { error = "api_error", message = ex.Message }, _jsonOptions);
        }
    }
}
