using System;
using System.Collections.Generic;
using UnityEngine;
using static PuzzleUI;

public class PuzzleGallery : MonoBehaviour
{
    private const int PageSize = 6;

    public void Build(PuzzleCatalog catalog, int page, Action<int> changePage, Action<int> open, Action close)
    {
        var content = (RectTransform)transform;
        Label(content, "GALERÍA", 0, 402, 680, 60, 32, Ink);
        var completed = new List<int>();
        for (int i = 0; i < catalog.Count; i++)
            if (StoryProgress.IsPuzzleUnlocked(catalog, i))
                for (int difficulty = 0; difficulty < 3; difficulty++)
                    if ((PuzzleProgress.CompletedMask(i, catalog.ProgressId) & (1 << difficulty)) != 0
                        && catalog[i].ImageForDifficulty(difficulty) != null)
                        completed.Add(i * 3 + difficulty);

        int pages = Mathf.Max(1, Mathf.CeilToInt(completed.Count / (float)PageSize));
        page = Mathf.Clamp(page, 0, pages - 1);
        if (completed.Count == 0)
            Label(content, "Resuelve un puzle para añadir su imagen.", 0, 35, 640, 100, 24, Muted);

        for (int slot = 0; slot < PageSize && page * PageSize + slot < completed.Count; slot++)
        {
            int index = completed[page * PageSize + slot];
            int puzzle = index / 3;
            int difficulty = index % 3;
            float x = (slot % 3 - 1) * 226;
            float y = 215 - slot / 3 * 285;
            var card = Button(content, "", x, y, 210, 250, () => open(index));
            card.name = "Gallery Image " + index;
            var rect = (RectTransform)card.transform;
            Preview(rect, catalog[puzzle].ImageForDifficulty(difficulty), 0, 25, 180);
            Label(rect, catalog[puzzle].title, 0, -77, 200, 34, 20, Ink);
            Label(rect, PuzzleSession.DifficultyNames[difficulty], 0, -108, 200, 28, 16, Muted);
        }

        if (pages > 1)
        {
            int current = page;
            Button(content, "‹", -180, -365, 72, 58, () => changePage(current - 1)).interactable = page > 0;
            Label(content, $"{page + 1} / {pages}", 0, -365, 180, 58, 23, Ink);
            Button(content, "›", 180, -365, 72, 58, () => changePage(current + 1)).interactable = page + 1 < pages;
        }
        Button(content, "Volver", 0, -447, 260, 48, () => close());
    }
}
