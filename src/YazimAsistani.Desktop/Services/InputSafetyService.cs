using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace YazimAsistani.Desktop.Services;

internal static class InputSafetyService
{
    private static readonly object SyncRoot = new();
    private static DateTime _lastCheck = DateTime.MinValue;
    private static bool _lastResult;

    public static bool CanOfferSuggestions()
    {
        lock (SyncRoot)
        {
            if (DateTime.UtcNow - _lastCheck < TimeSpan.FromMilliseconds(200))
            {
                return _lastResult;
            }

            _lastCheck = DateTime.UtcNow;
            _lastResult = Evaluate();
            return _lastResult;
        }
    }

    private static bool Evaluate()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == 0)
        {
            return false;
        }

        GetWindowThreadProcessId(foregroundWindow, out var processId);
        if (processId == Environment.ProcessId)
        {
            return false;
        }

        try
        {
            var focusedElement = AutomationElement.FocusedElement;
            if (focusedElement is null)
            {
                return false;
            }

            var isPassword = focusedElement.GetCurrentPropertyValue(
                AutomationElement.IsPasswordProperty,
                ignoreDefaultValue: true);

            return isPassword is not bool passwordField || !passwordField;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (COMException)
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint windowHandle, out int processId);
}

