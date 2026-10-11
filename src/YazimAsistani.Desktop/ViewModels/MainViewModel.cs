using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YazimAsistani.Core;
using YazimAsistani.Desktop.Services;
using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISuggestionEngine _suggestionEngine;
    private readonly SettingsStore _settingsStore;
    private readonly LearningStateStore _learningStateStore;
    private InputLanguage _inputLanguage;
    private string _uiLanguage;
    private bool _isAssistantEnabled;
    private string _previewText;
    private string _statusText = string.Empty;
    private bool _isVoiceTypingActive;
    private bool _saveTypingHistory;
    private string _geminiModel;

    public MainViewModel(
        ISuggestionEngine suggestionEngine,
        SettingsStore settingsStore,
        LearningStateStore learningStateStore,
        UserSettings settings)
    {
        _suggestionEngine = suggestionEngine;
        _settingsStore = settingsStore;
        _learningStateStore = learningStateStore;
        _inputLanguage = settings.InputLanguage;
        _uiLanguage = settings.UiLanguage is "en" ? "en" : "tr";
        _isAssistantEnabled = settings.AssistantEnabled;
        _saveTypingHistory = settings.SaveTypingHistory;
        _geminiModel = string.IsNullOrWhiteSpace(settings.GeminiModel)
            ? "gemini-3.8-flash"
            : settings.GeminiModel;
        _previewText = _inputLanguage == InputLanguage.Turkish ? "cagri" : "writ";

        ToggleLanguageCommand = new RelayCommand(ToggleLanguage);
        ToggleUiLanguageCommand = new RelayCommand(ToggleUiLanguage);
        ToggleAssistantCommand = new RelayCommand(ToggleAssistant);
        LearnPreviewCommand = new RelayCommand(LearnPreview);
        ClearPreviewCommand = new RelayCommand(ClearPreview);

        UpdateStatus();
        RefreshSuggestions();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SuggestionViewItem> Suggestions { get; } = [];

    public ICommand ToggleLanguageCommand { get; }

    public ICommand ToggleUiLanguageCommand { get; }

    public ICommand ToggleAssistantCommand { get; }

    public ICommand LearnPreviewCommand { get; }

    public ICommand ClearPreviewCommand { get; }

    public InputLanguage CurrentInputLanguage => _inputLanguage;

    public bool IsAssistantEnabled => _isAssistantEnabled;

    public bool SaveTypingHistory
    {
        get => _saveTypingHistory;
        set
        {
            if (SetField(ref _saveTypingHistory, value))
            {
                SaveSettings();
            }
        }
    }

    public string GeminiModel
    {
        get => _geminiModel;
        set
        {
            value = value.Trim();
            if (SetField(ref _geminiModel, value))
            {
                SaveSettings();
            }
        }
    }

    public string PreviewText
    {
        get => _previewText;
        set
        {
            if (SetField(ref _previewText, value))
            {
                RefreshSuggestions();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string WindowTitle => IsTurkishUi ? "Yazım Asistanı" : "Writing Assistant";

    public string Subtitle => IsTurkishUi
        ? "Türkçe ve İngilizce için çevrimdışı öncelikli yazım desteği"
        : "Offline-first writing support for Turkish and English";

    public string AssistantStateTitle => IsTurkishUi ? "Yazım motoru" : "Writing engine";

    public string LanguageButtonText => _inputLanguage == InputLanguage.Turkish ? "TR · Türkçe" : "EN · English";

    public string UiLanguageButtonText => IsTurkishUi ? "English UI" : "Türkçe arayüz";

    public string ToggleAssistantButtonText => _isAssistantEnabled
        ? (IsTurkishUi ? "Duraklat" : "Pause")
        : (IsTurkishUi ? "Etkinleştir" : "Enable");

    public string OnScreenKeyboardButtonText => IsTurkishUi ? "Ekran klavyesi" : "On-screen keyboard";

    public string ToolsButtonText => IsTurkishUi ? "Araçlar" : "Tools";

    public string VoiceButtonText => _isVoiceTypingActive
        ? (IsTurkishUi ? "Sesi durdur" : "Stop voice")
        : (IsTurkishUi ? "Sesle yaz" : "Voice typing");

    public string TryTitle => IsTurkishUi ? "Yerel motoru deneyin" : "Try the local engine";

    public string TryDescription => IsTurkishUi
        ? "Bir kelime yazın. Öneriler cihaz üzerinde, internete gönderilmeden üretilir."
        : "Type a word. Suggestions are generated on-device without sending it online.";

    public string SuggestionsTitle => IsTurkishUi ? "Öneriler" : "Suggestions";

    public string SuggestionsDescription => IsTurkishUi
        ? "Türkçe karakter tamamlama, yazım düzeltme ve öğrenilmiş sıralama."
        : "Character completion, spelling correction, and adaptive ranking.";

    public string LearnButtonText => IsTurkishUi ? "Bu metinden öğren" : "Learn from this text";

    public string ClearButtonText => IsTurkishUi ? "Temizle" : "Clear";

    public string PrivacyNotice => IsTurkishUi
        ? "Yerel modda ağ isteği yapılmaz. Kapat düğmesi uygulamayı sistem tepsisine küçültür."
        : "Local mode makes no network requests. The close button minimizes the app to the system tray.";

    private bool IsTurkishUi => _uiLanguage == "tr";

    public void ToggleLanguage()
    {
        _inputLanguage = _inputLanguage == InputLanguage.Turkish
            ? InputLanguage.English
            : InputLanguage.Turkish;
        PreviewText = _inputLanguage == InputLanguage.Turkish ? "cagri" : "writ";
        UpdateStatus();
        RaiseLocalizedProperties();
        SaveSettings();
    }

    public void ToggleAssistant()
    {
        _isAssistantEnabled = !_isAssistantEnabled;
        UpdateStatus();
        RaiseLocalizedProperties();
        SaveSettings();
    }

    public void SetExternalStatus(string status) => StatusText = status;

    public void SetVoiceTypingState(bool isActive, string? status = null)
    {
        _isVoiceTypingActive = isActive;
        OnPropertyChanged(nameof(VoiceButtonText));
        StatusText = status ?? (IsTurkishUi
            ? (isActive ? "Yerel sesle yazma etkin." : "Sesle yazma durduruldu.")
            : (isActive ? "Local voice typing is active." : "Voice typing stopped."));
    }

    public IReadOnlyList<Suggestion> SuggestForGlobalInput(string token, string? previousWord) =>
        _suggestionEngine.Suggest(token, _inputLanguage, previousWord);

    public void RememberGlobalSelection(string word)
    {
        _suggestionEngine.RememberSelection(word, _inputLanguage);
        _learningStateStore.Save(_suggestionEngine.ExportState());
    }

    public IReadOnlyList<StoredWord> GetWords() => _suggestionEngine
        .ExportState()
        .Words
        .OrderByDescending(word => word.Frequency + word.SelectionCount)
        .ThenBy(word => word.Text, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    public void AddWord(string word, string aliases, InputLanguage language)
    {
        var aliasList = aliases
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _suggestionEngine.AddOrUpdateWord(new LexiconEntry(word.Trim(), language, 1, aliasList));
        _learningStateStore.Save(_suggestionEngine.ExportState());
        RefreshSuggestions();
    }

    public void Dispose()
    {
        SaveSettings();
        _learningStateStore.Save(_suggestionEngine.ExportState());
    }

    private void ToggleUiLanguage()
    {
        _uiLanguage = IsTurkishUi ? "en" : "tr";
        UpdateStatus();
        RaiseLocalizedProperties();
        RefreshSuggestions();
        SaveSettings();
    }

    private void LearnPreview()
    {
        if (!string.IsNullOrWhiteSpace(PreviewText))
        {
            _suggestionEngine.LearnText(PreviewText, _inputLanguage);
            _learningStateStore.Save(_suggestionEngine.ExportState());
            StatusText = IsTurkishUi
                ? "Metin yalnızca yerel öğrenme motoruna eklendi."
                : "Text was added to the local learning engine only.";
            RefreshSuggestions();
        }
    }

    private void ClearPreview()
    {
        PreviewText = string.Empty;
        UpdateStatus();
    }

    private void RefreshSuggestions()
    {
        Suggestions.Clear();

        var tokens = TextNormalizer.Tokenize(PreviewText);
        if (tokens.Count == 0)
        {
            return;
        }

        var endsWithWhitespace = PreviewText.Length > 0 && char.IsWhiteSpace(PreviewText[^1]);
        var results = endsWithWhitespace
            ? _suggestionEngine.PredictNext(tokens[^1], _inputLanguage)
            : _suggestionEngine.Suggest(
                tokens[^1],
                _inputLanguage,
                tokens.Count > 1 ? tokens[^2] : null);

        foreach (var suggestion in results)
        {
            Suggestions.Add(new SuggestionViewItem(suggestion.Text, LocalizeKind(suggestion.Kind)));
        }
    }

    private string LocalizeKind(SuggestionKind kind) => (kind, IsTurkishUi) switch
    {
        (SuggestionKind.Completion, true) => "tamamlama",
        (SuggestionKind.SpellingCorrection, true) => "düzeltme",
        (SuggestionKind.NextWord, true) => "sonraki",
        (SuggestionKind.Completion, false) => "completion",
        (SuggestionKind.SpellingCorrection, false) => "correction",
        _ => "next"
    };

    private void UpdateStatus()
    {
        var language = _inputLanguage == InputLanguage.Turkish ? "Türkçe" : "English";
        StatusText = IsTurkishUi
            ? $"{(_isAssistantEnabled ? "Etkin" : "Duraklatıldı")} · Yazım dili: {language}"
            : $"{(_isAssistantEnabled ? "Enabled" : "Paused")} · Input language: {language}";
    }

    private void SaveSettings() => _settingsStore.Save(new UserSettings(
        _isAssistantEnabled,
        _inputLanguage,
        _uiLanguage,
        _saveTypingHistory,
        _geminiModel));

    private void RaiseLocalizedProperties()
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(AssistantStateTitle));
        OnPropertyChanged(nameof(LanguageButtonText));
        OnPropertyChanged(nameof(UiLanguageButtonText));
        OnPropertyChanged(nameof(ToggleAssistantButtonText));
        OnPropertyChanged(nameof(OnScreenKeyboardButtonText));
        OnPropertyChanged(nameof(ToolsButtonText));
        OnPropertyChanged(nameof(VoiceButtonText));
        OnPropertyChanged(nameof(CurrentInputLanguage));
        OnPropertyChanged(nameof(IsAssistantEnabled));
        OnPropertyChanged(nameof(SaveTypingHistory));
        OnPropertyChanged(nameof(GeminiModel));
        OnPropertyChanged(nameof(TryTitle));
        OnPropertyChanged(nameof(TryDescription));
        OnPropertyChanged(nameof(SuggestionsTitle));
        OnPropertyChanged(nameof(SuggestionsDescription));
        OnPropertyChanged(nameof(LearnButtonText));
        OnPropertyChanged(nameof(ClearButtonText));
        OnPropertyChanged(nameof(PrivacyNotice));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

