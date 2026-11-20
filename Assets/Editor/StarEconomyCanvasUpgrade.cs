using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Editor authoring only. Runtime shop and wallet reuse these serialized Canvas objects.
public static class StarEconomyCanvasUpgrade
{
    [MenuItem("Tools/One Hand Puzzle/Add Star Economy to Canvas")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PuzzleMenu>(true)).Single();
        var serialized = new SerializedObject(menu);
        var main = ((RectTransform)serialized.FindProperty("mainScreen").objectReferenceValue).Find("Content");
        var template = main.Find("Recap").GetComponent<Button>();
        var font = template.GetComponentInChildren<Text>(true).font;
        Wallet(main, font, false);
        if (main.Find("Shop") == null)
            Button(main, "Shop", template, "Tienda", new Vector2(102, -110), new Vector2(180, 56), new Vector2(0, 1));
        if (serialized.FindProperty("shopScreen").objectReferenceValue == null)
        {
            var source = (RectTransform)serialized.FindProperty("chaptersScreen").objectReferenceValue;
            var shop = Object.Instantiate(source.gameObject, source.parent);
            shop.name = "Star Shop";
            var content = shop.transform.Find("Content");
            for (int i = 0; i < 6; i++) Object.DestroyImmediate(content.Find("Row " + i).gameObject);
            content.gameObject.AddComponent<StarShop>();
            var title = content.Find("Title").GetComponent<Text>();
            RemoveFormats(title);
            title.text = "Tienda";
            Wallet(content, font, true);
            Button(content, "Wallpapers", template, "Fondos de pantalla", new Vector2(-190, 350), new Vector2(360, 52));
            Button(content, "Puzzles", template, "Puzles y desafíos", new Vector2(190, 350), new Vector2(360, 52));
            var empty = content.Find("Empty").GetComponent<Text>();
            RemoveFormats(empty);
            empty.text = "No hay artículos disponibles en esta categoría.";
            for (int i = 0; i < 6; i++)
            {
                var row = Rect(content, "Row " + i, new Vector2(0, 245 - i * 100), new Vector2(760, 92));
                row.gameObject.AddComponent<Image>().color = PuzzleUI.Panel;
                var art = Rect(row, "Artwork", new Vector2(-326, 0), new Vector2(82, 82));
                art.gameObject.AddComponent<RawImage>().raycastTarget = false;
                Text(row, "Title", font, "Artículo", new Vector2(-100, 18), new Vector2(330, 38), 22, TextAnchor.MiddleLeft);
                Formats(Text(row, "Price", font, "0 estrellas", new Vector2(-100, -21), new Vector2(330, 30), 20, TextAnchor.MiddleLeft), "{0} estrellas", "Adquirido");
                var buy = Button(row, "Buy", template, "Comprar", new Vector2(267, 0), new Vector2(194, 58));
                Formats(buy.GetComponentInChildren<Text>(), "Comprar", "Jugar", "Usar fondo");
            }
            shop.SetActive(false);
            serialized.FindProperty("shopScreen").objectReferenceValue = shop.GetComponent<RectTransform>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        foreach (var name in new[] { "PuzzleEasy", "PuzzleNormal", "PuzzleHard" })
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var controller = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PuzzleSceneController>(true)).Single();
            var content = ((RectTransform)new SerializedObject(controller).FindProperty("victoryScreen").objectReferenceValue).Find("Content");
            if (content.Find("Star Reward") == null)
                Formats(Text(content, "Star Reward", content.Find("Best").GetComponent<Text>().font, "+0 estrellas · Saldo: 0",
                    new Vector2(0, -210), new Vector2(720, 40), 24, TextAnchor.MiddleCenter), "+{0} estrellas · Saldo: {1}");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("STAR_ECONOMY_CANVAS_UPGRADE_PASS");
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size, Vector2? anchor = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = anchor ?? Vector2.one / 2;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }
    private static Text Text(Transform parent, string name, Font font, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        var rect = Rect(parent, name, position, size);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = PuzzleUI.Ink;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
    private static Button Button(Transform parent, string name, Button template, string label, Vector2 position, Vector2 size, Vector2? anchor = null)
    {
        var owner = Object.Instantiate(template.gameObject, parent);
        owner.name = name;
        var rect = (RectTransform)owner.transform;
        rect.anchorMin = rect.anchorMax = anchor ?? Vector2.one / 2;
        rect.pivot = Vector2.one / 2;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        owner.SetActive(true);
        var button = owner.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var text = owner.GetComponentInChildren<Text>(true);
        RemoveFormats(text);
        text.text = label;
        text.fontSize = 22;
        text.resizeTextForBestFit = false;
        return button;
    }
    private static void Wallet(Transform parent, Font font, bool right)
    {
        if (parent.Find("Wallet") != null) return;
        var rect = Rect(parent, "Wallet", new Vector2(right ? -102 : 102, -38), new Vector2(180, 56), new Vector2(right ? 1 : 0, 1));
        rect.gameObject.AddComponent<Image>().color = PuzzleUI.Panel;
        var icon = Rect(rect, "Icon", new Vector2(-60, 0), new Vector2(38, 38)).gameObject.AddComponent<PuzzleStarsGraphic>();
        icon.Slots = 1; icon.Stars = 1;
        icon.color = new Color(1, 0.77f, 0.27f, 1);
        icon.raycastTarget = false;
        Text(rect, "Amount", font, "0", new Vector2(23, 0), new Vector2(112, 44), 28, TextAnchor.MiddleLeft);
    }
    private static void RemoveFormats(Text text)
    {
        if (text.GetComponent<CanvasText>() != null) Object.DestroyImmediate(text.GetComponent<CanvasText>());
    }
    private static void Formats(Text text, params string[] formats)
    {
        var source = text.GetComponent<CanvasText>() ?? text.gameObject.AddComponent<CanvasText>();
        var serialized = new SerializedObject(source);
        var entries = serialized.FindProperty("formats");
        entries.arraySize = formats.Length;
        for (int i = 0; i < formats.Length; i++) entries.GetArrayElementAtIndex(i).stringValue = formats[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
