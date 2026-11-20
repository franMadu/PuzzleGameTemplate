using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WallpaperProgressValidation
{
    [MenuItem("Tools/One Hand Puzzle/Validate Wallpaper Rewards")]
    public static void Run()
    {
        var catalog = ScriptableObject.CreateInstance<PuzzleCatalog>();
        var story = ScriptableObject.CreateInstance<StorySequence>();
        var images = new[] { new Texture2D(2, 2), new Texture2D(2, 2), new Texture2D(2, 2), new Texture2D(2, 2) };
        string id = "WallpaperValidation." + Guid.NewGuid().ToString("N");
        string selectedKey = "Game.Settings.MenuWallpaper." + id;
        try
        {
            Set(catalog, "progressId", id);
            Set(catalog, "menuBackground", images[0]);
            Set(catalog, "story", story);
            Set(catalog, "chapters", new[] { new PuzzleChapter { image = images[0] } });
            var wallpapers = new GalleryWallpaper[4];
            for (int i = 0; i < wallpapers.Length; i++)
                wallpapers[i] = new GalleryWallpaper { id = "wallpaper-" + i, image = images[i] };
            Set(catalog, "wallpapers", wallpapers);
            Set(story, "steps", new[] { Step("a"), Step("b"), Step("c") });
            var chapter = new StoryChapter { title = "First", wallpaperRewards = new[] { images[1], images[2], null },
                subchapters = new[] { Section("a", "a"), Section("b", "b") } };
            Set(story, "chapters", new[] { chapter,
                new StoryChapter { title = "Second", wallpaperRewards = new[] { images[3] },
                    subchapters = new[] { Section("c", "c") } } });
            Require(story.Validate(catalog, out var error), "Reward configuration is valid: " + error);
            chapter.wallpaperRewards = new[] { new Texture2D(1, 1) };
            try { Require(!story.Validate(catalog, out _), "Unregistered reward images produce an authoring error"); }
            finally { UnityEngine.Object.DestroyImmediate(chapter.wallpaperRewards[0]); }
            chapter.wallpaperRewards = new[] { images[1], images[2], null };

            WallpaperProgress.Synchronize(catalog);
            Require(WallpaperProgress.IsUnlocked(catalog, 0), "Initial wallpaper is always available");
            Require(!WallpaperProgress.IsUnlocked(catalog, 1), "Chapter rewards start locked");
            RequireVisible(catalog, 1);
            Require(!WallpaperProgress.IsUnlocked(catalog, -1), "Invalid wallpaper is locked");
            GamePreferences.SetMenuBackground(catalog, 1);
            Require(!PlayerPrefs.HasKey(selectedKey), "Locked background cannot be selected");
            PlayerPrefs.SetString(selectedKey, wallpapers[1].Id);
            Require(GamePreferences.MenuBackground(catalog) == images[0], "Old locked selection falls back to initial background");
            PlayerPrefs.DeleteKey(selectedKey);
            Require(!StoryProgress.Advance(catalog, "wrong"), "Stale advance does not change progress");
            Require(StoryProgress.Advance(catalog, "a"), "Finish first subchapter");
            Require(!WallpaperProgress.IsUnlocked(catalog, 1), "Reaching last chapter step does not earn rewards");
            Require(StoryProgress.Advance(catalog, "b"), "Finish chapter at its last puzzle");
            Require(WallpaperProgress.IsUnlocked(catalog, 1) && WallpaperProgress.IsUnlocked(catalog, 2), "Multiple rewards unlock together");
            Require(!WallpaperProgress.IsUnlocked(catalog, 3), "Next chapter remains locked");
            RequireVisible(catalog, 3);
            GamePreferences.SetMenuBackground(catalog, 2);
            Require(GamePreferences.MenuBackground(catalog) == images[2], "Earned background can be selected");
            StoryProgress.Restart(catalog);
            chapter.wallpaperRewards = Array.Empty<Texture2D>();
            WallpaperProgress.Synchronize(catalog);
            Require(WallpaperProgress.IsUnlocked(catalog, 1) && GamePreferences.MenuBackground(catalog) == images[2],
                "Restarting and later reward edits preserve earned backgrounds and selection");
            chapter.wallpaperRewards = new[] { images[1], images[2] };

            for (int i = 0; i < wallpapers.Length; i++) PlayerPrefs.DeleteKey(id + ".Wallpaper.Unlocked." + wallpapers[i].Id);
            WallpaperProgress.Synchronize(catalog);
            Require(WallpaperProgress.IsUnlocked(catalog, 1) && WallpaperProgress.IsUnlocked(catalog, 2)
                && !WallpaperProgress.IsUnlocked(catalog, 3), "Existing save receives only completed chapter rewards");
            Require(StoryProgress.Advance(catalog, "a") && StoryProgress.Advance(catalog, "b")
                && StoryProgress.Advance(catalog, "c"), "Finish the story");
            Require(WallpaperProgress.IsUnlocked(catalog, 3), "Last chapter reward unlocks at story completion");
            RequireVisible(catalog, 4);
            StoryProgress.Restart(catalog);
            Require(WallpaperProgress.IsUnlocked(catalog, 3), "Final reward survives restart");
            Set(catalog, "story", null);
            WallpaperProgress.Synchronize(catalog);
            Require(WallpaperProgress.IsUnlocked(catalog, 1), "Earned rewards persist independently of story configuration");
        }
        finally
        {
            PlayerPrefs.DeleteKey(selectedKey);
            foreach (string field in new[] { "Step", "Completed", "Furthest", "FurthestStep" })
                PlayerPrefs.DeleteKey(id + ".Story." + field);
            for (int i = 0; i < images.Length; i++)
            {
                PlayerPrefs.DeleteKey(id + ".Wallpaper.Unlocked.wallpaper-" + i);
                UnityEngine.Object.DestroyImmediate(images[i]);
            }
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(story);
            UnityEngine.Object.DestroyImmediate(catalog);
        }
        EngineInteractionValidation.Run();
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/WallpaperProgressValidation.result", "PASS");
        Debug.Log("WALLPAPER_PROGRESS_VALIDATION_PASS");
    }

    private static StoryStep Step(string id) => new StoryStep { id = id, kind = StoryStepKind.Puzzle };
    private static void RequireVisible(PuzzleCatalog catalog, int expected)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainMenu.unity");
        try
        {
            var gallery = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PuzzleGallery>(true)).Single();
            gallery.Build(catalog, 0, _ => { }, _ => { }, () => { }, GalleryCategory.Wallpapers);
            Require(Enumerable.Range(0, 6).Count(slot => gallery.transform.Find("Image " + slot).gameObject.activeSelf)
                == expected, "Gallery shows only the available wallpapers");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    private static StorySubchapter Section(string first, string last) => new StorySubchapter {
        title = first, firstStepId = first, lastStepId = last };
    private static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
