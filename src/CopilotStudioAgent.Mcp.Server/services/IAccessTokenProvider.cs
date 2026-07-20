namespace CopilotStudioAgent.Mcp.Server.Services;

public interface IAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}
