using YazimAsistani.Core;

namespace YazimAsistani.Core.Tests;

public sealed class TextNormalizerTests
{
    [Theory]
    [InlineData("ÇAĞRI", "cagri")]
    [InlineData("IŞIK", "isik")]
    [InlineData("Görüşürüz", "gorusuruz")]
    public void TurkishLookupNormalizationSupportsAsciiInput(string input, string expected)
    {
        Assert.Equal(expected, TextNormalizer.NormalizeForLookup(input, InputLanguage.Turkish));
    }

    [Fact]
    public void TurkishCasingUsesDottedAndDotlessI()
    {
        Assert.Equal("İSTANBUL", TextNormalizer.ToUpper("istanbul", InputLanguage.Turkish));
        Assert.Equal("ışık", TextNormalizer.ToLower("IŞIK", InputLanguage.Turkish));
    }

    [Fact]
    public void RepairsCommonTurkishMojibake()
    {
        Assert.Equal("Türkçe için", TextNormalizer.NormalizeUnicode("TÃ¼rkÃ§e iÃ§in"));
    }

    [Fact]
    public void TokenizationPreservesApostrophesAndNumbers()
    {
        Assert.Equal(
            ["Ankara'nın", "2026", "hedefi"],
            TextNormalizer.Tokenize("Ankara'nın 2026 hedefi."));
    }
}

