using System.Runtime.InteropServices;
using System.Text;

namespace YazimAsistani.Desktop.Services;

internal static class KeyboardTextTranslator
{
    public static string? TryTranslate(uint virtualKey, uint scanCode)
    {
        var keyboardState = new byte[256];
        if (!GetKeyboardState(keyboardState))
        {
            return null;
        }

        var foregroundWindow = GetForegroundWindow();
        var threadId = GetWindowThreadProcessId(foregroundWindow, out _);
        var keyboardLayout = GetKeyboardLayout(threadId);
        var mappedScanCode = scanCode != 0
            ? scanCode
            : MapVirtualKeyEx(virtualKey, 0, keyboardLayout);
        var buffer = new StringBuilder(8);

        var characterCount = ToUnicodeEx(
            virtualKey,
            mappedScanCode,
            keyboardState,
            buffer,
            buffer.Capacity,
            0,
            keyboardLayout);

        return characterCount > 0 ? buffer.ToString(0, characterCount) : null;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetKeyboardState(byte[] keyboardState);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);

    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint threadId);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKeyEx(uint code, uint mapType, nint keyboardLayout);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicodeEx(
        uint virtualKey,
        uint scanCode,
        byte[] keyboardState,
        StringBuilder buffer,
        int bufferSize,
        uint flags,
        nint keyboardLayout);
}

