using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace YazimAsistani.Desktop.Services;

public sealed class GlobalHotKeyService : IDisposable
{
    private const int WmHotKey = 0x0312;
    private const int LanguageToggleId = 0x5941;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModNoRepeat = 0x4000;
    private const uint VirtualKeySpace = 0x20;

    private readonly nint _windowHandle;
    private readonly HwndSource _source;
    private bool _disposed;

    public GlobalHotKeyService(Window window)
    {
        _windowHandle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_windowHandle)
            ?? throw new InvalidOperationException("Global kısayol penceresi hazırlanamadı.");
        _source.AddHook(WindowProcedure);

        if (!RegisterHotKey(
                _windowHandle,
                LanguageToggleId,
                ModControl | ModAlt | ModNoRepeat,
                VirtualKeySpace))
        {
            _source.RemoveHook(WindowProcedure);
            throw new InvalidOperationException(
                $"Ctrl+Alt+Space kısayolu kaydedilemedi: {new Win32Exception().Message}");
        }
    }

    public event EventHandler? LanguageToggleRequested;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        UnregisterHotKey(_windowHandle, LanguageToggleId);
        _source.RemoveHook(WindowProcedure);
        GC.SuppressFinalize(this);
    }

    private nint WindowProcedure(
        nint hwnd,
        int message,
        nint wParam,
        nint lParam,
        ref bool handled)
    {
        if (message == WmHotKey && wParam == LanguageToggleId)
        {
            handled = true;
            LanguageToggleRequested?.Invoke(this, EventArgs.Empty);
        }

        return 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint windowHandle, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint windowHandle, int id);
}

