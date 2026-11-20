using UnityEngine;

// Earned rewards belong to the game save, independently of the selected menu background.
public static class WallpaperProgress
{
    private static string Key(PuzzleCatalog catalog, GalleryWallpaper wallpaper) =>
        catalog.ProgressId + ".Wallpaper.Unlocked." + wallpaper.Id;

    public static bool IsUnlocked(PuzzleCatalog catalog, int index)
    {
        var wallpaper = catalog.WallpaperAt(index);
        return wallpaper?.image != null && (wallpaper.image == catalog.MenuBackground
            || PlayerPrefs.GetInt(Key(catalog, wallpaper), 0) != 0);
    }

    // Also grants rewards for chapters finished before this feature was introduced.
    public static void Synchronize(PuzzleCatalog catalog)
    {
        var story = catalog.Story;
        if (story == null) return;
        int furthest = StoryProgress.FurthestIndex(catalog);
        bool changed = false;
        foreach (var chapter in story.Chapters)
        {
            if (chapter?.subchapters == null || chapter.subchapters.Length == 0) continue;
            int last = -1;
            bool valid = true;
            foreach (var section in chapter.subchapters)
            {
                int first = section == null ? -1 : story.IndexOf(section.firstStepId);
                int end = section == null ? -1 : story.IndexOf(section.lastStepId);
                if (first < 0 || end < first) { valid = false; continue; }
                if (furthest > end) changed |= Grant(catalog, section.wallpaperRewards);
                last = Mathf.Max(last, end);
            }
            // Furthest points to the next step: merely reaching the last puzzle earns nothing.
            if (!valid || furthest <= last) continue;
            changed |= Grant(catalog, chapter.wallpaperRewards);
        }
        if (changed) PlayerPrefs.Save();
    }

    private static bool Grant(PuzzleCatalog catalog, Texture2D[] rewards)
    {
        if (rewards == null) return false;
        bool changed = false;
        foreach (var image in rewards)
        {
            if (image == null) continue;
            for (int i = 0; i < catalog.WallpaperCount; i++)
            {
                var wallpaper = catalog.WallpaperAt(i);
                if (wallpaper?.image != image || PlayerPrefs.GetInt(Key(catalog, wallpaper), 0) != 0) continue;
                PlayerPrefs.SetInt(Key(catalog, wallpaper), 1);
                changed = true;
            }
        }
        return changed;
    }
}
