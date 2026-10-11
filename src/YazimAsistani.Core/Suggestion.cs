namespace YazimAsistani.Core;

public enum SuggestionKind
{
    Completion,
    SpellingCorrection,
    NextWord
}

public sealed record Suggestion(
    string Text,
    SuggestionKind Kind,
    double Score);

