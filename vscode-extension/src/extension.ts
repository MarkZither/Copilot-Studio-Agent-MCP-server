import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

export function activate(context: vscode.ExtensionContext): void {
    const outputChannel = vscode.window.createOutputChannel('CopilotStudioAgent MCP Server');
    context.subscriptions.push(outputChannel);

    outputChannel.appendLine('CopilotStudioAgent MCP Server: activating...');
    outputChannel.appendLine(`  platform: ${process.platform}`);
    outputChannel.appendLine(`  extension: ${context.extensionPath}`);

    const binaryName = resolveBinaryName(process.platform);
    outputChannel.appendLine(`  binary name resolved: ${binaryName ?? '(unsupported platform)'}`);
    if (!binaryName) {
        const msg = `CopilotStudioAgent MCP Server: platform '${process.platform}' is not supported in this release.`;
        vscode.window.showErrorMessage(msg);
        outputChannel.appendLine(msg);
        return;
    }

    const binaryPath = path.join(context.extensionPath, 'bin', binaryName);
    outputChannel.appendLine(`  binary path: ${binaryPath}`);
    const binaryExists = fs.existsSync(binaryPath);
    outputChannel.appendLine(`  binary exists: ${binaryExists}`);
    if (!binaryExists) {
        const msg = `CopilotStudioAgent MCP Server: bundled binary not found at '${binaryPath}'. Try reinstalling the extension.`;
        vscode.window.showErrorMessage(msg);
        outputChannel.appendLine(msg);
        return;
    }

    const onDidChange = new vscode.EventEmitter<void>();
    context.subscriptions.push(onDidChange);

    context.subscriptions.push(
        vscode.workspace.onDidChangeConfiguration(event => {
            if (event.affectsConfiguration('copilotstudioagent')) {
                onDidChange.fire();
            }
        })
    );

    try {
        const disposable = vscode.lm.registerMcpServerDefinitionProvider('copilotstudioagent', {
            onDidChangeMcpServerDefinitions: onDidChange.event,
            provideMcpServerDefinitions() {
                const config = vscode.workspace.getConfiguration('copilotstudioagent');
                const url = config.get<string>('url', '').trim();
                const tokenId = config.get<string>('tokenId', '').trim();
                const tokenSecret = config.get<string>('tokenSecret', '').trim();

                if (!url || !tokenId || !tokenSecret) {
                    vscode.window.showWarningMessage(
                        'CopilotStudioAgent MCP Server: URL, Token ID, and Token Secret must all be configured.',
                        'Open Settings'
                    ).then(selection => {
                        if (selection === 'Open Settings') {
                            vscode.commands.executeCommand('workbench.action.openSettings', 'copilotstudioagent');
                        }
                    });
                    outputChannel.appendLine('CopilotStudioAgent MCP Server: one or more settings are blank — server not started.');
                    return [];
                }

                outputChannel.appendLine(`CopilotStudioAgent MCP Server: providing server definition (url=${url}).`);

                return [
                    new vscode.McpStdioServerDefinition(
                        'CopilotStudioAgent',
                        binaryPath,
                        [],
                        {
                            COPILOT_STUDIO_AGENT_BASE_URL: url,
                            COPILOT_STUDIO_AGENT_TOKEN_SECRET: `${tokenId}:${tokenSecret}`,
                        }
                    )
                ];
            }
        });

        context.subscriptions.push(disposable);
        outputChannel.appendLine('CopilotStudioAgent MCP Server: activation complete.');
    } catch (error) {
        outputChannel.appendLine(`  ERROR calling registerMcpServerDefinitionProvider: ${error}`);
        vscode.window.showErrorMessage(`CopilotStudioAgent MCP Server failed to register: ${error}`);
    }
}

export function deactivate(): void {
    // VS Code disposes subscriptions; no manual cleanup required.
}

export function resolveBinaryName(platform: NodeJS.Platform): string | undefined {
    switch (platform) {
        case 'win32':
            return 'copilotstudioagent-mcp-server.exe';
        case 'linux':
            return 'copilotstudioagent-mcp-server-linux';
        default:
            return undefined;
    }
}
