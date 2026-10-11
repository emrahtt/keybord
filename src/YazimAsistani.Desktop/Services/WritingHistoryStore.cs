using System.IO;
using System.Text.Json;

namespace YazimAsistani.Desktop.Services;

public sealed class WritingHistoryStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public WritingHistoryState Load()
    {
        if (!File.Exists(filePath))
        {
            return WritingHistoryState.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<WritingHistoryState>(
                       File.ReadAllText(filePath),
                       JsonOptions)
                   ?? WritingHistoryState.Empty;
        }
        catch (JsonException)
        {
            return WritingHistoryState.Empty;
        }
        catch (IOException)
        {
            return WritingHistoryState.Empty;
        }
    }

    public void Save(WritingHistoryState state)
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

