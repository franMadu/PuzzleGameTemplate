using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CanvasValidation
{
    private const string ResultPath = "Logs/CanvasValidation.result";

    [MenuItem("Tools/One Hand Puzzle/Validate Canvas References")]
    public static void Validate()
    {
        int previousLibraryChapter = PuzzleSession.LibraryChapter;
        Directory.CreateDirectory("Logs");
        try
        {
            foreach (string name in new[] { "MainMenu", "PuzzleEasy", "PuzzleNormal", "PuzzleHard", "Story", "DialogueDemo" })
            {
                var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + name + ".unity");
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var components = roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                    Require(components.All(component => component != null), name + ": missing script");
                    foreach (var label in components.OfType<Text>()) ValidateFont(label);
                    foreach (var text in components.OfType<CanvasText>()) ValidateText(text);
                    foreach (var component in components)
                    {
                        string[] fields = component is PuzzleMenu ? new[] { "catalog", "titleScreen", "mainScreen", "galleryScreen", "imageScreen", "selectionScreen", "settingsScreen", "chaptersScreen", "subchaptersScreen", "difficultyScreen" }
                            : component is DialogueController ? new[] { "screen" }
                            : component is PuzzleSceneController ? new[] { "victoryScreen", "completionContinue", "gameLayout", "gameTitle", "catalog", "puzzle", "dialogue" }
                            : component is Puzzle ? new[] { "completedImage", "completionFlash", "board", "status" }
                            : component is StorySceneController ? new[] { "endingScreen", "errorScreen", "catalog", "dialogue" }
                            : component is DialogueDemoController ? new[] { "finishScreen", "dialogue", "conversation" } : Array.Empty<string>();
                        foreach (string field in fields)
                            Require(Field<UnityEngine.Object>(component, field) != null, name + ": missing " + component.GetType().Name + "." + field);
                    }
                    int before = roots.Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
                    if (name == "MainMenu") ValidateMenu(components);
                    foreach (var dialogue in components.OfType<DialogueController>())
                        Invoke(dialogue, "EnsureUI");
                    foreach (var controller in components.OfType<PuzzleSceneController>())
                    {
                        var layout = Field<GameObject>(controller, "gameLayout").transform;
                        Check<Button>(layout, "Back to Menu", "Continue After Reveal", "Shuffle Button");
                        var content = Field<RectTransform>(controller, "victoryScreen").Find("Content");
                        Check<Text>(content, "Title", "Grade", "Moves", "Best", "Next/Label");
                        Check<RawImage>(content, "Artwork");
                        Check<Button>(content, "Next", "Play Again", "Back");
                    }
                    foreach (var controller in components.OfType<StorySceneController>())
                    {
                        Check<Text>(Field<RectTransform>(controller, "endingScreen"), "Content/Ending");
                        Check<Button>(Field<RectTransform>(controller, "endingScreen"), "Content/Back");
                        Check<Text>(Field<RectTransform>(controller, "errorScreen"), "Content/Error");
                        Check<Button>(Field<RectTransform>(controller, "errorScreen"), "Content/Back");
                    }
                    int after = roots.Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
                    Require(before == after, name + ": binding created UI objects");
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            File.WriteAllText(ResultPath, "PASS: all six scenes, serialized references, editable text states/formats, menu bindings, gallery pagination/reuse, background persistence/fallback, zoom bounds, dialogue bindings and unchanged UI object counts.");
            Debug.Log("Canvas validation passed for all six scenes.");
        }
        catch (Exception error)
        {
            File.WriteAllText(ResultPath, "FAIL: " + error);
            Debug.LogException(error);
        }
        finally { PuzzleSession.LibraryChapter = previousLibraryChapter; }
    }

    private static void ValidateFont(Text label)
    {
        Require(label.font != null, label.name + ": missing font; text cannot render");
        if (string.IsNullOrWhiteSpace(label.text)) return;
        var settings = label.GetGenerationSettings(new Vector2(1200, 400));
        using (var generator = new TextGenerator())
        {
            Require(generator.Populate(label.text, settings) && generator.vertexCount > 0,
                label.name + ": text produced no visible geometry");
        }
    }

    private static void ValidateText(CanvasText source)
    {
        var label = source.GetComponent<Text>();
        var formats = Field<string[]>(source, "formats");
        Require(formats != null && formats.Length > 0, source.name + ": missing text formats");
        for (int state = 0; state < formats.Length; state++)
        {
            CanvasText.Set(label, state, 7, 8, 9);
            Require(label.text == string.Format(formats[state], 7, 8, 9), source.name + ": text state mismatch");
        }
        var serialized = new SerializedObject(source);
        var first = serialized.FindProperty("formats").GetArrayElementAtIndex(0);
        string original = first.stringValue;
        try
        {
            first.stringValue = "Texto editado {0}";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CanvasText.Set(label, 0, 12);
            Require(label.text == "Texto editado 12", source.name + ": Inspector text was overwritten");
        }
        finally
        {
            first.stringValue = original;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ValidateMenu(MonoBehaviour[] components)
    {
        var menu = components.OfType<PuzzleMenu>().Single();
        var catalog = Field<PuzzleCatalog>(menu, "catalog");
        Transform Content(string field) => Field<RectTransform>(menu, field).Find("Content");
        Check<Text>(Content("titleScreen"), "Title");
        Check<Button>(Content("titleScreen"), "Start");
        Check<Button>(Content("mainScreen"), "Settings", "Gallery", "Story", "Recap");
        var background = MenuControls.Get<RawImage>(Field<RectTransform>(menu, "mainScreen"), "Menu Background/Artwork");
        Require(background.GetComponent<AspectRatioFitter>() != null, "Missing background fitter");
        Check<RawImage>(Content("selectionScreen"), "Artwork");
        Check<Text>(Content("selectionScreen"), "Hidden", "Location");
        Check<Button>(Content("selectionScreen"), "Continue", "Back");
        var gallery = components.OfType<PuzzleGallery>().Single();
        gallery.Build(catalog, 0, _ => { }, _ => { }, () => { });
        gallery.Build(catalog, int.MaxValue, _ => { }, _ => { }, () => { });
        ValidateBackgroundAndGallery(catalog, gallery);
        var settings = components.OfType<SettingsMenu>().Single();
        settings.Build(() => { });
        settings.Build(() => { });
        var viewer = components.OfType<GalleryImageViewer>().Single();
        var texture = catalog[0].ImageForDifficulty(0);
        Require(texture != null, "Missing image for viewer validation");
        viewer.Build(texture, () => { }, () => { }, false);
        Invoke(viewer, "ChangeZoom", 10f);
        Require(Field<float>(viewer, "zoom") == 4, "Upper zoom bound");
        Invoke(viewer, "ChangeZoom", -10f);
        Require(Field<float>(viewer, "zoom") == 1, "Lower zoom bound");
        viewer.Build(texture, () => { }, () => { }, true);
        Require(!MenuControls.Get<Button>(viewer.transform, "Controls/Use Background").interactable, "Selected background state");
        Func<string, RectTransform> panel = name => (RectTransform)Content(name == "Chapter Selection" ? "chaptersScreen" : name == "Subchapter Selection" ? "subchaptersScreen" : "difficultyScreen");
        if (catalog.Story != null)
        {
            var library = new PuzzleLibrary(catalog, panel, () => { });
            Invoke(library, "Chapters", 0);
            for (int chapter = 0; chapter < catalog.Story.Chapters.Length; chapter++)
            {
                Invoke(library, "Subchapters", chapter, 0);
                foreach (var section in catalog.Story.Chapters[chapter].subchapters)
                    if (catalog.Story.PuzzleIn(section) >= 0)
                        for (int difficulty = 0; difficulty < 3; difficulty++)
                            Invoke(library, "PuzzleDetails", chapter, 0, section, difficulty);
            }
        }
    }

    private static void ValidateBackgroundAndGallery(PuzzleCatalog source, PuzzleGallery gallery)
    {
        var catalog = UnityEngine.Object.Instantiate(source);
        // Gallery fixtures must not depend on which puzzles the authored story contains.
        var story = ScriptableObject.CreateInstance<StorySequence>();
        typeof(StorySequence).GetField("steps", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(story,
            Enumerable.Range(0, catalog.Count).Select(index => new StoryStep {
                id = "validation-puzzle-" + index, kind = StoryStepKind.Puzzle, puzzleIndex = index
            }).ToArray());
        string id = "CanvasValidation." + Guid.NewGuid().ToString("N");
        var serialized = new SerializedObject(catalog);
        serialized.FindProperty("progressId").stringValue = id;
        serialized.FindProperty("story").objectReferenceValue = story;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        string backgroundKey = "Game.Settings.MenuBackground." + id;
        try
        {
            Require(GamePreferences.MenuBackground(catalog) == catalog.MenuBackground, "Default background");
            PlayerPrefs.SetInt(id + ".Story.Furthest", int.MaxValue);
            for (int chapter = 0; chapter < catalog.Count; chapter++)
                PlayerPrefs.SetInt(id + ".Completed." + chapter, 7);
            gallery.Build(catalog, 0, _ => { }, _ => { }, () => { });
            int completed = 0;
            for (int chapter = 0; chapter < catalog.Count; chapter++)
                for (int difficulty = 0; difficulty < 3; difficulty++)
                    if (catalog[chapter].ImageForDifficulty(difficulty) != null) completed++;
            int firstPage = Enumerable.Range(0, 6).Count(slot => gallery.transform.Find("Image " + slot).gameObject.activeSelf);
            Require(firstPage == Mathf.Min(6, completed), "Gallery first page");
            gallery.Build(catalog, int.MaxValue, _ => { }, _ => { }, () => { });
            int lastPage = Enumerable.Range(0, 6).Count(slot => gallery.transform.Find("Image " + slot).gameObject.activeSelf);
            Require(lastPage == (completed == 0 ? 0 : (completed - 1) % 6 + 1), "Gallery last page");
            int index = source[0].ImageForDifficulty(1) != null ? 1 : 0;
            GamePreferences.SetMenuBackground(catalog, index);
            Require(PlayerPrefs.GetInt(backgroundKey, -1) == index, "Saved background index");
            Require(GamePreferences.MenuBackground(catalog) == catalog[0].ImageForDifficulty(index), "Selected difficulty background");
            GamePreferences.SetMenuBackground(catalog, int.MaxValue);
            Require(GamePreferences.MenuBackground(catalog) == catalog.MenuBackground, "Removed image fallback");
            GamePreferences.SetMenuBackground(catalog, index);
            PlayerPrefs.SetInt(id + ".Completed.0", 0);
            Require(GamePreferences.MenuBackground(catalog) == catalog.MenuBackground, "Locked image fallback");
        }
        finally
        {
            PlayerPrefs.DeleteKey(backgroundKey);
            PlayerPrefs.DeleteKey(id + ".Story.Furthest");
            for (int chapter = 0; chapter < catalog.Count; chapter++) PlayerPrefs.DeleteKey(id + ".Completed." + chapter);
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(story);
        }
    }

    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static void Check<T>(Transform root, params string[] paths) where T : Component
    {
        foreach (string path in paths) Require(root.Find(path) != null && root.Find(path).GetComponent<T>() != null, root.name + "/" + path + ": missing " + typeof(T).Name);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
