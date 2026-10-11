using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.Services;

public sealed record KeyboardLayoutDefinition(
    string Id,
    string Name,
    InputLanguage? Language,
    IReadOnlyList<string> Keys,
    bool IsBuiltIn = false);

