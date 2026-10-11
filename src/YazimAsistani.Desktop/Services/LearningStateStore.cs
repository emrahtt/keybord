using System.IO;
using System.Text.Json;
using YazimAsistani.Core;

namespace YazimAsistani.Desktop.Services;

public sealed class LearningStateStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SuggestionEngineState Load()
    {
        if (!File.Exists(filePath))
        {
            return SuggestionEngineState.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<SuggestionEngineState>(
                       File.ReadAllText(filePath),
                       JsonOptions)
                   ?? SuggestionEngineState.Empty;
        }
        catch (JsonException)
        {
            return SuggestionEngineState.Empty;
        }
        catch (IOException)
        {
            return SuggestionEngineState.Empty;
        }
    }

    public void Save(SuggestionEngineState state)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryFile = filePath + ".tmp";
        File.WriteAllText(temporaryFile, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporaryFile, filePath, overwrite: true);
    }
}
