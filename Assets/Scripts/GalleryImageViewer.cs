using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GalleryImageViewer : MonoBehaviour, IScrollHandler, IDragHandler
{
    private RawImage artwork;
    private RectTransform viewport;
    private float zoom = 1;
    private Vector2 pan;

    public void Build(Texture2D texture, Action close, Action selectBackground = null, bool selected = false)
    {
        artwork = MenuControls.Get<RawImage>(transform, "Viewport/Artwork");
        viewport = MenuControls.Get<RectTransform>(transform, "Viewport");
        MenuControls.SetTexture(artwork, texture, false);
        zoom = 1;
        pan = Vector2.zero;
        LayoutImage();
        var controls = MenuControls.Get<RectTransform>(transform, "Controls");
        MenuControls.SafeArea(controls);
        MenuControls.Bind(controls, "Back", () => close());
        MenuControls.Bind(controls, "Zoom In", () => ChangeZoom(0.5f));
        MenuControls.Bind(controls, "Zoom Out", () => ChangeZoom(-0.5f));
        var use = MenuControls.Bind(controls, "Use Background", () =>
        {
            if (selectBackground == null) return;
            selectBackground();
            CanvasText.Set(MenuControls.Get<Text>(controls, "Use Background/Label"), 1);
            MenuControls.Get<Button>(controls, "Use Background").interactable = false;
        });
        use.gameObject.SetActive(selectBackground != null);
        use.interactable = selectBackground != null && !selected;
        CanvasText.Set(MenuControls.Get<Text>(controls, "Use Background/Label"), selected ? 1 : 0);
    }

    public void OnScroll(PointerEventData data) => ChangeZoom(data.scrollDelta.y * 0.2f);

    public void OnDrag(PointerEventData data)
    {
        if (zoom <= 1 || artwork == null) return;
        pan += data.delta / GetComponentInParent<Canvas>().scaleFactor;
        LayoutImage();
    }

    private void ChangeZoom(float amount)
    {
        zoom = Mathf.Clamp(zoom + amount, 1, 4);
        LayoutImage();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (artwork != null) LayoutImage();
    }

    private void LayoutImage()
    {
        if (artwork == null || artwork.texture == null) return;
        var bounds = viewport.rect.size;
        float fit = Mathf.Min(bounds.x / artwork.texture.width, bounds.y / artwork.texture.height);
        var size = new Vector2(artwork.texture.width, artwork.texture.height) * fit * zoom;
        artwork.rectTransform.sizeDelta = size;
        var limit = Vector2.Max(Vector2.zero, (size - bounds) / 2);
        pan = new Vector2(Mathf.Clamp(pan.x, -limit.x, limit.x), Mathf.Clamp(pan.y, -limit.y, limit.y));
        artwork.rectTransform.anchoredPosition = pan;
        MenuControls.Get<Button>(transform, "Controls/Zoom In").interactable = zoom < 4;
        MenuControls.Get<Button>(transform, "Controls/Zoom Out").interactable = zoom > 1;
    }
}

