using System.ComponentModel;
using System.Windows;
using YazimAsistani.Desktop.Services;
using YazimAsistani.Desktop.ViewModels;
using YazimAsistani.Desktop.Views;

namespace YazimAsistani.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SnippetService _snippetService;
    private readonly KeyboardLayoutService _keyboardLayoutService;
    private readonly WritingHistoryService _writingHistoryService;
    private readonly PortableBackupService _backupService;
    private readonly WindowsCredentialStore _credentialStore;
    private readonly GeminiTextService _geminiTextService;
    private GlobalHotKeyService? _hotKeyService;
    private TrayIconService? _trayIconService;
    private OnScreenKeyboardWindow? _onScreenKeyboardWindow;
    private SuggestionOverlayWindow? _suggestionOverlayWindow;
    private GlobalKeyboardHook? _keyboardHook;
    private TypingCoordinator? _typingCoordinator;
    private ToolsWindow? _toolsWindow;
    private VoiceTypingService? _voiceTypingService;

    public MainWindow(
        MainViewModel viewModel,
        SnippetService snippetService,
        KeyboardLayoutService keyboardLayoutService,
        WritingHistoryService writingHistoryService,
        PortableBackupService backupService,
        WindowsCredentialStore credentialStore,
        GeminiTextService geminiTextService)
    {
        _viewModel = viewModel;
        _snippetService = snippetService;
        _keyboardLayoutService = keyboardLayoutService;
        _writingHistoryService = writingHistoryService;
        _backupService = backupService;
        _credentialStore = credentialStore;
        _geminiTextService = geminiTextService;
        DataContext = viewModel;
        InitializeComponent();

        Loaded += HandleLoaded;
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= HandleLoaded;

        _trayIconService = new TrayIconService(
            showWindow: ShowFromTray,
            toggleLanguage: () => Dispatcher.Invoke(_viewModel.ToggleLanguage),
            exitApplication: () => Dispatcher.Invoke(App.Current.ExitApplication));

        try
        {
            _hotKeyService = new GlobalHotKeyService(this);
            _hotKeyService.LanguageToggleRequested += (_, _) =>
                Dispatcher.Invoke(_viewModel.ToggleLanguage);
        }
        catch (InvalidOperationException exception)
        {
            _viewModel.SetExternalStatus(exception.Message);
        }

        try
        {
            _suggestionOverlayWindow = new SuggestionOverlayWindow();
            _typingCoordinator = new TypingCoordinator(
                () => _viewModel.IsAssistantEnabled,
                _viewModel.SuggestForGlobalInput,
                trigger => _snippetService.TryExpand(trigger, _viewModel.CurrentInputLanguage));
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyPressed += (_, key) => _typingCoordinator.HandleKey(key);
            _typingCoordinator.SuggestionsUpdated += (_, update) => Dispatcher.Invoke(() =>
                _suggestionOverlayWindow.ShowSuggestions(
                    update.Suggestions,
                    update.TypedCharacterCount));
            _typingCoordinator.SuggestionsDismissed += (_, _) => Dispatcher.Invoke(() =>
                _suggestionOverlayWindow.Hide());
            _typingCoordinator.SnippetExpansionRequested += (_, expansion) => Dispatcher.Invoke(() =>
            {
                try
                {
                    TextInjector.ReplacePreviousCharacters(
                        expansion.TypedCharacterCount,
                        expansion.Expansion);
                    _snippetService.RememberUse(
                        expansion.Trigger,
                        _viewModel.CurrentInputLanguage);
                }
                catch (Win32Exception exception)
                {
                    _viewModel.SetExternalStatus($"Kısaltma gönderilemedi: {exception.Message}");
                }
            });
            _typingCoordinator.TokenCompleted += (_, token) =>
            {
                if (_viewModel.SaveTypingHistory)
                {
                    _writingHistoryService.RecordToken(
                        token.Token,
                        _viewModel.CurrentInputLanguage,
                        token.EndsSentence);
                }
            };
            _suggestionOverlayWindow.SuggestionChosen += (_, choice) => Dispatcher.Invoke(() =>
            {
                try
                {
                    TextInjector.ReplacePreviousCharacters(
                        choice.TypedCharacterCount,
                        choice.Suggestion);
                    _viewModel.RememberGlobalSelection(choice.Suggestion);
                    _typingCoordinator.AcceptSuggestion(choice.Suggestion);
                }
                catch (Win32Exception exception)
                {
                    _viewModel.SetExternalStatus($"Öneri gönderilemedi: {exception.Message}");
                }
            });
        }
        catch (Win32Exception exception)
        {
            _viewModel.SetExternalStatus($"Global yazım desteği başlatılamadı: {exception.Message}");
        }
    }

    private void ShowFromTray()
    {
        Dispatcher.Invoke(() =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        });
    }

    private void OpenOnScreenKeyboard(object sender, RoutedEventArgs e)
    {
        if (_onScreenKeyboardWindow is null)
        {
            _onScreenKeyboardWindow = new OnScreenKeyboardWindow(
                () => _viewModel.CurrentInputLanguage,
                _keyboardLayoutService);
            _onScreenKeyboardWindow.Closed += (_, _) => _onScreenKeyboardWindow = null;
        }

        _onScreenKeyboardWindow.RefreshKeys();
        _onScreenKeyboardWindow.Show();
    }

    private void OpenTools(object sender, RoutedEventArgs e)
    {
        if (_toolsWindow is null)
        {
            _toolsWindow = new ToolsWindow(
                _viewModel,
                _snippetService,
                _keyboardLayoutService,
                _writingHistoryService,
                _backupService,
                _credentialStore,
                _geminiTextService);
            _toolsWindow.Closed += (_, _) => _toolsWindow = null;
        }

        _toolsWindow.RefreshData();
        _toolsWindow.Show();
        _toolsWindow.Activate();
    }

    private void ToggleVoiceTyping(object sender, RoutedEventArgs e)
    {
        _voiceTypingService ??= CreateVoiceTypingService();

        if (_voiceTypingService.IsRunning)
        {
            try
            {
                _voiceTypingService.Stop();
                _viewModel.SetVoiceTypingState(false);
            }
            catch (InvalidOperationException exception)
            {
                _viewModel.SetVoiceTypingState(false, exception.Message);
            }

            return;
        }

        try
        {
            _voiceTypingService.Start(_viewModel.CurrentInputLanguage);
            _viewModel.SetVoiceTypingState(true);
        }
        catch (InvalidOperationException exception)
        {
            _viewModel.SetVoiceTypingState(false, exception.Message);
        }
    }

    private VoiceTypingService CreateVoiceTypingService()
    {
        var service = new VoiceTypingService();
        service.TextRecognized += (_, text) => Dispatcher.Invoke(() =>
        {
            try
            {
                TextInjector.SendUnicodeText(text + " ");
            }
            catch (Win32Exception exception)
            {
                _viewModel.SetExternalStatus($"Sesli metin gönderilemedi: {exception.Message}");
            }
        });
        return service;
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!App.Current.IsExiting)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _hotKeyService?.Dispose();
        _keyboardHook?.Dispose();
        _trayIconService?.Dispose();
        _suggestionOverlayWindow?.Close();
        _onScreenKeyboardWindow?.Close();
        _toolsWindow?.Close();
        _voiceTypingService?.Dispose();
        _geminiTextService.Dispose();
        _viewModel.Dispose();
        base.OnClosing(e);
    }
}

