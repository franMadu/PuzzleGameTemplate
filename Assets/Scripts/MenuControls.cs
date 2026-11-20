using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Controls are authored in the scene. Runtime code only binds actions and updates values.
public static class MenuControls
{
    public static T Get<T>(Transform root, string path) where T : Component
        => root.Find(path).GetComponent<T>() ?? root.Find(path).GetComponentInChildren<T>(true);

    public static Button Bind(Transform root, string path, UnityAction action)
    {
        var button = Get<Button>(root, path);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (ScreenTransition.IsBusy) return;
            GameAudio.Click();
            action();
        });
        return button;
    }

    public static void SetTexture(RawImage image, Texture2D texture, bool crop = true)
    {
        image.texture = texture;
        image.gameObject.SetActive(texture != null);
        image.uvRect = new Rect(0, 0, 1, 1);
        var fitter = image.GetComponent<AspectRatioFitter>();
        if (fitter != null && texture != null)
        {
            fitter.aspectRatio = texture.width / (float)texture.height;
            return;
        }
        if (!crop || texture == null) return;
        float side = Mathf.Min(texture.width, texture.height);
        float width = side / texture.width, height = side / texture.height;
        image.uvRect = new Rect((1 - width) / 2, (1 - height) / 2, width, height);
    }

    public static void SafeArea(RectTransform content)
    {
        var safe = Screen.safeArea;
        var size = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        content.anchorMin = safe.min / size;
        content.anchorMax = safe.max / size;
        content.offsetMin = new Vector2(24, 24);
        content.offsetMax = new Vector2(-24, -24);
    }
}
