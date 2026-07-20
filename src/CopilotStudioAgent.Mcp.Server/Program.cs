using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CopilotStudioAgent.Mcp.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var transport = Environment.GetEnvironmentVariable("COPILOTSTUDIOAGENT_MCP_TRANSPORT") ?? "stdio";

if (transport is not ("stdio" or "http" or "both"))
{
    Console.Error.WriteLine(
        $"Invalid COPILOTSTUDIOAGENT_MCP_TRANSPORT value: '{transport}'. Valid values: stdio, http, both.");
    return 1;
}

if (transport == "stdio")
{
    // Headless / CI path — no admin sidecar, no HTTP listener needed.
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.AddConsole(options =>
        options.LogToStandardErrorThreshold = LogLevel.Trace);

    builder.Configuration.AddInMemoryCollection(MapCopilotStudioAgentEnvVars());
    builder.Services
        .AddCopilotStudioAgentClient()
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(Assembly.GetExecutingAssembly())
        .WithResourcesFromAssembly(Assembly.GetExecutingAssembly());

    var host = builder.Build();
    host.Services.GetRequiredService<ILogger<Program>>()
        .LogInformation("Logging for fun.");
    await host.RunAsync().ConfigureAwait(false);
}
else
{
    var mcpPort = int.TryParse(
        Environment.GetEnvironmentVariable("COPILOTSTUDIOAGENT_MCP_HTTP_PORT"), out var p) ? p : 3000;

    var builder = WebApplication.CreateBuilder(args);

    if (transport is "stdio" or "both")
    {
        builder.Logging.AddConsole(options =>
            options.LogToStandardErrorThreshold = LogLevel.Trace);
    }

    builder.Configuration.AddInMemoryCollection(MapCopilotStudioAgentEnvVars());

    var mcpBuilder = builder.Services
        .AddCopilotStudioAgentClient()
        .AddMcpServer()
        .WithToolsFromAssembly(Assembly.GetExecutingAssembly())
        .WithResourcesFromAssembly(Assembly.GetExecutingAssembly());

    if (transport == "stdio")
    {
        // stdio + admin enabled: stdio MCP transport alongside the admin Kestrel listener.
        mcpBuilder.WithStdioServerTransport();
    }
    else
    {
        mcpBuilder.WithHttpTransport();
        if (transport == "both")
        {
            mcpBuilder.WithStdioServerTransport();
        }
    }

    // Explicit Kestrel listeners so the admin port can be added alongside the MCP port.
    // Once Listen() is called explicitly, ASPNETCORE_URLS is no longer honoured by Kestrel,
    // so we read and apply it manually.
    builder.WebHost.ConfigureKestrel(opts =>
    {
        if (transport != "stdio")
        {
            var aspnetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            if (string.IsNullOrEmpty(aspnetUrls))
            {
                opts.ListenAnyIP(mcpPort);
            }
            else
            {
                foreach (var urlString in aspnetUrls.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!Uri.TryCreate(urlString.Trim(), UriKind.Absolute, out var listenUri))
                    {
                        continue;
                    }

                    var address = listenUri.Host is "*" or "+" or "0.0.0.0"
                        ? IPAddress.Any
                        : IPAddress.Parse(listenUri.Host);
                    opts.Listen(address, listenUri.Port);
                }
            }
        }
    });

    var app = builder.Build();

    if (transport != "stdio")
    {
        var authToken = app.Configuration["COPILOTSTUDIOAGENT_MCP_HTTP_AUTH_TOKEN"]
            ?? Environment.GetEnvironmentVariable("COPILOTSTUDIOAGENT_MCP_HTTP_AUTH_TOKEN");

        if (string.IsNullOrEmpty(authToken))
        {
            app.Logger.LogWarning(
                "HTTP authentication is disabled. Set COPILOTSTUDIOAGENT_MCP_HTTP_AUTH_TOKEN to enable.");

            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
            app.MapMcp();
        }
        else
        {
            var authTokenBytes = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(authToken));

            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/mcp"))
                {
                    var header = ctx.Request.Headers.Authorization.ToString();
                    if (!IsAuthorized(header, authTokenBytes))
                    {
                        ctx.Response.StatusCode = 401;
                        return;
                    }
                }

                await next(ctx).ConfigureAwait(false);
            });

            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
            app.MapMcp();
        }
    }

    await app.RunAsync().ConfigureAwait(false);
}

return 0;

static bool IsAuthorized(string authorizationHeader, ReadOnlyMemory<byte> expected)
{
    const string bearerPrefix = "Bearer ";
    if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.Ordinal))
    {
        return false;
    }

    var provided = Encoding.UTF8.GetBytes(authorizationHeader[bearerPrefix.Length..]);
    return provided.Length == expected.Length
        && CryptographicOperations.FixedTimeEquals(expected.Span, provided);
}

static Dictionary<string, string?> MapCopilotStudioAgentEnvVars()
{
    var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    MapOptional(map, "COPILOTSTUDIOAGENT_BASE_URL", "CopilotStudioAgent:BaseUrl");
    MapOptional(map, "COPILOTSTUDIOAGENT_TENANT_ID", "CopilotStudioAgent:TenantId");
    MapOptional(map, "COPILOTSTUDIOAGENT_TOKEN_ID", "CopilotStudioAgent:TokenId");
    MapOptional(map, "COPILOTSTUDIOAGENT_TOKEN_SECRET", "CopilotStudioAgent:TokenSecret");
    MapOptional(map, "COPILOTSTUDIOAGENT_SCOPE", "CopilotStudioAgent:Scope");
    MapOptional(map, "COPILOTSTUDIOAGENT_AGENT_ENDPOINT", "CopilotStudioAgent:AgentEndpoint");
    MapOptional(map, "COPILOTSTUDIOAGENT_ACCESS_TOKEN", "CopilotStudioAgent:AccessToken");
    MapOptional(map, "COPILOTSTUDIOAGENT_TIMEOUT_SECONDS", "CopilotStudioAgent:TimeoutSeconds");

    MapOptional(map, "COPILOTSTUDIOAGENT_PLACEHOLDER", "VectorSearch:Enabled");

    return map;
}

static void MapOptional(Dictionary<string, string?> map, string envVar, string configKey)
{
    var value = Environment.GetEnvironmentVariable(envVar);
    if (value is not null)
    {
        map[configKey] = value;
    }
}

public partial class Program
{
    private static readonly Regex _scopeEntryRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
}
