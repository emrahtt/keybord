namespace YazimAsistani.Core;

public sealed record SuggestionEngineState(
    IReadOnlyList<StoredWord> Words,
    IReadOnlyList<StoredBigram> Bigrams)
{
    public static SuggestionEngineState Empty { get; } = new([], []);
}

public sealed record StoredWord(
    string Text,
    InputLanguage Language,
    int Frequency,
    int SelectionCount,
    IReadOnlyList<string> Aliases);

public sealed record StoredBigram(
    InputLanguage Language,
    string Previous,
    string Next,
    int Count);

