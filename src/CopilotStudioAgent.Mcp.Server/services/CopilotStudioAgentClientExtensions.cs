using CopilotStudioAgent.Mcp.Server.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CopilotStudioAgent.Mcp.Server.Services;

public static class CopilotStudioAgentClientExtensions
{
    public static IServiceCollection AddCopilotStudioAgentClient(this IServiceCollection services)
    {
        services.AddOptions<CopilotStudioAgentOptions>()
            .BindConfiguration("CopilotStudioAgent")
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<CopilotStudioAgentOptions>, CopilotStudioAgentOptionsValidator>();
        services.AddHttpClient<CopilotStudioAgentClient>();
        services.AddHttpClient<DefaultAccessTokenProvider>();
        services.AddSingleton<IAccessTokenProvider, DefaultAccessTokenProvider>();

        return services;
    }
}
