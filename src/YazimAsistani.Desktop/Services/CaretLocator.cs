using System.Runtime.InteropServices;
using System.Windows;
using WpfPoint = System.Windows.Point;

namespace YazimAsistani.Desktop.Services;

internal static class CaretLocator
{
    public static bool TryGetScreenPosition(out WpfPoint point)
    {
        var information = new GuiThreadInfo
        {
            Size = Marshal.SizeOf<GuiThreadInfo>()
        };

        if (GetGuiThreadInfo(0, ref information) && information.CaretWindow != 0)
        {
            var nativePoint = new NativePoint
            {
                X = information.CaretRectangle.Left,
                Y = information.CaretRectangle.Bottom + 6
            };

            if (ClientToScreen(information.CaretWindow, ref nativePoint))
            {
                point = new WpfPoint(nativePoint.X, nativePoint.Y);
                return true;
            }
        }

        if (GetCursorPos(out var cursor))
        {
            point = new WpfPoint(cursor.X + 10, cursor.Y + 18);
            return true;
        }

        point = default;
        return false;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int Size;
        public int Flags;
        public nint ActiveWindow;
        public nint FocusWindow;
        public nint CaptureWindow;
        public nint MenuOwnerWindow;
        public nint MoveSizeWindow;
        public nint CaretWindow;
        public NativeRectangle CaretRectangle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGuiThreadInfo(uint threadId, ref GuiThreadInfo information);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(nint windowHandle, ref NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);
}

