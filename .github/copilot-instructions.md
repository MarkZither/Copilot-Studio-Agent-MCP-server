# GitHub Copilot Instructions — CopilotStudioAgent-mcp-server

## Project Overview

**CopilotStudioAgent-mcp-server** is a Model Context Protocol (MCP) server implemented in .NET 10 / C# that provides access to a Copilot Studio Agent via M365 Agents SDK.

The server exposes 1 MCP tool and resource covering business context search.

## Tech Stack

- **Runtime**: .NET 10, C#
- **Protocol**: Model Context Protocol (MCP) — `stdio` and Streamable HTTP transports
- **Testing**: TUnit, Moq, FluentAssertions
- **Linting / formatting**: `dotnet format`, EditorConfig

## Project Structure

```
src/
  CopilotStudioAgent.Mcp.Server/                        # Main server project
    Server.cs                                  # MCP server entry point
    Program.cs
    config/                                    # Configuration manager
    resources/                                 # MCP resource handlers
    tools/                                     # MCP tool handlers
    utils/                                     # Shared utilities (errors, logger, rate-limit)
    validation/                                # Input validators
tests/
  CopilotStudioAgent.Mcp.Server.Tests/                  # TUnit test project
docs/
  architecture/decisions/                      # ADRs
  features/                                    # Feature specs
  migrations/                                  # Migration specs
  envisioning/                # Envisioning docs
```

## Code Conventions

- Follow `.github/docs/coding-guidelines.md` for all C# code.
- Use `async`/`await` throughout; avoid `.Result` or `.Wait()`.
- Validate all external inputs at system boundaries (API responses, tool arguments).
- Use structured logging (`ILogger<T>`) — never `Console.WriteLine` in production code.
- Keep tools and resources stateless; inject dependencies via constructor.
- Each tool handler lives in its own file under `tools/`.
- Each resource handler lives in its own file under `resources/`.

## Documentation

- Write specs using `docs/features/TEMPLATE.md`.
- Record architecture decisions using `docs/architecture/decisions/ADR-TEMPLATE.md`.
- Follow the style guide in `.github/instructions/documentation-style.instructions.md`.

## Git and Commits

⚠️ **CRITICAL: NEVER use `git commit --no-verify` or `git push --force-with-lease` or similar bypass flags.**
- Pre-commit hooks (Husky + dotnet format) exist for a reason: they enforce code quality
- If a hook fails: **fix the underlying issue** (run `dotnet format`, resolve errors) and retry
- If you bypass hooks, the PR will fail CI/CD checks anyway, and you will have violated the project's quality guardrails
- Always let hooks run. They protect the codebase.

## Agent Mode Instructions

- Use `@workspace` references when citing files.
- Prefer editing existing files over creating new ones.
- Do not add comments, docstrings, or type annotations to unchanged code.
- Security: follow OWASP Top 10; never log secrets or API tokens.
