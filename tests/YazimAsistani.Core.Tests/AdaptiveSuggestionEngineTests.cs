using YazimAsistani.Core;

namespace YazimAsistani.Core.Tests;

public sealed class AdaptiveSuggestionEngineTests
{
    [Fact]
    public void SuggestsTurkishCharactersFromAsciiInput()
    {
        var engine = CreateEngine();

        var suggestion = Assert.Single(engine.Suggest("cagri", InputLanguage.Turkish));

        Assert.Equal("çağrı", suggestion.Text);
        Assert.Equal(SuggestionKind.Completion, suggestion.Kind);
    }

    [Fact]
    public void CorrectsASimpleMisspelling()
    {
        var engine = CreateEngine();

        var suggestions = engine.Suggest("meraba", InputLanguage.Turkish);

        Assert.Contains(suggestions, suggestion =>
            suggestion.Text == "merhaba" && suggestion.Kind == SuggestionKind.SpellingCorrection);
    }

    [Fact]
    public void LearnsNextWordFromLocalText()
    {
        var engine = CreateEngine();
        engine.LearnText("bugün hava güzel bugün hava güzel bugün hava güzel", InputLanguage.Turkish);

        var prediction = Assert.Single(engine.PredictNext("hava", InputLanguage.Turkish));

        Assert.Equal("güzel", prediction.Text);
        Assert.Equal(SuggestionKind.NextWord, prediction.Kind);
    }

    [Fact]
    public void RememberedSelectionRanksHigher()
    {
        var engine = new AdaptiveSuggestionEngine(
        [
            new LexiconEntry("yazılım", InputLanguage.Turkish, 10, ["yazilim"]),
            new LexiconEntry("yazılımcı", InputLanguage.Turkish, 10, ["yazilimci"])
        ]);

        engine.RememberSelection("yazılımcı", InputLanguage.Turkish);

        Assert.Equal("yazılımcı", engine.Suggest("yaz", InputLanguage.Turkish)[0].Text);
    }

    [Fact]
    public void ExportedLearningCanBeImportedWithoutLosingPredictions()
    {
        var source = CreateEngine();
        source.LearnText("bugün hava güzel bugün hava güzel", InputLanguage.Turkish);
        source.RememberSelection("güzel", InputLanguage.Turkish);

        var restored = CreateEngine();
        restored.ImportState(source.ExportState());

        Assert.Equal("güzel", restored.PredictNext("hava", InputLanguage.Turkish)[0].Text);
        Assert.Contains(restored.Suggest("guz", InputLanguage.Turkish), item => item.Text == "güzel");
    }

    [Fact]
    public void CustomWordAndAliasesAreImmediatelySearchable()
    {
        var engine = CreateEngine();
        engine.AddOrUpdateWord(new LexiconEntry(
            "özelleştirilebilir",
            InputLanguage.Turkish,
            1,
            ["ozellestirilebilir", "ozel"]));

        var suggestions = engine.Suggest("ozel", InputLanguage.Turkish);

        Assert.Contains(suggestions, item => item.Text == "özelleştirilebilir");
    }

    [Fact]
    public void SuggestionsStayWithinTheSelectedLanguage()
    {
        var engine = new AdaptiveSuggestionEngine(
        [
            new LexiconEntry("hello", InputLanguage.English, 10),
            new LexiconEntry("helal", InputLanguage.Turkish, 10)
        ]);

        var suggestions = engine.Suggest("hel", InputLanguage.Turkish);

        Assert.Contains(suggestions, item => item.Text == "helal");
        Assert.DoesNotContain(suggestions, item => item.Text == "hello");
    }

    [Fact]
    public void NonPositiveLimitsReturnNoSuggestions()
    {
        var engine = CreateEngine();

        Assert.Empty(engine.Suggest("mer", InputLanguage.Turkish, limit: 0));
        Assert.Empty(engine.PredictNext("merhaba", InputLanguage.Turkish, limit: -1));
    }

    [Fact]
    public void ImportedInvalidCountersAreClamped()
    {
        var engine = CreateEngine();
        engine.ImportState(new SuggestionEngineState(
        [
            new StoredWord("deneme", InputLanguage.Turkish, -5, -2, [])
        ],
        [
            new StoredBigram(InputLanguage.Turkish, "bir", "deneme", -1)
        ]));

        var stored = Assert.Single(
            engine.ExportState().Words,
            word => word.Text == "deneme");
        Assert.Equal(1, stored.Frequency);
        Assert.Equal(0, stored.SelectionCount);
        Assert.Empty(engine.PredictNext("bir", InputLanguage.Turkish));
    }

    private static AdaptiveSuggestionEngine CreateEngine() => new(SeedLexicon.Create());
}
