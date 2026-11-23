using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class EngineInteractionValidation
{
    [MenuItem("Tools/One Hand Puzzle/Validate Dialogue Interaction and Stars")]
    public static void Run()
    {
        ValidateStars();
        foreach (var name in new[] { "Story", "PuzzleEasy", "PuzzleNormal", "PuzzleHard" })
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + name + ".unity");
            try
            {
                var controller = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<DialogueController>(true)).Single();
                var screen = Field<RectTransform>(controller, "screen");
                screen.gameObject.SetActive(true);
                int before = screen.GetComponentsInChildren<Transform>(true).Length;
                Invoke(controller, "EnsureUI");
                ValidatePortraitHeight(controller);
                var panel = Field<RectTransform>(controller, "dialogueBox");
                var menu = screen.Find("Back to Menu").GetComponent<Button>();
                var menuRect = (RectTransform)menu.transform;
                Require(menuRect.anchorMin == new Vector2(0, 1) && menuRect.anchorMax == new Vector2(0, 1)
                    && menuRect.anchoredPosition.x > 0 && menuRect.anchoredPosition.y < 0,
                    "Menu button must be authored in the upper left corner");
                Require(menu.GetComponentInChildren<Text>(true).text == "Menú principal", "Menu button needs a visible label");
                Require(menu.transform.GetSiblingIndex() > screen.Find("Restore Dialogue").GetSiblingIndex(),
                    "Menu button must remain above the hidden-dialogue restore overlay");
                Require(panel.GetComponent<Button>() != null && panel.GetComponent<Image>().raycastTarget, "Whole panel must receive clicks");
                Require(panel.Find("Next Icon").GetComponent<Button>() == null && !panel.Find("Next Icon").GetComponent<Graphic>().raycastTarget, "Next icon must not intercept clicks");
                Require(panel.Find("Hide").GetComponent<Button>() != null && screen.Find("Restore Dialogue").GetComponent<Button>() != null, "Missing authored hide/restore controls");
                var pages = Field<IList>(controller, "pages");
                var type = typeof(DialogueController).GetNestedType("Page", BindingFlags.NonPublic);
                pages.Clear();
                for (int i = 0; i < 2; i++)
                {
                    var page = Activator.CreateInstance(type, true);
                    type.GetField("text").SetValue(page, "Texto con tildes: ¿qué ocurre? " + i);
                    type.GetField("speakerSide").SetValue(page, DialogueSpeakerSide.Narration);
                    pages.Add(page);
                }
                Property(controller, "PageIndex", 0);
                Property(controller, "IsPlaying", true);
                Invoke(controller, "ShowPage", true);
                Invoke(controller, "Reveal", 0);
                Require(controller.IsTyping && !panel.Find("Next Icon").gameObject.activeSelf, "Icon must be hidden while typing");
                var body = Field<Text>(controller, "bodyText");
                string partial = body.text;
                Click(panel.Find("Hide").GetComponent<Button>());
                Require(controller.IsDialogueHidden && !panel.gameObject.activeSelf && screen.Find("Restore Dialogue").gameObject.activeSelf, "Hide must retain scene and enable restore overlay");
                Require(menu.gameObject.activeInHierarchy && menu.interactable, "Menu button remains accessible with hidden dialogue");
                typeof(DialogueController).GetField("revealProgress", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, 1000f);
                Invoke(controller, "Update");
                Require(controller.IsTyping && body.text == partial && controller.PageIndex == 0, "Hidden dialogue must pause typing and progression");
                Click(screen.Find("Restore Dialogue").GetComponent<Button>());
                Require(!controller.IsDialogueHidden && controller.PageIndex == 0 && body.text == partial, "Restore must not advance or reveal text");
                controller.HideDialogue();
                controller.Advance();
                Require(!controller.IsDialogueHidden && controller.PageIndex == 0 && body.text == partial, "Advance shortcut must only restore hidden dialogue");
                Click(panel.GetComponent<Button>());
                Require(!controller.IsTyping && controller.PageIndex == 0 && panel.Find("Next Icon").gameObject.activeSelf, "First click completes current text");
                Click(panel.GetComponent<Button>());
                Require(controller.PageIndex == 1, "Second click advances exactly one page");
                controller.HideDialogue();
                controller.RestoreDialogue();
                Require(controller.PageIndex == 1 && !controller.IsDialogueHidden, "Repeated hide/restore must preserve page");
                Require(before == screen.GetComponentsInChildren<Transform>(true).Length, "Runtime binding must not create UI");
                Property(controller, "IsPlaying", false);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        CanvasValidation.Validate();
        if (File.ReadAllText("Logs/CanvasValidation.result").StartsWith("FAIL")) throw new Exception("Canvas validation failed");
        File.WriteAllText("Logs/EngineInteractionValidation.result", "PASS: five dialogue Canvases, click/reveal/advance, hide/restore without progression, paused typing, authored UI, star thresholds, legacy save conversion and best-score persistence");
        Debug.Log("ENGINE_INTERACTION_VALIDATION_PASS");
    }

    static void ValidatePortraitHeight(DialogueController controller)
    {
        var texture = new Texture2D(64, 128);
        var sprites = new[] {
            Sprite.Create(texture, new Rect(0, 0, 64, 128), Vector2.one / 2),
            Sprite.Create(texture, new Rect(0, 0, 64, 103), Vector2.one / 2),
            Sprite.Create(texture, new Rect(0, 0, 64, 110), Vector2.one / 2)
        };
        try
        {
            foreach (string side in new[] { "left", "right" })
            {
                var image = Field<Image>(controller, side + "Character");
                var focus = Field<object>(controller, side + "Focus");
                float height = image.rectTransform.rect.height;
                foreach (var sprite in sprites)
                {
                    Invoke(controller, "SetCharacter", image, sprite, null, false, true, side == "right", focus, true);
                    var bounds = (Vector4)typeof(Image).GetMethod("GetDrawingDimensions", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(image, new object[] { true });
                    Require(Mathf.Abs(bounds.w - bounds.y - height) < 0.01f,
                        "Different portrait aspect ratios must retain the full authored drawing height");
                    Require(Mathf.Abs(image.rectTransform.rect.width / height - sprite.rect.width / sprite.rect.height) < 0.0001f,
                        "Portraits must retain their original proportions");
                }
                Invoke(controller, "SetCharacter", image, null, null, false, true, side == "right", focus, true);
            }
        }
        finally
        {
            foreach (var sprite in sprites) UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    static void ValidateStars()
    {
        Require(PuzzleRating.StarsFor(8,8) == 3 && PuzzleRating.StarsFor(10,8) == 3, "Three star threshold");
        Require(PuzzleRating.StarsFor(11,8) == 2 && PuzzleRating.StarsFor(16,8) == 2, "Two star threshold");
        Require(PuzzleRating.StarsFor(17,8) == 1 && PuzzleRating.StarsFor(int.MaxValue,1) == 1, "One star floor and overflow safety");
        string game = "EngineValidation." + Guid.NewGuid().ToString("N");
        try
        {
            for (int grade = 1; grade <= 5; grade++)
            {
                PlayerPrefs.SetInt(game + ".BestGrade.0.0", grade);
                Require(PuzzleProgress.BestStars(0,0,game) == (grade <= 2 ? 3 : grade <= 4 ? 2 : 1), "Legacy grade conversion");
            }
            PuzzleProgress.Complete(0,0,1,11,8,game);
            Require(PuzzleProgress.BestStars(0,0,game) == 2, "Improve legacy one-star result");
            PuzzleProgress.Complete(0,0,1,17,8,game);
            Require(PuzzleProgress.BestStars(0,0,game) == 2, "Never lower a best result");
            PuzzleProgress.Complete(0,0,1,8,8,game);
            Require(PuzzleProgress.BestStars(0,0,game) == 3 && PuzzleProgress.CompletedMask(0,game) == 1, "Three-star completion persists");
        }
        finally
        {
            foreach (string key in new[] { ".BestGrade.0.0", ".BestStars.0.0", ".Completed.0", ".Unlocked", ".StarEconomy.v1" }) PlayerPrefs.DeleteKey(game + key);
            PlayerPrefs.Save();
        }
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Click(Button button)
    {
        // Exercise the actual bindings without creating persistent audio in an editor test.
        var instance = typeof(GameAudio).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        var previous = instance.GetValue(null);
        var audio = new GameObject("Validation Audio", typeof(GameAudio));
        try { instance.SetValue(null,audio.GetComponent<GameAudio>()); button.onClick.Invoke(); }
        finally { instance.SetValue(null,previous); UnityEngine.Object.DestroyImmediate(audio); }
    }
    static T Field<T>(object source, string field) => (T)source.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(source);
    static void Property(object source, string property, object value) => source.GetType().GetProperty(property).SetValue(source,value);
    static void Invoke(object source, string method, params object[] arguments) => source.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(source,arguments);
}
