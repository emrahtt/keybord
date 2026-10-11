using InputLanguage = YazimAsistani.Core.InputLanguage;

namespace YazimAsistani.Desktop.Services;

public sealed class WritingHistoryService
{
    private const int MaximumWords = 200;
    private const int MaximumSentences = 100;

    private readonly object _syncRoot = new();
    private readonly WritingHistoryStore _store;
    private WritingHistoryState _state;

    public WritingHistoryService(WritingHistoryStore store)
    {
        _store = store;
        _state = store.Load();
    }

    public WritingHistoryState GetState()
    {
        lock (_syncRoot)
        {
            return _state with
            {
                RecentWords = _state.RecentWords.ToArray(),
                RecentSentences = _state.RecentSentences.ToArray()
            };
        }
    }

    public void RecordToken(string token, InputLanguage language, bool endsSentence)
    {
        token = token.Trim();
        if (token.Length == 0)
        {
            return;
        }

        lock (_syncRoot)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var words = _state.RecentWords
                .Prepend(new HistoryWord(token, language, timestamp))
                .Take(MaximumWords)
                .ToArray();
            var draft = string.IsNullOrWhiteSpace(_state.CurrentDraft)
                ? token
                : $"{_state.CurrentDraft} {token}";
            var sentences = _state.RecentSentences;

            if (endsSentence)
            {
                sentences = sentences
                    .Prepend(new HistorySentence(draft, language, timestamp))
                    .Take(MaximumSentences)
                    .ToArray();
                draft = string.Empty;
            }

            _state = new WritingHistoryState(draft, words, sentences);
            _store.Save(_state);
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _state = WritingHistoryState.Empty;
            _store.Save(_state);
        }
    }
}
