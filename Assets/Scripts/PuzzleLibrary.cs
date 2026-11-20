using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Replays have their own cursor; they never write the story checkpoint.
public class PuzzleLibrary
{
    private readonly PuzzleCatalog catalog;
    private readonly Func<string, RectTransform> screen;
    private readonly Action close;
    private const int PageSize = 6;

    public PuzzleLibrary(PuzzleCatalog catalog, Func<string, RectTransform> screen, Action close)
    { this.catalog = catalog; this.screen = screen; this.close = close; }

    public void Open()
    {
        PuzzleSession.EndReplay();
        if (PuzzleSession.LibraryChapter >= 0 && PuzzleSession.LibraryChapter < catalog.Story.Chapters.Length)
            Subchapters(PuzzleSession.LibraryChapter);
        else Chapters();
    }

    private void Chapters(int page = 0)
    {
        PuzzleSession.LibraryChapter = -1;
        var content = screen("Chapter Selection");
        var visible = new List<int>();
        for (int i = 0; i < catalog.Story.Chapters.Length; i++)
            foreach (var section in catalog.Story.Chapters[i].subchapters)
                if (StoryProgress.IsUnlocked(catalog, section)) { visible.Add(i); break; }
        page = ClampPage(page, visible.Count);
        content.Find("Empty").gameObject.SetActive(visible.Count == 0);
        Rows(content, page, visible.Count, row => catalog.Story.Chapters[visible[row]].title,
            row => Subchapters(visible[row]));
        Pages(content, page, visible.Count, Chapters);
        MenuControls.Bind(content, "Back", () => close());
    }

    private void Subchapters(int chapter, int page = 0)
    {
        PuzzleSession.LibraryChapter = chapter;
        var definition = catalog.Story.Chapters[chapter];
        var content = screen("Subchapter Selection");
        MenuControls.Get<Text>(content, "Title").text = definition.title;
        var visible = new List<StorySubchapter>();
        foreach (var section in definition.subchapters)
            if (StoryProgress.IsUnlocked(catalog, section)) visible.Add(section);
        page = ClampPage(page, visible.Count);
        int current = page;
        Rows(content, page, visible.Count, row => visible[row].title, row =>
        {
            var section = visible[row];
            if (catalog.Story.PuzzleIn(section) >= 0) PuzzleDetails(chapter, current, section, 0);
            else Launch(section, 0);
        });
        Pages(content, page, visible.Count, next => Subchapters(chapter, next));
        MenuControls.Bind(content, "Back", () => Chapters());
    }

    private static int ClampPage(int page, int count) => Mathf.Clamp(page, 0, Mathf.Max(0, (count - 1) / PageSize));

    private static void Rows(RectTransform content, int page, int count, Func<int, string> title, Action<int> open)
    {
        for (int slot = 0; slot < PageSize; slot++)
        {
            string path = "Row " + slot;
            int row = page * PageSize + slot;
            content.Find(path).gameObject.SetActive(row < count);
            if (row >= count) continue;
            MenuControls.Get<Text>(content, path + "/Label").text = title(row);
            MenuControls.Bind(content, path, () => open(row));
        }
    }

    private void PuzzleDetails(int chapter, int page, StorySubchapter section, int difficulty)
    {
        int puzzle = catalog.Story.PuzzleIn(section);
        var content = screen("Puzzle Difficulty Selection");
        MenuControls.Get<Text>(content, "Title").text = section.title;
        for (int i = 0; i < 3; i++)
        {
            int choice = i;
            var tab = MenuControls.Bind(content, "Difficulty " + i, () => PuzzleDetails(chapter, page, section, choice));
            tab.GetComponent<Image>().color = i == difficulty ? PuzzleUI.Accent : PuzzleUI.Panel;
        }
        var texture = catalog[puzzle].ImageForDifficulty(difficulty);
        bool completed = (PuzzleProgress.CompletedMask(puzzle, catalog.ProgressId) & (1 << difficulty)) != 0;
        MenuControls.SetTexture(MenuControls.Get<RawImage>(content, "Artwork"), completed ? texture : null);
        content.Find("Hidden").gameObject.SetActive(!completed || texture == null);
        CanvasText.Set(MenuControls.Get<Text>(content, "Grade"), completed ? 1 : 0);
        var stars = MenuControls.Get<PuzzleStarsGraphic>(content, "Grade/Stars");
        stars.Stars = completed ? Mathf.Max(1, PuzzleProgress.BestStars(puzzle, difficulty, catalog.ProgressId)) : 0;
        stars.gameObject.SetActive(completed);
        MenuControls.Bind(content, "Play", () => Launch(section, difficulty)).interactable = texture != null
            && (catalog[puzzle].starCost == 0 || StarEconomy.OwnsPuzzle(catalog, puzzle));
        MenuControls.Bind(content, "Back", () => Subchapters(chapter, page));
    }

    private void Launch(StorySubchapter section, int difficulty)
    {
        if (!StoryProgress.IsUnlocked(catalog, section) || ScreenTransition.IsBusy) return;
        int puzzle = catalog.Story.PuzzleIn(section);
        if (puzzle >= 0 && catalog[puzzle].starCost > 0 && !StarEconomy.OwnsPuzzle(catalog, puzzle)) return;
        PuzzleSession.StoryMode = false;
        PuzzleSession.Difficulty = difficulty;
        PuzzleSession.ReplayIndex = catalog.Story.IndexOf(section.firstStepId);
        PuzzleSession.ReplayEnd = Mathf.Min(catalog.Story.IndexOf(section.lastStepId), StoryProgress.FurthestIndex(catalog));
        ScreenTransition.LoadScene(PuzzleSession.StoryScene);
    }

    private static void Pages(RectTransform content, int page, int count, Action<int> navigate)
    {
        int pages = Mathf.Max(1, Mathf.CeilToInt(count / (float)PageSize));
        content.Find("Pages").gameObject.SetActive(pages > 1);
        MenuControls.Bind(content, "Pages/Previous", () => navigate(page - 1)).interactable = page > 0;
        CanvasText.Set(MenuControls.Get<Text>(content, "Pages/Number"), 0, page + 1, pages);
        MenuControls.Bind(content, "Pages/Next", () => navigate(page + 1)).interactable = page + 1 < pages;
    }
}
