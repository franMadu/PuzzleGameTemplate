using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-time editor authoring tool. Runtime controllers never create static UI objects.
public static class EngineCanvasUpgrade
{
    [MenuItem("Tools/One Hand Puzzle/Upgrade Dialogue Controls and Stars")]
    public static void Run()
    {
        foreach (var name in new[] { "MainMenu", "Story", "PuzzleEasy", "PuzzleNormal", "PuzzleHard" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            foreach (var controller in components.OfType<DialogueController>()) UpgradeDialogue(controller);
            foreach (var controller in components.OfType<PuzzleSceneController>())
            {
                var content = Reference<RectTransform>(controller, "victoryScreen").Find("Content");
                var grade = content.Find("Grade");
                var formats = grade.GetComponent<CanvasText>();
                if (formats != null) UnityEngine.Object.DestroyImmediate(formats);
                var text = grade.GetComponent<Text>();
                if (text != null) UnityEngine.Object.DestroyImmediate(text);
                Stars(grade.gameObject);
                var best = content.Find("Best").GetComponent<Text>();
                best.alignment = TextAnchor.MiddleLeft;
                best.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400);
                SetFormats(best.GetComponent<CanvasText>(), "Mejor resultado");
                ChildStars(best.transform, new Vector2(110, 0), new Vector2(150, 38));
            }
            if (name == "MainMenu")
            {
                var grade = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Text>(true))
                    .Single(label => label.name == "Grade");
                grade.alignment = TextAnchor.MiddleLeft;
                SetFormats(grade.GetComponent<CanvasText>(), "Sin completar", "Mejor resultado");
                ChildStars(grade.transform, new Vector2(210, 0), new Vector2(160, 42));
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("ENGINE_CANVAS_UPGRADE_PASS");
    }

    static T Reference<T>(UnityEngine.Object owner, string field) where T : UnityEngine.Object
        => (T)new SerializedObject(owner).FindProperty(field).objectReferenceValue;

    static void UpgradeDialogue(DialogueController controller)
    {
        var screen = Reference<RectTransform>(controller, "screen");
        var panel = (RectTransform)screen.Find("Content/Dialogue Box");
        var graphic = panel.GetComponent<Image>();
        graphic.raycastTarget = true;
        var advance = panel.GetComponent<Button>() ?? panel.gameObject.AddComponent<Button>();
        advance.targetGraphic = graphic;
        advance.transition = Selectable.Transition.None;
        advance.navigation = new Navigation { mode = Navigation.Mode.None };
        var icon = panel.Find("Advance");
        if (icon != null)
        {
            UnityEngine.Object.DestroyImmediate(icon.GetComponent<Button>());
            icon.name = "Next Icon";
            icon.GetComponent<Graphic>().raycastTarget = false;
            var rect = (RectTransform)icon;
            rect.sizeDelta = new Vector2(32, 32);
            rect.anchoredPosition = new Vector2(-28, 28);
            icon.gameObject.SetActive(false);
        }
        if (panel.Find("Hide") == null)
        {
            var hide = UnityEngine.Object.Instantiate(panel.Find("Skip").gameObject, panel);
            hide.name = "Hide";
            ((RectTransform)hide.transform).anchoredPosition = new Vector2(110, 34);
            hide.GetComponent<DialogueControlGraphic>().SetHideIcon(true);
            hide.GetComponent<Button>().onClick.RemoveAllListeners();
        }
        if (screen.Find("Restore Dialogue") == null)
        {
            var restore = new GameObject("Restore Dialogue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            restore.transform.SetParent(screen, false);
            restore.layer = panel.gameObject.layer;
            var rect = (RectTransform)restore.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            restore.GetComponent<Image>().color = Color.clear;
            var button = restore.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            restore.SetActive(false);
        }
    }

    static PuzzleStarsGraphic Stars(GameObject owner)
    {
        var stars = owner.GetComponent<PuzzleStarsGraphic>() ?? owner.AddComponent<PuzzleStarsGraphic>();
        stars.color = new Color(1f, 0.77f, 0.27f, 1f);
        stars.raycastTarget = false;
        return stars;
    }
    static void ChildStars(Transform parent, Vector2 position, Vector2 size)
    {
        var child = parent.Find("Stars");
        if (child != null) return;
        var go = new GameObject("Stars", typeof(RectTransform), typeof(CanvasRenderer), typeof(PuzzleStarsGraphic));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Stars(go);
    }
    static void SetFormats(CanvasText source, params string[] values)
    {
        var serialized = new SerializedObject(source);
        var formats = serialized.FindProperty("formats");
        formats.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) formats.GetArrayElementAtIndex(i).stringValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        source.GetComponent<Text>().text = values[0];
    }
}
