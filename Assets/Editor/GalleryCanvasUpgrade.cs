using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class GalleryCanvasUpgrade
{
    [MenuItem("Tools/One Hand Puzzle/Add Gallery Categories to Canvas")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        PuzzleGallery gallery = null;
        foreach (var root in scene.GetRootGameObjects())
            if ((gallery = root.GetComponentInChildren<PuzzleGallery>(true)) != null) break;
        var content = gallery.transform;
        foreach (var name in new[] { "Puzzles", "Wallpapers" })
        {
            if (content.Find(name) != null) continue;
            var go = UnityEngine.Object.Instantiate(content.Find("Back").gameObject,content);
            go.name = name;
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = new Vector2(name == "Puzzles" ? -170 : 170,344);
            rect.sizeDelta = new Vector2(320,44);
            var button = go.GetComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.RemoveAllListeners();
            go.transform.Find("Label").GetComponent<Text>().text = name == "Puzzles" ? "Puzles" : "Fondos de pantalla";
        }
        var empty = content.Find("Empty").GetComponent<Text>();
        var source = empty.GetComponent<CanvasText>() ?? empty.gameObject.AddComponent<CanvasText>();
        var serialized = new SerializedObject(source);
        var formats = serialized.FindProperty("formats");
        formats.arraySize = 2;
        formats.GetArrayElementAtIndex(0).stringValue = "Completa puzles para ver sus imágenes aquí.";
        formats.GetArrayElementAtIndex(1).stringValue = "Todavía no hay fondos de pantalla disponibles.";
        serialized.ApplyModifiedPropertiesWithoutUndo();
        empty.text = formats.GetArrayElementAtIndex(0).stringValue;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("GALLERY_CANVAS_UPGRADE_PASS");
    }
}
