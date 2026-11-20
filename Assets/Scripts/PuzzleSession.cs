using UnityEngine;

public enum PuzzleMenuPage { Main, Story, Library }

// Only selection data crosses scene boundaries; every scene owns its own objects.
public static class PuzzleSession
{
    public const string MenuScene = "MainMenu";
    public const string StoryScene = "Story";
    public static readonly string[] GameScenes = { "PuzzleEasy", "PuzzleNormal", "PuzzleHard" };
    public static int Chapter { get; set; }
    public static int Difficulty { get; set; }
    public static bool StoryMode { get; set; }
    public static bool TitleDismissed { get; set; }
    public static PuzzleMenuPage MenuPage { get; set; }
    public static int LibraryChapter { get; set; } = -1;
    public static int ReplayIndex { get; set; } = -1;
    public static int ReplayEnd { get; set; } = -1;
    public static bool IsReplay => ReplayIndex >= 0;
    public static void EndReplay() { ReplayIndex = -1; ReplayEnd = -1; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Chapter = 0;
        Difficulty = 0;
        StoryMode = false;
        TitleDismissed = false;
        MenuPage = PuzzleMenuPage.Main;
        LibraryChapter = -1;
        EndReplay();
    }
}

public static class PuzzleProgress
{
    public static int Unlocked(int count, string gameId = "OneHandPuzzle") => Mathf.Clamp(PlayerPrefs.GetInt(gameId + ".Unlocked", 1), 1, count);
    public static int CompletedMask(int chapter, string gameId = "OneHandPuzzle") => PlayerPrefs.GetInt(gameId + ".Completed." + chapter, 0);
    public static int BestStars(int chapter, int difficulty, string gameId = "OneHandPuzzle")
    {
        int saved = Mathf.Clamp(PlayerPrefs.GetInt(gameId + ".BestStars." + chapter + "." + difficulty, 0), 0, 3);
        int legacy = PlayerPrefs.GetInt(gameId + ".BestGrade." + chapter + "." + difficulty, 0);
        return Mathf.Max(saved, PuzzleRating.StarsFromLegacyGrade(legacy));
    }
    public static string BestGrade(int chapter, int difficulty, string gameId = "OneHandPuzzle")
    {
        int saved = PlayerPrefs.GetInt(gameId + ".BestGrade." + chapter + "." + difficulty, 0);
        return saved >= 1 && saved <= 5 ? ((PuzzleGrade)(saved - 1)).ToString() : "";
    }

    public static void Complete(int chapter, int difficulty, int count, int moves, int minimum, string gameId = "OneHandPuzzle")
    {
        int stars = PuzzleRating.StarsFor(moves, minimum);
        string key = gameId + ".BestStars." + chapter + "." + difficulty;
        PlayerPrefs.SetInt(key, Mathf.Max(BestStars(chapter, difficulty, gameId), stars));
        PlayerPrefs.SetInt(gameId + ".Completed." + chapter, CompletedMask(chapter, gameId) | (1 << difficulty));
        PlayerPrefs.SetInt(gameId + ".Unlocked", Mathf.Max(Unlocked(count, gameId), Mathf.Min(chapter + 2, count)));
        PlayerPrefs.Save();
    }
}
