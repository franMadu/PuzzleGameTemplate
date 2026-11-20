using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GalleryCategory { Puzzles, Wallpapers }

public class PuzzleGallery : MonoBehaviour
{
    private const int PageSize = 6;

    public void Build(PuzzleCatalog catalog, int page, Action<int> changePage, Action<int> open, Action close,
        GalleryCategory category = GalleryCategory.Puzzles, Action<GalleryCategory> changeCategory = null)
    {
        var completed = new List<int>();
        if (category == GalleryCategory.Wallpapers)
        {
            for (int i = 0; i < catalog.WallpaperCount; i++)
                if (catalog.WallpaperAt(i)?.image != null) completed.Add(i);
        }
        else for (int i = 0; i < catalog.Count; i++)
            if (StoryProgress.IsPuzzleUnlocked(catalog, i))
                for (int difficulty = 0; difficulty < 3; difficulty++)
                    if ((PuzzleProgress.CompletedMask(i, catalog.ProgressId) & (1 << difficulty)) != 0
                        && catalog[i].ImageForDifficulty(difficulty) != null)
                        completed.Add(i * 3 + difficulty);

        int pages = Mathf.Max(1, Mathf.CeilToInt(completed.Count / (float)PageSize));
        page = Mathf.Clamp(page, 0, pages - 1);
        transform.Find("Empty").gameObject.SetActive(completed.Count == 0);
        CanvasText.Set(MenuControls.Get<Text>(transform, "Empty"), category == GalleryCategory.Wallpapers ? 1 : 0);
        foreach (var choice in new[] { GalleryCategory.Puzzles, GalleryCategory.Wallpapers })
        {
            var captured = choice;
            string tab = choice == GalleryCategory.Puzzles ? "Puzzles" : "Wallpapers";
            var button = MenuControls.Bind(transform, tab, () => changeCategory?.Invoke(captured));
            button.GetComponent<Image>().color = choice == category ? PuzzleUI.Accent : PuzzleUI.Panel;
        }
        for (int slot = 0; slot < PageSize; slot++)
        {
            string path = "Image " + slot;
            var card = transform.Find(path);
            bool visible = page * PageSize + slot < completed.Count;
            card.gameObject.SetActive(visible);
            if (!visible) continue;
            int index = completed[page * PageSize + slot];
            MenuControls.Bind(transform, path, () => open(index));
            MenuControls.SetTexture(MenuControls.Get<RawImage>(card, "Artwork"),
                category == GalleryCategory.Wallpapers ? catalog.WallpaperAt(index).image : catalog[index / 3].ImageForDifficulty(index % 3));
        }

        int current = page;
        transform.Find("Pages").gameObject.SetActive(pages > 1);
        MenuControls.Bind(transform, "Pages/Previous", () => changePage(current - 1)).interactable = page > 0;
        CanvasText.Set(MenuControls.Get<Text>(transform, "Pages/Number"), 0, page + 1, pages);
        MenuControls.Bind(transform, "Pages/Next", () => changePage(current + 1)).interactable = page + 1 < pages;
        MenuControls.Bind(transform, "Back", () => close());
    }
}

