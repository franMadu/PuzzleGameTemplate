using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Author the navigation control in the scene; runtime only binds its action.
public static class DialogueMenuCanvasUpgrade
{
    [MenuItem("Tools/One Hand Puzzle/Add Main Menu Button to Dialogues")]
    public static void Run()
    {
        foreach (string name in new[] { "Story", "DialogueDemo", "PuzzleEasy", "PuzzleNormal", "PuzzleHard" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            var controller = components.OfType<DialogueController>().Single();
            var screen = (RectTransform)new SerializedObject(controller).FindProperty("screen").objectReferenceValue;
            if (screen.Find("Back to Menu") != null) continue;
            var template = components.OfType<Button>().First(button =>
                (button.name == "Back" || button.name == "Back to Menu") && button.GetComponentInChildren<Text>(true) != null);
            var owner = UnityEngine.Object.Instantiate(template.gameObject, screen);
            owner.name = "Back to Menu";
            owner.SetActive(true);
            owner.transform.SetAsLastSibling();
            var rect = (RectTransform)owner.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -20);
            rect.sizeDelta = new Vector2(206, 56);
            rect.localScale = Vector3.one;
            var button = owner.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.interactable = true;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = owner.GetComponentInChildren<Text>(true);
            var formats = label.GetComponent<CanvasText>();
            if (formats != null) UnityEngine.Object.DestroyImmediate(formats);
            label.text = "Menú principal";
            label.fontSize = 22;
            label.resizeTextForBestFit = false;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("DIALOGUE_MENU_CANVAS_UPGRADE_PASS");
    }
}
