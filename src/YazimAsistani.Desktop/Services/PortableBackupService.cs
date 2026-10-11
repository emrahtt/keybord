using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace YazimAsistani.Desktop.Services;

public sealed class PortableBackupService(string dataDirectory)
{
    public string DataDirectory { get; } = Path.GetFullPath(dataDirectory);

    public void CreateBackup(string destinationFile)
    {
        destinationFile = Path.GetFullPath(destinationFile);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)
            ?? throw new InvalidOperationException("Yedek hedefi geçersiz."));

        if (File.Exists(destinationFile))
        {
            File.Delete(destinationFile);
        }

        using var archive = ZipFile.Open(destinationFile, ZipArchiveMode.Create);
        foreach (var file in Directory.EnumerateFiles(DataDirectory, "*", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFullPath(file), destinationFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(DataDirectory, file);
            archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
        }
    }

    public void RestoreBackup(string sourceFile)
    {
        sourceFile = Path.GetFullPath(sourceFile);
        var stagingDirectory = Path.Combine(
            Path.GetTempPath(),
            $"yazim-asistani-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);

        try
        {
            using var archive = ZipFile.OpenRead(sourceFile);
            foreach (var entry in archive.Entries)
            {
                ValidateEntry(entry);
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var stagedPath = Path.GetFullPath(Path.Combine(stagingDirectory, entry.FullName));
                if (!stagedPath.StartsWith(
                        stagingDirectory + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("Yedek güvenli olmayan bir dosya yolu içeriyor.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(stagedPath)!);
                entry.ExtractToFile(stagedPath, overwrite: true);

                if (Path.GetExtension(stagedPath).Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(stagedPath));
                }
            }

            foreach (var stagedFile in Directory.EnumerateFiles(
                         stagingDirectory,
                         "*",
                         SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(stagingDirectory, stagedFile);
                var destinationPath = Path.GetFullPath(Path.Combine(DataDirectory, relativePath));
                if (!destinationPath.StartsWith(
                        DataDirectory + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("Yedek hedef dizinin dışına yazmaya çalışıyor.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                var temporaryPath = destinationPath + ".restore.tmp";
                File.Copy(stagedFile, temporaryPath, overwrite: true);
                File.Move(temporaryPath, destinationPath, overwrite: true);
            }
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }
    }

    private static void ValidateEntry(ZipArchiveEntry entry)
    {
        if (entry.FullName.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(entry.FullName))
        {
            throw new InvalidDataException("Yedek güvenli olmayan bir dosya yolu içeriyor.");
        }

        if (!string.IsNullOrEmpty(entry.Name) &&
            !Path.GetExtension(entry.Name).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Yedek yalnızca Yazım Asistanı JSON verilerini içerebilir.");
        }
    }
}
