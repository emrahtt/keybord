using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YazimAsistani.Desktop.Services;

public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WhKeyboardLowLevel = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSystemKeyDown = 0x0104;
    private const uint LlkhfInjected = 0x00000010;

    private readonly HookProcedure _callback;
    private nint _hookHandle;

    public GlobalKeyboardHook()
    {
        _callback = HandleHook;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var moduleHandle = GetModuleHandle(module?.ModuleName);
        _hookHandle = SetWindowsHookEx(WhKeyboardLowLevel, _callback, moduleHandle, 0);

        if (_hookHandle == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public event EventHandler<GlobalKeyEventArgs>? KeyPressed;

    public void Dispose()
    {
        if (_hookHandle == 0)
        {
            return;
        }

        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = 0;
        GC.SuppressFinalize(this);
    }

    private nint HandleHook(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && (wParam == WmKeyDown || wParam == WmSystemKeyDown))
        {
            var data = Marshal.PtrToStructure<LowLevelKeyboardInput>(lParam);
            if ((data.Flags & LlkhfInjected) == 0)
            {
                KeyPressed?.Invoke(this, new GlobalKeyEventArgs(data.VirtualKey, data.ScanCode));
            }
        }

        return CallNextHookEx(_hookHandle, code, wParam, lParam);
    }

    private delegate nint HookProcedure(int code, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct LowLevelKeyboardInput
    {
        public readonly uint VirtualKey;
        public readonly uint ScanCode;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly nuint ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int hookId, HookProcedure callback, nint module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hook);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string? moduleName);
}

public sealed record GlobalKeyEventArgs(uint VirtualKey, uint ScanCode);

