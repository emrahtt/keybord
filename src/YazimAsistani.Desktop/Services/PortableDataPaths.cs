using System.IO;

namespace YazimAsistani.Desktop.Services;

public sealed class PortableDataPaths
{
    public PortableDataPaths()
    {
        RootDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(RootDirectory);
    }

    public string RootDirectory { get; }

    public string SettingsFile => Path.Combine(RootDirectory, "settings.json");

    public string LearningStateFile => Path.Combine(RootDirectory, "learning.json");

    public string SnippetsFile => Path.Combine(RootDirectory, "snippets.json");

    public string KeyboardLayoutsFile => Path.Combine(RootDirectory, "keyboard-layouts.json");

    public string WritingHistoryFile => Path.Combine(RootDirectory, "writing-history.json");
}

