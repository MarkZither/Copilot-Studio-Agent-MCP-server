import { vi } from 'vitest';

export const workspace = {
    onDidChangeConfiguration: vi.fn(() => ({ dispose: vi.fn() })),
    getConfiguration: vi.fn(() => ({
        get: vi.fn((_key: string, defaultVal: unknown) => defaultVal),
    })),
};

export const window = {
    showWarningMessage: vi.fn(),
    showErrorMessage: vi.fn(),
    createOutputChannel: vi.fn(() => ({
        appendLine: vi.fn(),
        append: vi.fn(),
        dispose: vi.fn(),
    })),
};

export const commands = {
    executeCommand: vi.fn(),
};

export const lm = {
    registerMcpServerDefinitionProvider: vi.fn(() => ({ dispose: vi.fn() })),
};

export class EventEmitter<T> {
    public event = vi.fn(() => ({ dispose: vi.fn() }));

    public fire(_value: T): void {
        // no-op in tests
    }
}

export class McpStdioServerDefinition {
    constructor(
        public readonly label: string,
        public readonly command: string,
        public readonly args: readonly string[],
        public readonly env: Record<string, string>,
    ) {}
}
