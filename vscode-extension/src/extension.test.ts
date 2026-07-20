import { describe, expect, it } from 'vitest';
import { resolveBinaryName } from './extension';

describe('resolveBinaryName', () => {
    it('returns the Linux binary name for Linux', () => {
        expect(resolveBinaryName('linux')).toBe('copilotstudioagent-mcp-server-linux');
    });

    it('returns the Windows binary name for Windows', () => {
        expect(resolveBinaryName('win32')).toBe('copilotstudioagent-mcp-server.exe');
    });

    it('returns undefined for unsupported platforms', () => {
        expect(resolveBinaryName('darwin')).toBeUndefined();
    });
});
