using UnityEngine;
using UnityEngine.UI;

public static class PuzzleUI
{
    public static readonly Color Background = new Color(0.043f, 0.078f, 0.102f);
    public static readonly Color Panel = new Color(0.09f, 0.16f, 0.19f);
    public static readonly Color Accent = new Color(0.24f, 0.48f, 0.47f);
    public static readonly Color Ink = new Color(0.94f, 0.9f, 0.8f);
    public static readonly Color Muted = new Color(0.63f, 0.73f, 0.75f);

    public static RectTransform Rect(string name, RectTransform parent, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    public static RectTransform Screen(string name, RectTransform parent)
    {
        var screen = Rect(name, parent, 0, 0, 0, 0);
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        screen.gameObject.AddComponent<Image>().color = Background;
        return screen;
    }

    public static Text Label(RectTransform parent, string text, float x, float y, float width, float height, int size, Color color)
    {
        var label = Rect("Label", parent, x, y, width, height).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    public static Button Button(RectTransform parent, string text, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(text.Length == 0 ? "Puzzle Card" : text, parent, x, y, width, height);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = Accent;
        var button = rect.gameObject.AddComponent<Button>();
        ButtonSound.Bind(button);
        button.targetGraphic = image;
        var colors = button.colors;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f);
        colors.pressedColor = new Color(0.7f, 0.85f, 0.85f);
        button.colors = colors;
        button.onClick.AddListener(action);
        if (text.Length > 0) Label(rect, text, 0, 0, width - 12, height, 23, Ink);
        return button;
    }

    public static void Preview(RectTransform parent, Texture2D texture, float x, float y, float size)
    {
        var image = Rect("Image Preview", parent, x, y, size, size).gameObject.AddComponent<RawImage>();
        image.texture = texture;
        float side = Mathf.Min(texture.width, texture.height);
        float width = side / texture.width;
        float height = side / texture.height;
        image.uvRect = new Rect((1 - width) / 2, (1 - height) / 2, width, height);
        image.raycastTarget = false;
    }
}
