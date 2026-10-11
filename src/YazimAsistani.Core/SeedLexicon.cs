namespace YazimAsistani.Core;

public static class SeedLexicon
{
    public static IReadOnlyList<LexiconEntry> Create() =>
    [
        new("merhaba", InputLanguage.Turkish, 100),
        new("teşekkürler", InputLanguage.Turkish, 90, ["tesekkurler"]),
        new("lütfen", InputLanguage.Turkish, 85, ["lutfen"]),
        new("çağrı", InputLanguage.Turkish, 70, ["cagri"]),
        new("çalışma", InputLanguage.Turkish, 80, ["calisma"]),
        new("görüşürüz", InputLanguage.Turkish, 75, ["gorusuruz"]),
        new("sağlık", InputLanguage.Turkish, 70, ["saglik"]),
        new("şimdi", InputLanguage.Turkish, 88, ["simdi"]),
        new("özellik", InputLanguage.Turkish, 65, ["ozellik"]),
        new("uygulama", InputLanguage.Turkish, 85),
        new("yazılım", InputLanguage.Turkish, 70, ["yazilim"]),
        new("yardımcı", InputLanguage.Turkish, 65, ["yardimci"]),
        new("bugün", InputLanguage.Turkish, 80, ["bugun"]),
        new("yarın", InputLanguage.Turkish, 75, ["yarin"]),
        new("hello", InputLanguage.English, 100),
        new("thanks", InputLanguage.English, 90),
        new("please", InputLanguage.English, 85),
        new("application", InputLanguage.English, 80),
        new("keyboard", InputLanguage.English, 80),
        new("writing", InputLanguage.English, 75),
        new("assistant", InputLanguage.English, 70),
        new("suggestion", InputLanguage.English, 65),
        new("language", InputLanguage.English, 85),
        new("today", InputLanguage.English, 80),
        new("tomorrow", InputLanguage.English, 75)
    ];
}

