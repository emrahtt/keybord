namespace YazimAsistani.Core;

public interface ISuggestionEngine
{
    IReadOnlyList<Suggestion> Suggest(
        string input,
        InputLanguage language,
        string? previousWord = null,
        int limit = 6);

    IReadOnlyList<Suggestion> PredictNext(
        string previousWord,
        InputLanguage language,
        int limit = 6);

    void LearnText(string text, InputLanguage language);

    void RememberSelection(string word, InputLanguage language);

    void AddOrUpdateWord(LexiconEntry entry);

    SuggestionEngineState ExportState();

    void ImportState(SuggestionEngineState state);
}

