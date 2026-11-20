using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleChallenges : MonoBehaviour
{
    public void Build(PuzzleCatalog catalog, int page, Action<int> navigate, Action<int> play, Action shop, Action close)
    {
        var items = new List<int>();
        for (int i = 0; i < catalog.Count; i++)
            if (catalog[i].image != null || catalog[i].normalImage != null || catalog[i].hardImage != null) items.Add(i);
        int pages = Mathf.Max(1, Mathf.CeilToInt(items.Count / 6f));
        page = Mathf.Clamp(page, 0, pages - 1);
        transform.Find("Empty").gameObject.SetActive(items.Count == 0);
        for (int slot = 0; slot < 6; slot++)
        {
            var row = transform.Find("Row " + slot);
            int offset = page * 6 + slot;
            row.gameObject.SetActive(offset < items.Count);
            if (offset >= items.Count) continue;
            int index = items[offset];
            bool available = StarEconomy.CanPlayPuzzle(catalog, index);
            bool paid = catalog[index].starCost > 0;
            int best = 0;
            for (int d = 0; d < 3; d++) best = Mathf.Max(best, PuzzleProgress.BestStars(index, d, catalog.ProgressId));
            MenuControls.Get<Text>(row, "Title").text = catalog[index].title;
            var image = catalog[index].image ?? catalog[index].normalImage ?? catalog[index].hardImage;
            MenuControls.SetTexture(MenuControls.Get<RawImage>(row, "Artwork"), available ? image : null);
            CanvasText.Set(MenuControls.Get<Text>(row, "Price"), available ? (best > 0 ? 1 : 0) : (paid ? 2 : 3));
            var stars = MenuControls.Get<PuzzleStarsGraphic>(row, "Stars");
            stars.Stars = best; stars.gameObject.SetActive(available && best > 0);
            CanvasText.Set(MenuControls.Get<Text>(row, "Buy/Label"), available ? 0 : paid ? 1 : 2);
            MenuControls.Bind(row, "Buy", () => { if (available) play(index); else if (paid) shop(); }).interactable = available || paid;
        }
        int current = page;
        transform.Find("Pages").gameObject.SetActive(pages > 1);
        MenuControls.Bind(transform, "Pages/Previous", () => navigate(current-1)).interactable = page > 0;
        CanvasText.Set(MenuControls.Get<Text>(transform, "Pages/Number"), 0, page+1, pages);
        MenuControls.Bind(transform, "Pages/Next", () => navigate(current+1)).interactable = page+1 < pages;
        MenuControls.Bind(transform, "Back", () => close());
    }
}
