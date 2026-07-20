# CopilotStudioAgent MCP Server

> Minimal VS Code extension for registering the CopilotStudioAgent MCP server with GitHub Copilot and other MCP-compatible clients.

## What It Does

This extension registers a [Model Context Protocol](https://modelcontextprotocol.io/) server definition for the CopilotStudioAgent .NET server. Once configured, Copilot can invoke the bundled server binary with your agent endpoint and credentials.

## Prerequisites

- VS Code 1.120 or later
- A built CopilotStudioAgent server binary placed in the extension's `bin` folder
- A configured Copilot Studio Agent URL and API token

## Configuration

Open Settings and configure:

- `copilotstudioagent.url`
- `copilotstudioagent.tokenId`
- `copilotstudioagent.tokenSecret`

## Development

From the extension folder:

```bash
npm install
npm run build
npm test
```

## Notes

This template intentionally omits any admin panel, status bar UI, or sidecar process. Those can be added back later once the product direction is clear.
