using System.Windows;
using YazimAsistani.Core;
using YazimAsistani.Desktop.Services;
using YazimAsistani.Desktop.ViewModels;

namespace YazimAsistani.Desktop;

public partial class App : System.Windows.Application
{
    public static new App Current => (App)System.Windows.Application.Current;

    internal bool IsExiting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dataPaths = new PortableDataPaths();
        var settingsStore = new SettingsStore(dataPaths.SettingsFile);
        var learningStateStore = new LearningStateStore(dataPaths.LearningStateFile);
        var snippetService = new SnippetService(new SnippetStore(dataPaths.SnippetsFile));
        var keyboardLayoutService = new KeyboardLayoutService(
            new KeyboardLayoutStore(dataPaths.KeyboardLayoutsFile));
        var writingHistoryService = new WritingHistoryService(
            new WritingHistoryStore(dataPaths.WritingHistoryFile));
        var backupService = new PortableBackupService(dataPaths.RootDirectory);
        var credentialStore = new WindowsCredentialStore();
        var geminiTextService = new GeminiTextService();
        var settings = settingsStore.Load();
        var engine = new AdaptiveSuggestionEngine(SeedLexicon.Create());
        engine.ImportState(learningStateStore.Load());
        var viewModel = new MainViewModel(engine, settingsStore, learningStateStore, settings);

        var window = new MainWindow(
            viewModel,
            snippetService,
            keyboardLayoutService,
            writingHistoryService,
            backupService,
            credentialStore,
            geminiTextService);
        MainWindow = window;
        window.Show();
    }

    public void ExitApplication()
    {
        IsExiting = true;
        MainWindow?.Close();
        Shutdown();
    }
}

