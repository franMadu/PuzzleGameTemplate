using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Editor authoring only: all panels, labels and controls are saved in their scene Canvas.
public static class GameplayCanvasUpgrade
{
    private static readonly string[] Scenes = { "Story", "PuzzleEasy", "PuzzleNormal", "PuzzleHard" };
    private static readonly Color Gold = new Color(.85f, .69f, .40f, 1);
    private static readonly Color Surface = new Color(.065f, .09f, .12f, .96f);
    private static Font font;

    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += RepairOpenScenes;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RepairOpenScenes;
    }
    private static void RepairOpenScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var dialogue in Resources.FindObjectsOfTypeAll<DialogueController>())
        {
            var scene = dialogue.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)
                || !Scenes.Contains(scene.name)) continue;
            if (new SerializedObject(dialogue).FindProperty("visualStyleVersion").intValue < 1) Apply(dialogue);
        }
    }

    [MenuItem("Tools/One Hand Puzzle/Style Dialogue and Puzzle Screens")]
    public static void Run()
    {
        foreach (string name in Scenes)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            Apply(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DialogueController>(true)).Single());
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("GAMEPLAY_CANVAS_STYLE_PASS");
    }

    public static void Apply(DialogueController dialogue)
    {
        var screen = Reference<RectTransform>(dialogue, "screen");
        var box = screen.Find("Content/Dialogue Box");
        font = box.Find("Body").GetComponent<Text>().font;
        screen.GetComponent<Image>().color = Color.clear;
        box.GetComponent<Image>().color = new Color(Surface.r, Surface.g, Surface.b, .78f);
        var top = Node(box, "Top Accent", true);
        Place(top, 0, 1, 1, 1); top.sizeDelta = new Vector2(0, 2);
        top.GetComponent<Image>().color = Gold;
        var body = box.Find("Body").GetComponent<Text>();
        Place(body.transform, 0, 0, 1, 1);
        body.rectTransform.offsetMin = new Vector2(30, 76);
        body.rectTransform.offsetMax = new Vector2(-30, -20);
        body.color = PuzzleUI.Ink;
        body.fontSize = 34; body.lineSpacing = 1; body.resizeTextForBestFit = false;
        var thought = new SerializedObject(body.GetComponent<DialogueTextStyle>());
        thought.FindProperty("spokenColor").colorValue = PuzzleUI.Ink;
        thought.FindProperty("thoughtColor").colorValue = new Color(.70f, .82f, 1, 1);
        thought.ApplyModifiedPropertiesWithoutUndo();
        var speaker = (RectTransform)box.Find("Speaker Name Box");
        speaker.sizeDelta = new Vector2(260, 44);
        speaker.GetComponent<Image>().color = PuzzleUI.Accent;
        var name = speaker.Find("Name").GetComponent<Text>();
        Place(name.transform, 0, 0, 1, 1, 8); name.fontSize = 23; name.color = PuzzleUI.Ink;
        DialogueButton(box.Find("Hide"), "Ocultar", true, 20);
        DialogueButton(box.Find("Skip"), "Saltar", false, 178);
        var next = (RectTransform)box.Find("Next Icon");
        Fixed(next, Vector2.right, new Vector2(-30, 32), new Vector2(32, 32), Vector2.one / 2);
        next.GetComponent<Graphic>().color = Gold;
        var menu = screen.Find("Back to Menu");
        Fixed(menu, new Vector2(0, 1), new Vector2(24, -24), new Vector2(206, 56), new Vector2(0, 1));
        StyleButton(menu, false); Caption(menu.Find("Label"), "Menú principal", 20);
        // Keep the menu above the full-screen restore target, including when text is hidden.
        menu.SetAsLastSibling();
        var root = dialogue.transform;
        var background = root.Find("Background");
        if (background != null) background.GetComponent<Image>().color = PuzzleUI.Background;
        var puzzle = root.GetComponent<PuzzleSceneController>();
        if (puzzle != null) Puzzle(puzzle);
        var story = root.GetComponent<StorySceneController>();
        if (story != null)
        {
            Finish(Reference<RectTransform>(story, "endingScreen"), "Title", "Ending");
            Finish(Reference<RectTransform>(story, "errorScreen"), null, "Error");
        }
        var serialized = new SerializedObject(dialogue);
        serialized.FindProperty("visualStyleVersion").intValue = 1;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(dialogue.gameObject.scene);
    }

    private static void DialogueButton(Transform button, string caption, bool hide, float left)
    {
        var old = button.GetComponent<DialogueControlGraphic>();
        if (old != null) Object.DestroyImmediate(old);
        if (button.GetComponent<Image>() == null) button.gameObject.AddComponent<Image>();
        Fixed(button, Vector2.zero, new Vector2(left, 14), new Vector2(144, 44), Vector2.zero);
        StyleButton(button, false);
        var icon = Node(button, "Icon"); Place(icon, .07f, .18f, .27f, .82f);
        var graphic = icon.GetComponent<DialogueControlGraphic>() ?? icon.gameObject.AddComponent<DialogueControlGraphic>();
        graphic.SetHideIcon(hide); graphic.SetCloseIcon(false); graphic.color = Gold; graphic.raycastTarget = false;
        var label = Label(button, "Label", caption, 19); Place(label.transform, .29f, 0, .98f, 1);
    }

    private static void Puzzle(PuzzleSceneController puzzle)
    {
        var layout = Reference<GameObject>(puzzle, "gameLayout").transform;
        Stretch(layout); Header(layout);
        var back = layout.Find("Back to Menu");
        Place(back, 0, .90f, .12f, .99f); StyleButton(back, false); Caption(back.Find("Label"), "Volver", 20);
        Place(layout.Find("Title"), .145f, .90f, .60f, .99f);
        var title = layout.Find("Title").GetComponent<Text>(); title.fontSize = 30; title.color = PuzzleUI.Ink; title.alignment = TextAnchor.MiddleLeft;
        Formats(title, "{0} · Fácil", "{0} · Normal", "{0} · Difícil");
        var frame = (RectTransform)layout.Find("Board Frame");
        Fixed(frame, Vector2.one / 2, new Vector2(0, 18), new Vector2(664, 664), Vector2.one / 2);
        frame.GetComponent<Image>().color = new Color(.34f, .29f, .20f, 1);
        var board = Reference<RectTransform>(puzzle.GetComponent<Puzzle>(), "board");
        Fixed(board, Vector2.one / 2, Vector2.zero, new Vector2(640, 640), Vector2.one / 2);
        board.GetComponent<Image>().color = PuzzleUI.Panel;
        Place(layout.Find("Status"), .20f, .105f, .80f, .15f);
        var status = layout.Find("Status").GetComponent<Text>(); status.fontSize = 23; status.color = PuzzleUI.Ink;
        Formats(status, "Movimientos: {0} · Mínimo: {1}");
        Place(layout.Find("Instructions"), .02f, .01f, .59f, .085f);
        layout.Find("Instructions").gameObject.SetActive(true);
        Caption(layout.Find("Instructions"), "Desliza las piezas para reconstruir la imagen.", 20);
        layout.Find("Instructions").GetComponent<Text>().color = PuzzleUI.Muted;
        Place(layout.Find("Shuffle Button"), .65f, .01f, .98f, .085f);
        StyleButton(layout.Find("Shuffle Button"), false);
        Place(layout.Find("Shuffle Button/Shuffle Label"), 0, 0, 1, 1, 6);
        Caption(layout.Find("Shuffle Button/Shuffle Label"), "Volver a mezclar", 23);
        Place(layout.Find("Continue After Reveal"), .65f, .01f, .98f, .085f);
        StyleButton(layout.Find("Continue After Reveal"), true);
        Caption(layout.Find("Continue After Reveal/Label"), "Ver resultado", 23);
        Victory(Reference<RectTransform>(puzzle, "victoryScreen"));
    }

    private static void Victory(RectTransform panel)
    {
        panel.GetComponent<Image>().color = Surface;
        var c = panel.Find("Content"); Stretch(c); Header(c);
        Place(c.Find("Title"), .04f, .90f, .60f, .99f);
        var title = c.Find("Title").GetComponent<Text>(); title.fontSize = 30; title.color = PuzzleUI.Ink; title.alignment = TextAnchor.MiddleLeft;
        var label = Label(c, "Result Label", "PUZLE COMPLETADO", 20); label.color = Gold;
        Place(label.transform, .56f, .77f, .96f, .83f);
        var slot = c.Find("Artwork");
        Transform art;
        if (slot.GetComponent<RawImage>() != null)
        {
            art = slot;
            slot = Node(c, "Artwork Area");
            art.SetParent(slot, false);
            slot.name = "Artwork"; art.name = "Image";
        }
        else art = slot.Find("Image");
        Place(slot, .04f, .18f, .51f, .83f); Place(art, 0, 0, 1, 1);
        var fitter = art.GetComponent<AspectRatioFitter>() ?? art.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fitter.aspectRatio = 1;
        Place(c.Find("Grade"), .65f, .65f, .87f, .75f);
        c.Find("Grade").GetComponent<PuzzleStarsGraphic>().color = Gold;
        Place(c.Find("Moves"), .56f, .55f, .96f, .63f);
        var moves = c.Find("Moves").GetComponent<Text>(); moves.fontSize = 23; moves.color = PuzzleUI.Ink;
        Formats(moves, "{0} movimientos · Mínimo: {1}");
        Place(c.Find("Best"), .56f, .47f, .96f, .53f);
        var best = c.Find("Best").GetComponent<Text>(); best.fontSize = 21; best.color = PuzzleUI.Muted;
        Place(c.Find("Best/Stars"), .58f, .15f, 1, .85f);
        c.Find("Best/Stars").GetComponent<PuzzleStarsGraphic>().color = Gold;
        Place(c.Find("Star Reward"), .56f, .34f, .96f, .44f);
        var reward = c.Find("Star Reward").GetComponent<Text>(); reward.fontSize = 24; reward.color = Gold;
        Formats(reward, "+{0} estrellas\nSaldo disponible: {1}");
        Place(c.Find("Next"), .56f, .21f, .96f, .30f); StyleButton(c.Find("Next"), true);
        var next = c.Find("Next/Label").GetComponent<Text>(); next.fontSize = 24; next.color = PuzzleUI.Ink;
        Formats(next, "Volver a capítulos", "Continuar historia", "Siguiente puzle", "Ver desafíos");
        Place(c.Find("Play Again"), .56f, .11f, .755f, .18f); StyleButton(c.Find("Play Again"), false);
        Caption(c.Find("Play Again/Label"), "Repetir", 22);
        Place(c.Find("Back"), .77f, .11f, .96f, .18f); StyleButton(c.Find("Back"), false);
        Caption(c.Find("Back/Label"), "Menú principal", 20);
    }

    private static void Finish(RectTransform panel, string heading, string message)
    {
        panel.GetComponent<Image>().color = Surface;
        var c = panel.Find("Content"); Stretch(c);
        var card = Node(c, "Summary Card", true); Place(card, .15f, .35f, .85f, .78f);
        card.SetAsFirstSibling(); card.GetComponent<Image>().color = PuzzleUI.Panel;
        var top = Node(card, "Top Accent", true); Place(top, 0, 1, 1, 1); top.sizeDelta = new Vector2(0, 2); top.GetComponent<Image>().color = Gold;
        if (heading != null) { Place(c.Find(heading), .20f, .61f, .80f, .74f); Caption(c.Find(heading), "HISTORIA COMPLETADA", 32); c.Find(heading).GetComponent<Text>().color = Gold; }
        if (message != null) { Place(c.Find(message), .20f, .40f, .80f, heading == null ? .72f : .60f); c.Find(message).GetComponent<Text>().color = PuzzleUI.Ink; }
        if (heading == null && message == null) { var text = Label(c, "Summary", "CONVERSACIÓN COMPLETADA", 30); text.color = Gold; Place(text.transform, .20f, .52f, .80f, .72f); }
        Place(c.Find("Back"), .30f, .20f, .70f, .30f); StyleButton(c.Find("Back"), true); Caption(c.Find("Back/Label"), "Menú principal", 24);
    }

    private static void Header(Transform content)
    {
        var bar = Node(content, "Header", true); Place(bar, 0, .89f, 1, 1); bar.SetAsFirstSibling();
        bar.GetComponent<Image>().color = new Color(.035f, .055f, .075f, 1);
        var wallet = Node(content, "Wallet", true); Place(wallet, .65f, .915f, .85f, .98f); wallet.GetComponent<Image>().color = PuzzleUI.Panel;
        var icon = Node(wallet, "Icon"); Place(icon, .04f, .20f, .29f, .80f);
        var star = icon.GetComponent<PuzzleStarsGraphic>() ?? icon.gameObject.AddComponent<PuzzleStarsGraphic>();
        star.Slots = 1; star.Stars = 1; star.color = Gold; star.raycastTarget = false;
        var amount = Label(wallet, "Amount", "0", 28); Place(amount.transform, .35f, .10f, .97f, .90f);
    }
    private static T Reference<T>(Object owner, string field) where T : Object => (T)new SerializedObject(owner).FindProperty(field).objectReferenceValue;
    private static RectTransform Node(Transform parent, string name, bool image = false)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing == null) { var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)); go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false); existing = (RectTransform)go.transform; }
        if (image && existing.GetComponent<Image>() == null) existing.gameObject.AddComponent<Image>();
        if (image) existing.GetComponent<Image>().raycastTarget = false;
        return existing;
    }
    private static Text Label(Transform parent, string name, string caption, int size)
    {
        var node = Node(parent, name); var label = node.GetComponent<Text>() ?? node.gameObject.AddComponent<Text>();
        label.font = font; label.text = caption; label.fontSize = size; label.color = PuzzleUI.Ink;
        label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; return label;
    }
    private static void Caption(Transform owner, string caption, int size)
    {
        var text = owner.GetComponent<Text>(); text.text = caption; text.fontSize = size;
        text.color = PuzzleUI.Ink; text.resizeTextForBestFit = false; text.raycastTarget = false;
    }
    private static void StyleButton(Transform owner, bool primary)
    {
        var button = owner.GetComponent<Button>(); var image = owner.GetComponent<Image>(); image.color = primary ? PuzzleUI.Accent : PuzzleUI.Panel; image.raycastTarget = true;
        button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1);
        colors.pressedColor = new Color(.8f, .8f, .8f, 1); colors.disabledColor = new Color(.5f, .5f, .5f, .65f); button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
    }
    private static void Stretch(Transform owner) { Place(owner, 0, 0, 1, 1, 24); }
    private static void Place(Transform owner, float x1, float y1, float x2, float y2, float inset = 0)
    {
        var rect = (RectTransform)owner; rect.anchorMin = new Vector2(x1, y1); rect.anchorMax = new Vector2(x2, y2);
        rect.offsetMin = Vector2.one * inset; rect.offsetMax = -Vector2.one * inset; rect.pivot = Vector2.one / 2; rect.localScale = Vector3.one;
    }
    private static void Fixed(Transform owner, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
    {
        var rect = (RectTransform)owner; rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
        rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
    }
    private static void Formats(Text label, params string[] values)
    {
        var source = label.GetComponent<CanvasText>() ?? label.gameObject.AddComponent<CanvasText>();
        var data = new SerializedObject(source); var formats = data.FindProperty("formats"); formats.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) formats.GetArrayElementAtIndex(i).stringValue = values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
