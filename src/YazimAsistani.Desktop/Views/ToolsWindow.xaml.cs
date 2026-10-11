using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using YazimAsistani.Core;
using YazimAsistani.Desktop.Services;
using YazimAsistani.Desktop.ViewModels;
using InputLanguage = YazimAsistani.Core.InputLanguage;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace YazimAsistani.Desktop.Views;

public partial class ToolsWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SnippetService _snippetService;
    private readonly KeyboardLayoutService _layoutService;
    private readonly WritingHistoryService _historyService;
    private readonly PortableBackupService _backupService;
    private readonly WindowsCredentialStore _credentialStore;
    private readonly GeminiTextService _geminiTextService;
    private readonly CloudVoiceRecorderService _cloudVoiceRecorder = new();
    private string? _editingLayoutId;

    public ToolsWindow(
        MainViewModel viewModel,
        SnippetService snippetService,
        KeyboardLayoutService layoutService,
        WritingHistoryService historyService,
        PortableBackupService backupService,
        WindowsCredentialStore credentialStore,
        GeminiTextService geminiTextService)
    {
        _viewModel = viewModel;
        _snippetService = snippetService;
        _layoutService = layoutService;
        _historyService = historyService;
        _backupService = backupService;
        _credentialStore = credentialStore;
        _geminiTextService = geminiTextService;
        DataContext = viewModel;
        InitializeComponent();
        RefreshData();
    }

    public void RefreshData()
    {
        RefreshWords();
        SnippetList.ItemsSource = _snippetService.GetAll();
        LayoutList.ItemsSource = _layoutService.GetAll();
        RefreshHistory();
        RefreshGeminiStatus();
    }

    private void RefreshWords()
    {
        var query = WordSearchBox.Text.Trim();
        WordList.ItemsSource = _viewModel.GetWords()
            .Where(word => query.Length == 0 || word.Text.Contains(
                query,
                StringComparison.CurrentCultureIgnoreCase))
            .Select(word => $"{word.Text}  ·  {word.Language}  ·  {word.Frequency + word.SelectionCount}")
            .Take(500)
            .ToArray();
    }

    private void WordSearchChanged(object sender, TextChangedEventArgs e) => RefreshWords();

    private void AddWordClicked(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewWordBox.Text))
        {
            return;
        }

        _viewModel.AddWord(
            NewWordBox.Text,
            WordAliasesBox.Text,
            GetLanguage(WordLanguageBox));
        NewWordBox.Clear();
        WordAliasesBox.Clear();
        RefreshWords();
    }

    private void SaveSnippetClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            _snippetService.AddOrUpdate(
                SnippetTriggerBox.Text,
                SnippetExpansionBox.Text,
                GetLanguage(SnippetLanguageBox));
            SnippetTriggerBox.Clear();
            SnippetExpansionBox.Clear();
            SnippetList.ItemsSource = _snippetService.GetAll();
        }
        catch (ArgumentException exception)
        {
            System.Windows.MessageBox.Show(
                this,
                exception.Message,
                "Yazım Asistanı",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void DeleteSnippetClicked(object sender, RoutedEventArgs e)
    {
        if (SnippetList.SelectedItem is SnippetDefinition definition)
        {
            _snippetService.Remove(definition);
            SnippetList.ItemsSource = _snippetService.GetAll();
        }
    }

    private void LayoutSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LayoutList.SelectedItem is not KeyboardLayoutDefinition layout)
        {
            return;
        }

        _editingLayoutId = layout.IsBuiltIn ? null : layout.Id;
        LayoutNameBox.Text = layout.IsBuiltIn ? $"{layout.Name} (kopya)" : layout.Name;
        LayoutKeysBox.Text = string.Join(' ', layout.Keys);
        LayoutLanguageBox.SelectedIndex = layout.Language switch
        {
            InputLanguage.Turkish => 1,
            InputLanguage.English => 2,
            _ => 0
        };
    }

    private void SaveLayoutClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var keys = LayoutKeysBox.Text.Split(
                [' ', ',', '\r', '\n', '\t'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _layoutService.AddOrUpdate(
                _editingLayoutId,
                LayoutNameBox.Text,
                GetOptionalLanguage(LayoutLanguageBox),
                keys);
            _editingLayoutId = null;
            LayoutNameBox.Clear();
            LayoutKeysBox.Clear();
            LayoutList.ItemsSource = _layoutService.GetAll();
        }
        catch (ArgumentException exception)
        {
            System.Windows.MessageBox.Show(
                this,
                exception.Message,
                "Yazım Asistanı",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void DeleteLayoutClicked(object sender, RoutedEventArgs e)
    {
        if (LayoutList.SelectedItem is KeyboardLayoutDefinition layout && !layout.IsBuiltIn)
        {
            _layoutService.Remove(layout);
            _editingLayoutId = null;
            LayoutNameBox.Clear();
            LayoutKeysBox.Clear();
            LayoutList.ItemsSource = _layoutService.GetAll();
        }
    }

    private void HistoryEnabledChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveTypingHistory = HistoryEnabledCheckBox.IsChecked == true;
    }

    private void ClearHistoryClicked(object sender, RoutedEventArgs e)
    {
        _historyService.Clear();
        RefreshHistory();
    }

    private void RefreshHistory()
    {
        HistoryEnabledCheckBox.IsChecked = _viewModel.SaveTypingHistory;
        var state = _historyService.GetState();
        RecoveryDraftBox.Text = state.CurrentDraft;
        RecentWordsList.ItemsSource = state.RecentWords
            .Take(30)
            .Select(word => word.Text)
            .ToArray();
        RecentSentencesList.ItemsSource = state.RecentSentences
            .Take(20)
            .Select(sentence => sentence.Text)
            .ToArray();
    }

    private void CreateBackupClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Yazım Asistanı yedeği oluştur",
            Filter = "Yazım Asistanı yedeği (*.zip)|*.zip",
            FileName = $"Yazim-Asistani-Yedek-{DateTime.Now:yyyy-MM-dd}.zip",
            AddExtension = true,
            DefaultExt = ".zip"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _backupService.CreateBackup(dialog.FileName);
            BackupStatusText.Text = $"Yedek oluşturuldu: {dialog.FileName}";
        }
        catch (IOException exception)
        {
            BackupStatusText.Text = $"Yedek oluşturulamadı: {exception.Message}";
        }
    }

    private void RestoreBackupClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Yazım Asistanı yedeğini geri yükle",
            Filter = "Yazım Asistanı yedeği (*.zip)|*.zip",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _backupService.RestoreBackup(dialog.FileName);
            BackupStatusText.Text = "Yedek geri yüklendi. Değişiklikleri uygulamak için uygulamayı yeniden başlatın.";
        }
        catch (InvalidDataException exception)
        {
            BackupStatusText.Text = $"Geçersiz yedek: {exception.Message}";
        }
        catch (IOException exception)
        {
            BackupStatusText.Text = $"Yedek geri yüklenemedi: {exception.Message}";
        }
    }

    private void OpenDataFolderClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _backupService.DataDirectory,
                UseShellExecute = true
            });
        }
        catch (Win32Exception exception)
        {
            BackupStatusText.Text = $"Klasör açılamadı: {exception.Message}";
        }
    }

    private void RefreshGeminiStatus()
    {
        GeminiModelBox.Text = _viewModel.GeminiModel;
        try
        {
            GeminiCredentialStatus.Text = _credentialStore.Contains(
                WindowsCredentialStore.GeminiApiKeyTarget)
                ? "Gemini anahtarı Windows Kimlik Bilgisi Yöneticisi'nde güvenli biçimde kayıtlı."
                : "Gemini anahtarı kayıtlı değil. AI özellikleri anahtar eklenene kadar ağ isteği yapmaz.";
        }
        catch (Win32Exception exception)
        {
            GeminiCredentialStatus.Text = $"Kimlik bilgisi durumu okunamadı: {exception.Message}";
        }
    }

    private void SaveGeminiKeyClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            _credentialStore.Write(
                WindowsCredentialStore.GeminiApiKeyTarget,
                GeminiApiKeyBox.Password);
            GeminiApiKeyBox.Clear();
            _viewModel.GeminiModel = GeminiModelBox.Text;
            RefreshGeminiStatus();
        }
        catch (Exception exception) when (exception is ArgumentException or Win32Exception)
        {
            GeminiCredentialStatus.Text = $"Anahtar kaydedilemedi: {exception.Message}";
        }
    }

    private void DeleteGeminiKeyClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            _credentialStore.Delete(WindowsCredentialStore.GeminiApiKeyTarget);
            GeminiApiKeyBox.Clear();
            RefreshGeminiStatus();
        }
        catch (Win32Exception exception)
        {
            GeminiCredentialStatus.Text = $"Anahtar silinemedi: {exception.Message}";
        }
    }

    private async void RunGeminiClicked(object sender, RoutedEventArgs e)
    {
        RunAiButton.IsEnabled = false;
        GeminiCredentialStatus.Text = "Gemini yanıtı bekleniyor…";

        try
        {
            var apiKey = _credentialStore.Read(WindowsCredentialStore.GeminiApiKeyTarget);
            _viewModel.GeminiModel = GeminiModelBox.Text;
            var action = GetAiAction();
            AiOutputBox.Text = await _geminiTextService.TransformAsync(
                apiKey ?? string.Empty,
                _viewModel.GeminiModel,
                action,
                AiInputBox.Text,
                AiCustomInstructionBox.Text);
            GeminiCredentialStatus.Text = "Dönüştürme tamamlandı.";
        }
        catch (Exception exception) when (exception is
            ArgumentException or
            InvalidOperationException or
            HttpRequestException or
            JsonException or
            IOException or
            Win32Exception)
        {
            GeminiCredentialStatus.Text = $"AI işlemi başarısız: {exception.Message}";
        }
        finally
        {
            RunAiButton.IsEnabled = true;
        }
    }

    private void CopyAiResultClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(AiOutputBox.Text))
        {
            System.Windows.Clipboard.SetText(AiOutputBox.Text);
            GeminiCredentialStatus.Text = "Sonuç panoya kopyalandı.";
        }
    }

    private async void ToggleCloudVoiceClicked(object sender, RoutedEventArgs e)
    {
        if (!_cloudVoiceRecorder.IsRecording)
        {
            try
            {
                var apiKey = _credentialStore.Read(WindowsCredentialStore.GeminiApiKeyTarget);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new InvalidOperationException("Önce Gemini API anahtarını kaydedin.");
                }

                _viewModel.GeminiModel = GeminiModelBox.Text;
                _cloudVoiceRecorder.Start();
                CloudVoiceButton.Content = "Kaydı durdur ve yazıya çevir / Stop & transcribe";
                GeminiCredentialStatus.Text = "Mikrofon kaydı sürüyor. Durdurduğunuzda ses Gemini'ye gönderilecek.";
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                GeminiCredentialStatus.Text = $"Ses kaydı başlatılamadı: {exception.Message}";
            }

            return;
        }

        CloudVoiceButton.IsEnabled = false;
        GeminiCredentialStatus.Text = "Kayıt hazırlanıyor ve Gemini'ye gönderiliyor…";
        try
        {
            var audio = await _cloudVoiceRecorder.StopAsync();
            var apiKey = _credentialStore.Read(WindowsCredentialStore.GeminiApiKeyTarget);
            var transcription = await _geminiTextService.TranscribeAudioAsync(
                apiKey ?? string.Empty,
                _viewModel.GeminiModel,
                audio,
                _viewModel.CurrentInputLanguage);
            AiInputBox.Text = string.IsNullOrWhiteSpace(AiInputBox.Text)
                ? transcription
                : $"{AiInputBox.Text.TrimEnd()}{Environment.NewLine}{transcription}";
            GeminiCredentialStatus.Text = "Bulut transkripsiyonu tamamlandı; metin giriş alanına eklendi.";
        }
        catch (Exception exception) when (exception is
            ArgumentException or
            InvalidOperationException or
            HttpRequestException or
            JsonException or
            IOException or
            TimeoutException or
            Win32Exception)
        {
            GeminiCredentialStatus.Text = $"Bulut transkripsiyonu başarısız: {exception.Message}";
        }
        finally
        {
            CloudVoiceButton.Content = "Bulut ses kaydını başlat / Start cloud dictation";
            CloudVoiceButton.IsEnabled = true;
        }
    }

    private AiTextAction GetAiAction()
    {
        var tag = (AiActionBox.SelectedItem as WpfComboBoxItem)?.Tag as string;
        return Enum.TryParse<AiTextAction>(tag, out var action)
            ? action
            : AiTextAction.Correct;
    }

    private void NormalizeTextClicked(object sender, RoutedEventArgs e) =>
        TextToolOutput.Text = TextNormalizer.NormalizeUnicode(TextToolInput.Text);

    private void UpperTextClicked(object sender, RoutedEventArgs e) =>
        TextToolOutput.Text = TextNormalizer.ToUpper(TextToolInput.Text, _viewModel.CurrentInputLanguage);

    private void LowerTextClicked(object sender, RoutedEventArgs e) =>
        TextToolOutput.Text = TextNormalizer.ToLower(TextToolInput.Text, _viewModel.CurrentInputLanguage);

    private static InputLanguage GetLanguage(WpfComboBox comboBox) =>
        comboBox.SelectedItem is WpfComboBoxItem { Tag: string tag } && tag == "English"
            ? InputLanguage.English
            : InputLanguage.Turkish;

    private static InputLanguage? GetOptionalLanguage(WpfComboBox comboBox) =>
        comboBox.SelectedItem is WpfComboBoxItem { Tag: string tag }
            ? tag switch
            {
                "Turkish" => InputLanguage.Turkish,
                "English" => InputLanguage.English,
                _ => null
            }
            : null;

    protected override void OnClosed(EventArgs e)
    {
        _cloudVoiceRecorder.Dispose();
        base.OnClosed(e);
    }
}
