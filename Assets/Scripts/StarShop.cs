using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Six reusable cards and the wallet are authored in MainMenu's Canvas.
public class StarShop : MonoBehaviour
{
    private const int PageSize = 6;
    public void Build(PuzzleCatalog catalog, int page, bool puzzles, Action<int, bool> navigate,
        Action<int> playPuzzle, Action close)
    {
        WallpaperProgress.Synchronize(catalog);
        var items = new List<int>();
        if (puzzles)
        {
            for (int i = 0; i < catalog.Count; i++)
                if (catalog[i].starCost > 0 && !string.IsNullOrWhiteSpace(catalog[i].id)
                    && (catalog[i].image != null || catalog[i].normalImage != null || catalog[i].hardImage != null)) items.Add(i);
        }
        else for (int i = 0; i < catalog.WallpaperCount; i++)
            if (catalog.WallpaperAt(i)?.image != null && catalog.WallpaperAt(i).starCost > 0) items.Add(i);
        int pages = Mathf.Max(1, Mathf.CeilToInt(items.Count / (float)PageSize));
        page = Mathf.Clamp(page, 0, pages - 1);
        int current = page;
        int balance = StarEconomy.Balance(catalog);
        MenuControls.Get<Text>(transform, "Wallet/Amount").text = balance.ToString();
        transform.Find("Empty").gameObject.SetActive(items.Count == 0);
        MenuControls.Bind(transform, "Wallpapers", () => navigate(0, false)).GetComponent<Image>().color = puzzles ? PuzzleUI.Panel : PuzzleUI.Accent;
        MenuControls.Bind(transform, "Puzzles", () => navigate(0, true)).GetComponent<Image>().color = puzzles ? PuzzleUI.Accent : PuzzleUI.Panel;
        for (int slot = 0; slot < PageSize; slot++)
        {
            var row = transform.Find("Row " + slot);
            int offset = page * PageSize + slot;
            row.gameObject.SetActive(offset < items.Count);
            if (offset >= items.Count) continue;
            int index = items[offset];
            bool owned = puzzles ? StarEconomy.OwnsPuzzle(catalog, index) : WallpaperProgress.IsUnlocked(catalog, index);
            int cost = puzzles ? catalog[index].starCost : catalog.WallpaperAt(index).starCost;
            Texture2D image = puzzles ? catalog[index].image ?? catalog[index].normalImage ?? catalog[index].hardImage : catalog.WallpaperAt(index).image;
            MenuControls.SetTexture(MenuControls.Get<RawImage>(row, "Artwork"), image);
            MenuControls.Get<Text>(row, "Title").text = puzzles ? catalog[index].title : image.name;
            CanvasText.Set(MenuControls.Get<Text>(row, "Price"), owned ? 1 : 0, cost);
            CanvasText.Set(MenuControls.Get<Text>(row, "Buy/Label"), owned ? (puzzles ? 1 : 2) : 0);
            MenuControls.Bind(row, "Buy", () =>
            {
                if (owned)
                {
                    if (puzzles) playPuzzle(index);
                    else { GamePreferences.SetMenuBackground(catalog, index); navigate(current, puzzles); }
                }
                else
                {
                    if (puzzles) StarEconomy.TryBuyPuzzle(catalog, index);
                    else StarEconomy.TryBuyWallpaper(catalog, index);
                    navigate(current, puzzles);
                }
            }).interactable = owned || balance >= cost;
        }
        transform.Find("Pages").gameObject.SetActive(pages > 1);
        MenuControls.Bind(transform, "Pages/Previous", () => navigate(current - 1, puzzles)).interactable = page > 0;
        CanvasText.Set(MenuControls.Get<Text>(transform, "Pages/Number"), 0, page + 1, pages);
        MenuControls.Bind(transform, "Pages/Next", () => navigate(current + 1, puzzles)).interactable = page + 1 < pages;
        MenuControls.Bind(transform, "Back", () => close());
    }
}
