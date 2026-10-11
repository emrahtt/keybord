namespace YazimAsistani.Core;

public sealed record LexiconEntry(
    string Text,
    InputLanguage Language,
    int Frequency = 1,
    IReadOnlyCollection<string>? Aliases = null);

