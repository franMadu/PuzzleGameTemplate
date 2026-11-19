using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PuzzleUI;

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
        Label(content, "CAPÍTULOS", 0, 402, 680, 60, 32, Ink);
        var visible = new List<int>();
        for (int i = 0; i < catalog.Story.Chapters.Length; i++)
            foreach (var section in catalog.Story.Chapters[i].subchapters)
                if (StoryProgress.IsUnlocked(catalog, section)) { visible.Add(i); break; }
        if (visible.Count == 0) Label(content, "Avanza en la historia para desbloquear capítulos.", 0, 50, 640, 100, 24, Muted);
        for (int slot = 0; slot < PageSize && page * PageSize + slot < visible.Count; slot++)
        {
            int index = visible[page * PageSize + slot];
            Button(content, catalog.Story.Chapters[index].title, 0, 280 - slot * 95, 660, 78, () => Subchapters(index));
        }
        Pages(content, page, visible.Count, Chapters);
        Button(content, "Volver", 0, -447, 260, 48, () => close());
    }

    private void Subchapters(int chapter, int page = 0)
    {
        PuzzleSession.LibraryChapter = chapter;
        var definition = catalog.Story.Chapters[chapter];
        var content = screen("Subchapter Selection");
        Label(content, definition.title, 0, 402, 680, 60, 32, Ink);
        var visible = new List<StorySubchapter>();
        foreach (var section in definition.subchapters)
            if (StoryProgress.IsUnlocked(catalog, section)) visible.Add(section);
        for (int slot = 0; slot < PageSize && page * PageSize + slot < visible.Count; slot++)
        {
            var section = visible[page * PageSize + slot];
            Button(content, section.title, 0, 280 - slot * 95, 660, 78, () =>
            {
                if (catalog.Story.PuzzleIn(section) >= 0) PuzzleDetails(chapter, page, section, 0);
                else Launch(section, 0);
            });
        }
        Pages(content, page, visible.Count, next => Subchapters(chapter, next));
        Button(content, "Volver", 0, -447, 260, 48, () => Chapters());
    }

    private void PuzzleDetails(int chapter, int page, StorySubchapter section, int difficulty)
    {
        int puzzle = catalog.Story.PuzzleIn(section);
        var content = screen("Puzzle Difficulty Selection");
        Label(content, section.title, 0, 402, 680, 60, 32, Ink);
        for (int i = 0; i < 3; i++)
        {
            int choice = i;
            var tab = Button(content, PuzzleSession.DifficultyNames[i], (i - 1) * 226, 300, 214, 58,
                () => PuzzleDetails(chapter, page, section, choice));
            tab.name = "Difficulty Tab " + i;
            tab.GetComponent<Image>().color = i == difficulty ? Accent : Panel;
        }
        var texture = catalog[puzzle].ImageForDifficulty(difficulty);
        bool completed = (PuzzleProgress.CompletedMask(puzzle, catalog.ProgressId) & (1 << difficulty)) != 0;
        if (completed && texture != null) Preview(content, texture, 0, 65, 330);
        else
        {
            var hidden = Rect("Hidden Puzzle Image", content, 0, 65, 330, 330);
            hidden.gameObject.AddComponent<Image>().color = Panel;
            Label(hidden, "?", 0, 0, 330, 330, 80, Muted);
        }
        string grade = PuzzleProgress.BestGrade(puzzle, difficulty, catalog.ProgressId);
        Label(content, completed ? "Mejor: " + grade : "Sin completar", 0, -170, 660, 60, 26, Ink);
        Button(content, "Jugar", 0, -310, 460, 66, () => Launch(section, difficulty)).interactable = texture != null;
        Button(content, "Volver", 0, -447, 260, 48, () => Subchapters(chapter, page));
    }

    private void Launch(StorySubchapter section, int difficulty)
    {
        if (!StoryProgress.IsUnlocked(catalog, section) || ScreenTransition.IsBusy) return;
        PuzzleSession.StoryMode = false;
        PuzzleSession.Difficulty = difficulty;
        PuzzleSession.ReplayIndex = catalog.Story.IndexOf(section.firstStepId);
        PuzzleSession.ReplayEnd = Mathf.Min(catalog.Story.IndexOf(section.lastStepId), StoryProgress.FurthestIndex(catalog));
        ScreenTransition.LoadScene(PuzzleSession.StoryScene);
    }

    private static void Pages(RectTransform content, int page, int count, Action<int> navigate)
    {
        int pages = Mathf.Max(1, Mathf.CeilToInt(count / (float)PageSize));
        if (pages <= 1) return;
        Button(content, "‹", -180, -365, 72, 58, () => navigate(page - 1)).interactable = page > 0;
        Label(content, $"{page + 1} / {pages}", 0, -365, 180, 58, 23, Ink);
        Button(content, "›", 180, -365, 72, 58, () => navigate(page + 1)).interactable = page + 1 < pages;
    }
}
