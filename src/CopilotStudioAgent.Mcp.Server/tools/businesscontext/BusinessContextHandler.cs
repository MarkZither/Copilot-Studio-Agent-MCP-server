using System.ComponentModel;
using System.Text.Json;
using CopilotStudioAgent.Mcp.Server.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace CopilotStudioAgent.Mcp.Server.Tools.BusinessContext;

[McpServerToolType]
internal sealed class BusinessContextToolHandler(
    ILogger<BusinessContextToolHandler> logger,
    CopilotStudioAgentClient agentClient)
{
    private readonly ILogger<BusinessContextToolHandler> _logger = logger;
    private readonly CopilotStudioAgentClient _agentClient = agentClient;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    [McpServerTool(Name = "businesscontext_search")]
    [Description("Calls a Copilot Studio agent to search business context from sources like SharePoint libraries.")]
    public async Task<string> SearchBusinessContextsAsync(
        [Description("Query string for filtering business contexts.")] string query,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _agentClient.SearchAsync(query, ct).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Business context search failed for query {Query}", query);
            return JsonSerializer.Serialize(new { error = "api_error", message = ex.Message }, _jsonOptions);
        }
    }
}
