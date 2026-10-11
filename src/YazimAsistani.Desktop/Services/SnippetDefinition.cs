using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.Services;

public sealed record SnippetDefinition(
    string Trigger,
    string Expansion,
    InputLanguage Language,
    int UseCount = 0);

