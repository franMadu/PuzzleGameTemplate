using UnityEngine;

// Story checkpoints are independent from scores earned by replaying unlocked puzzles.
public static class StoryProgress
{
    private static string Key(PuzzleCatalog catalog, string field) => catalog.ProgressId + ".Story." + field;
    public static bool Completed(PuzzleCatalog catalog) => PlayerPrefs.GetInt(Key(catalog, "Completed"), 0) != 0;
    public static int CurrentIndex(PuzzleCatalog catalog)
    {
        if (catalog.Story == null) return 0;
        if (Completed(catalog)) return catalog.Story.Count;
        int index = catalog.Story.IndexOf(PlayerPrefs.GetString(Key(catalog, "Step"), ""));
        return Mathf.Max(0, index);
    }

    public static int PreviewPuzzle(PuzzleCatalog catalog)
    {
        if (catalog.Story == null) return 0;
        int start = Completed(catalog) ? 0 : CurrentIndex(catalog);
        for (int i = start; i < catalog.Story.Count; i++)
            if (catalog.Story[i].kind == StoryStepKind.Puzzle) return catalog.Story[i].puzzleIndex;
        for (int i = Mathf.Min(start - 1, catalog.Story.Count - 1); i >= 0; i--)
            if (catalog.Story[i].kind == StoryStepKind.Puzzle) return catalog.Story[i].puzzleIndex;
        return 0;
    }

    public static int FurthestIndex(PuzzleCatalog catalog)
    {
        int stored = PlayerPrefs.GetInt(Key(catalog, "Furthest"), -1);
        int storedIndex = catalog.Story.IndexOf(PlayerPrefs.GetString(Key(catalog, "FurthestStep"), ""));
        return Mathf.Max(CurrentIndex(catalog), storedIndex, stored == int.MaxValue ? catalog.Story.Count : -1);
    }

    public static bool IsUnlocked(PuzzleCatalog catalog, StorySubchapter section)
    {
        int first = catalog.Story.IndexOf(section.firstStepId);
        int last = catalog.Story.IndexOf(section.lastStepId);
        if (first < 0 || last < first) return false;
        int required = first;
        for (int i = first; i <= last; i++)
            if (catalog.Story[i].kind == StoryStepKind.Puzzle) required = i;
        return required <= FurthestIndex(catalog);
    }

    public static bool IsPuzzleUnlocked(PuzzleCatalog catalog, int puzzle)
    {
        if (catalog.Story == null) return false;
        for (int i = 0; i < catalog.Story.Count && i <= FurthestIndex(catalog); i++)
            if (catalog.Story[i].kind == StoryStepKind.Puzzle && catalog.Story[i].puzzleIndex == puzzle) return true;
        return false;
    }

    public static void Begin(PuzzleCatalog catalog)
    {
        if (Completed(catalog)) Restart(catalog);
        PuzzleSession.StoryMode = true;
        PuzzleSession.EndReplay();
        PuzzleSession.Difficulty = catalog[PreviewPuzzle(catalog)].StoryDifficulty;
        SaveIndex(catalog, CurrentIndex(catalog));
    }

    public static bool Advance(PuzzleCatalog catalog, string expectedStepId)
    {
        int index = CurrentIndex(catalog);
        if (catalog.Story == null || index >= catalog.Story.Count || catalog.Story[index].id != expectedStepId) return false;
        SaveIndex(catalog, index + 1);
        return true;
    }

    public static void Restart(PuzzleCatalog catalog)
    {
        Remember(catalog, FurthestIndex(catalog));
        SaveIndex(catalog, 0);
    }

    private static void Remember(PuzzleCatalog catalog, int index)
    {
        int furthest = Mathf.Max(FurthestIndex(catalog), index);
        PlayerPrefs.SetString(Key(catalog, "FurthestStep"), furthest >= catalog.Story.Count ? "" : catalog.Story[furthest].id);
        PlayerPrefs.SetInt(Key(catalog, "Furthest"), furthest >= catalog.Story.Count ? int.MaxValue : 0);
    }

    private static void SaveIndex(PuzzleCatalog catalog, int index)
    {
        Remember(catalog, index);
        bool finished = index >= catalog.Story.Count;
        PlayerPrefs.SetString(Key(catalog, "Step"), finished ? "" : catalog.Story[index].id);
        PlayerPrefs.SetInt(Key(catalog, "Completed"), finished ? 1 : 0);
        WallpaperProgress.Synchronize(catalog);
        PlayerPrefs.Save();
    }
}
