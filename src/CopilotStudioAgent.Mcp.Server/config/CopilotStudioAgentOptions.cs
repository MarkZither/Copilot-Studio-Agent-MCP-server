namespace CopilotStudioAgent.Mcp.Server.Config;

public sealed class CopilotStudioAgentOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string TenantId { get; set; } = string.Empty;
    public string TokenId { get; set; } = string.Empty;
    public string TokenSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = "https://api.botframework.com/.default";
    public string AgentEndpoint { get; set; } = "/api/messages";
    public string? AccessToken { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
