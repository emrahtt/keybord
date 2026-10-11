using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.Services;

public sealed record WritingHistoryState(
    string CurrentDraft,
    IReadOnlyList<HistoryWord> RecentWords,
    IReadOnlyList<HistorySentence> RecentSentences)
{
    public static WritingHistoryState Empty { get; } = new(string.Empty, [], []);
}

public sealed record HistoryWord(
    string Text,
    InputLanguage Language,
    DateTimeOffset Timestamp);

public sealed record HistorySentence(
    string Text,
    InputLanguage Language,
    DateTimeOffset Timestamp);

