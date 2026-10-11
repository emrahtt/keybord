using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using YazimAsistani.Core;
using YazimAsistani.Desktop.Services;
using InputLanguage = YazimAsistani.Core.InputLanguage;
using WpfButton = System.Windows.Controls.Button;

namespace YazimAsistani.Desktop.Views;

public partial class OnScreenKeyboardWindow : Window
{
    private const int ExtendedStyleIndex = -20;
    private const int NoActivateStyle = 0x08000000;
    private const int ToolWindowStyle = 0x00000080;

    private readonly Func<InputLanguage> _languageProvider;
    private readonly KeyboardLayoutService _layoutService;
    private bool _refreshing;

    public OnScreenKeyboardWindow(
        Func<InputLanguage> languageProvider,
        KeyboardLayoutService layoutService)
    {
        _languageProvider = languageProvider;
        _layoutService = layoutService;
        InitializeComponent();
        SourceInitialized += HandleSourceInitialized;
        RefreshKeys();
    }

    public void RefreshKeys()
    {
        var language = _languageProvider();
        var layouts = _layoutService.GetForLanguage(language);
        var selectedId = (LayoutSelector.SelectedItem as KeyboardLayoutDefinition)?.Id;
        _refreshing = true;
        LayoutSelector.ItemsSource = layouts;
        LayoutSelector.SelectedItem = layouts.FirstOrDefault(layout => layout.Id == selectedId)
            ?? layouts.First();
        _refreshing = false;
        RenderSelectedLayout();
    }

    private void RenderSelectedLayout()
    {
        if (LayoutSelector.SelectedItem is not KeyboardLayoutDefinition layout)
        {
            return;
        }

        LanguageTitle.Text = _languageProvider() == InputLanguage.Turkish ? "TR" : "EN";

        KeyGrid.Children.Clear();
        foreach (var key in layout.Keys)
        {
            var button = new WpfButton
            {
                Content = key,
                Tag = key,
                Margin = new Thickness(3),
                FontSize = 16,
                Padding = new Thickness(8)
            };
            button.Click += CharacterKeyClicked;
            KeyGrid.Children.Add(button);
        }
    }

    private void CharacterKeyClicked(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { Tag: string text })
        {
            TrySend(() => TextInjector.SendUnicodeText(text));
        }
    }

    private void SpecialKeyClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton { Tag: string action })
        {
            return;
        }

        switch (action)
        {
            case "Backspace":
                TrySend(() => TextInjector.ReplacePreviousCharacters(1, string.Empty));
                break;
            case "Space":
                TrySend(() => TextInjector.SendUnicodeText(" "));
                break;
            case "Enter":
                TrySend(() => TextInjector.SendUnicodeText(Environment.NewLine));
                break;
        }
    }

    private void TrySend(Action send)
    {
        try
        {
            send();
        }
        catch (Win32Exception exception)
        {
            LanguageTitle.Text = $"Hata / Error: {exception.Message}";
        }
    }

    private void RefreshButtonClicked(object sender, RoutedEventArgs e) => RefreshKeys();

    private void LayoutSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_refreshing)
        {
            RenderSelectedLayout();
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
