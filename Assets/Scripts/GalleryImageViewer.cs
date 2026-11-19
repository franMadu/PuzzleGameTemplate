using System;
using UnityEngine;
using UnityEngine.UI;

public class GalleryImageViewer : MonoBehaviour
{
    public void Build(Texture2D texture, Action close)
    {
        var root = (RectTransform)transform;
        var background = GetComponent<Image>();
        background.color = Color.black;
        var image = PuzzleUI.Rect("Full Image", root, 0, 0, 0, 0);
        var graphic = image.gameObject.AddComponent<RawImage>();
        graphic.texture = texture;
        graphic.raycastTarget = false;
        var fit = image.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectRatio = (float)texture.width / texture.height;
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        var button = gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        ButtonSound.Bind(button);
        button.onClick.AddListener(() =>
        {
            if (ScreenTransition.IsBusy || !button.IsInteractable()) return;
            button.interactable = false;
            close();
        });
    }
}
