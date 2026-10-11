using System.IO;
using System.Text.Json;

namespace YazimAsistani.Desktop.Services;

public sealed class KeyboardLayoutStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IReadOnlyList<KeyboardLayoutDefinition> Load()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<KeyboardLayoutDefinition>>(
                       File.ReadAllText(filePath),
                       JsonOptions)
                   ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    public void Save(IReadOnlyCollection<KeyboardLayoutDefinition> layouts)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryFile = filePath + ".tmp";
        File.WriteAllText(temporaryFile, JsonSerializer.Serialize(layouts, JsonOptions));
        File.Move(temporaryFile, filePath, overwrite: true);
    }
}

