using System.IO;
using System.Text.Json;

namespace YazimAsistani.Desktop.Services;

public sealed class SnippetStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IReadOnlyList<SnippetDefinition> Load()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<SnippetDefinition>>(
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

    public void Save(IReadOnlyCollection<SnippetDefinition> snippets)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryFile = filePath + ".tmp";
        File.WriteAllText(temporaryFile, JsonSerializer.Serialize(snippets, JsonOptions));
        File.Move(temporaryFile, filePath, overwrite: true);
    }
}

