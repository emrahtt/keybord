using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using YazimAsistani.Core;
using YazimAsistani.Desktop.Services;
using WpfButton = System.Windows.Controls.Button;

namespace YazimAsistani.Desktop.Views;

public partial class SuggestionOverlayWindow : Window
{
    private const int ExtendedStyleIndex = -20;
    private const int NoActivateStyle = 0x08000000;
    private const int ToolWindowStyle = 0x00000080;
    private int _typedCharacterCount;

    public SuggestionOverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += HandleSourceInitialized;
    }

    public event EventHandler<SuggestionChosenEventArgs>? SuggestionChosen;

    public void ShowSuggestions(IReadOnlyList<Suggestion> suggestions, int typedCharacterCount)
    {
        _typedCharacterCount = typedCharacterCount;
        SuggestionItems.Items.Clear();

        foreach (var suggestion in suggestions.Take(6))
        {
            var button = new WpfButton
            {
                Content = suggestion.Text,
                Tag = suggestion.Text,
                Margin = new Thickness(2),
                Padding = new Thickness(12, 7, 12, 7),
                FontSize = 14
            };
            button.Click += SuggestionButtonClicked;
            SuggestionItems.Items.Add(button);
        }

        if (CaretLocator.TryGetScreenPosition(out var position))
        {
            Left = position.X;
            Top = position.Y;
        }

        if (!IsVisible)
        {
            Show();
        }
    }

    private void SuggestionButtonClicked(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { Tag: string suggestion })
        {
            SuggestionChosen?.Invoke(
                this,
                new SuggestionChosenEventArgs(suggestion, _typedCharacterCount));
        }
    }

    private void HandleSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var styles = GetWindowLongPtr(handle, ExtendedStyleIndex).ToInt64();
        SetWindowLongPtr(handle, ExtendedStyleIndex, new nint(styles | NoActivateStyle | ToolWindowStyle));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint windowHandle, int index, nint newValue);
}

public sealed record SuggestionChosenEventArgs(string Suggestion, int TypedCharacterCount);

