"use strict";
var __create = Object.create;
var __defProp = Object.defineProperty;
var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
var __getOwnPropNames = Object.getOwnPropertyNames;
var __getProtoOf = Object.getPrototypeOf;
var __hasOwnProp = Object.prototype.hasOwnProperty;
var __export = (target, all) => {
  for (var name in all)
    __defProp(target, name, { get: all[name], enumerable: true });
};
var __copyProps = (to, from, except, desc) => {
  if (from && typeof from === "object" || typeof from === "function") {
    for (let key of __getOwnPropNames(from))
      if (!__hasOwnProp.call(to, key) && key !== except)
        __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
  }
  return to;
};
var __toESM = (mod, isNodeMode, target) => (target = mod != null ? __create(__getProtoOf(mod)) : {}, __copyProps(
  // If the importer is in node compatibility mode or this is not an ESM
  // file that has been converted to a CommonJS file using a Babel-
  // compatible transform (i.e. "__esModule" has not been set), then set
  // "default" to the CommonJS "module.exports" for node compatibility.
  isNodeMode || !mod || !mod.__esModule ? __defProp(target, "default", { value: mod, enumerable: true }) : target,
  mod
));
var __toCommonJS = (mod) => __copyProps(__defProp({}, "__esModule", { value: true }), mod);

// src/extension.ts
var extension_exports = {};
__export(extension_exports, {
  activate: () => activate,
  deactivate: () => deactivate,
  resolveBinaryName: () => resolveBinaryName
});
module.exports = __toCommonJS(extension_exports);
var vscode = __toESM(require("vscode"));
var path = __toESM(require("path"));
var fs = __toESM(require("fs"));
function activate(context) {
  const outputChannel = vscode.window.createOutputChannel("CopilotStudioAgent MCP Server");
  context.subscriptions.push(outputChannel);
  outputChannel.appendLine("CopilotStudioAgent MCP Server: activating...");
  outputChannel.appendLine(`  platform: ${process.platform}`);
  outputChannel.appendLine(`  extension: ${context.extensionPath}`);
  const binaryName = resolveBinaryName(process.platform);
  outputChannel.appendLine(`  binary name resolved: ${binaryName ?? "(unsupported platform)"}`);
  if (!binaryName) {
    const msg = `CopilotStudioAgent MCP Server: platform '${process.platform}' is not supported in this release.`;
    vscode.window.showErrorMessage(msg);
    outputChannel.appendLine(msg);
    return;
  }
  const binaryPath = path.join(context.extensionPath, "bin", binaryName);
  outputChannel.appendLine(`  binary path: ${binaryPath}`);
  const binaryExists = fs.existsSync(binaryPath);
  outputChannel.appendLine(`  binary exists: ${binaryExists}`);
  if (!binaryExists) {
    const msg = `CopilotStudioAgent MCP Server: bundled binary not found at '${binaryPath}'. Try reinstalling the extension.`;
    vscode.window.showErrorMessage(msg);
    outputChannel.appendLine(msg);
    return;
  }
  const onDidChange = new vscode.EventEmitter();
  context.subscriptions.push(onDidChange);
  context.subscriptions.push(
    vscode.workspace.onDidChangeConfiguration((event) => {
      if (event.affectsConfiguration("copilotstudioagent")) {
        onDidChange.fire();
      }
    })
  );
  try {
    const disposable = vscode.lm.registerMcpServerDefinitionProvider("copilotstudioagent", {
      onDidChangeMcpServerDefinitions: onDidChange.event,
      provideMcpServerDefinitions() {
        const config = vscode.workspace.getConfiguration("copilotstudioagent");
        const url = config.get("url", "").trim();
        const tokenId = config.get("tokenId", "").trim();
        const tokenSecret = config.get("tokenSecret", "").trim();
        if (!url || !tokenId || !tokenSecret) {
          vscode.window.showWarningMessage(
            "CopilotStudioAgent MCP Server: URL, Token ID, and Token Secret must all be configured.",
            "Open Settings"
          ).then((selection) => {
            if (selection === "Open Settings") {
              vscode.commands.executeCommand("workbench.action.openSettings", "copilotstudioagent");
            }
          });
          outputChannel.appendLine("CopilotStudioAgent MCP Server: one or more settings are blank \u2014 server not started.");
          return [];
        }
        outputChannel.appendLine(`CopilotStudioAgent MCP Server: providing server definition (url=${url}).`);
        return [
          new vscode.McpStdioServerDefinition(
            "CopilotStudioAgent",
            binaryPath,
            [],
            {
              COPILOT_STUDIO_AGENT_BASE_URL: url,
              COPILOT_STUDIO_AGENT_TOKEN_SECRET: `${tokenId}:${tokenSecret}`
            }
          )
        ];
      }
    });
    context.subscriptions.push(disposable);
    outputChannel.appendLine("CopilotStudioAgent MCP Server: activation complete.");
  } catch (error) {
    outputChannel.appendLine(`  ERROR calling registerMcpServerDefinitionProvider: ${error}`);
    vscode.window.showErrorMessage(`CopilotStudioAgent MCP Server failed to register: ${error}`);
  }
}
function deactivate() {
}
function resolveBinaryName(platform) {
  switch (platform) {
    case "win32":
      return "copilotstudioagent-mcp-server.exe";
    case "linux":
      return "copilotstudioagent-mcp-server-linux";
    default:
      return void 0;
  }
}
// Annotate the CommonJS export names for ESM import in node:
0 && (module.exports = {
  activate,
  deactivate,
  resolveBinaryName
});
