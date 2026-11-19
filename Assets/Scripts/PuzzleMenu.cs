using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static PuzzleUI;

public class PuzzleMenu : MonoBehaviour
{
    [SerializeField] private PuzzleCatalog catalog;
    private RectTransform screen;
    private int selected;
    private bool storyMode;

    private int Unlocked => PuzzleProgress.Unlocked(catalog.Count, catalog.ProgressId);
    private int CompletedMask(int index) => PuzzleProgress.CompletedMask(index, catalog.ProgressId);
    private bool HasStory => catalog.Story != null && catalog.Story.Count > 0;

    private void Start()
    {
        GameAudio.PlayMusic(null);
        selected = Mathf.Clamp(PuzzleSession.Chapter, 0, Unlocked - 1);
        var page = PuzzleSession.MenuPage;
        PuzzleSession.MenuPage = PuzzleMenuPage.Main;
        if (page == PuzzleMenuPage.Main) ShowMain();
        else if (page == PuzzleMenuPage.Library) ShowLibrary();
        else ShowSelection(true);
    }

    private RectTransform NewScreen(string name)
    {
        var previous = screen;
        screen = Screen(name, (RectTransform)transform);
        ScreenTransition.Replace(previous, screen);
        return Rect("Content", screen, 0, 0, 720, 920);
    }

    public void ShowMain()
    {
        var content = NewScreen("Main Menu");
        var gearRect = Rect("Settings", screen, -60, -60, 72, 72);
        gearRect.anchorMin = gearRect.anchorMax = Vector2.one;
        var gearGraphic = gearRect.gameObject.AddComponent<SettingsGearGraphic>();
        gearGraphic.color = Ink;
        var gear = gearRect.gameObject.AddComponent<Button>();
        ButtonSound.Bind(gear);
        gear.targetGraphic = gearGraphic;
        gear.onClick.AddListener(() => NewScreen("Settings Menu").gameObject.AddComponent<SettingsMenu>().Build(ShowMain));
        Label(content, catalog.GameTitle, 0, 260, 660, 150, 54, Ink);
        int completed = 0;
        for (int i = 0; i < catalog.Count; i++) if (CompletedMask(i) != 0) completed++;
        bool finished = HasStory ? StoryProgress.Completed(catalog) : completed == catalog.Count;
        bool started = HasStory ? StoryProgress.CurrentIndex(catalog) > 0 : completed > 0;
        Button(content, finished ? "Repetir historia" : started ? "Continuar historia" : "Modo historia",
            0, 30, 460, 78, () =>
            {
                selected = 0;
                if (HasStory) selected = StoryProgress.PreviewPuzzle(catalog);
                else
                {
                    while (selected < catalog.Count && CompletedMask(selected) != 0) selected++;
                    if (selected == catalog.Count) selected = 0;
                }
                ShowSelection(true);
            });
        Button(content, "Capítulos", 0, -72, 460, 78, () =>
        {
            PuzzleSession.LibraryChapter = -1;
            ShowLibrary();
        });
        Button(content, "Galería", 0, -174, 460, 78, () => ShowGallery());
        Button(content, "Salir", 0, -276, 460, 78, ExitGame);
    }

    private void ShowLibrary()
    {
        if (!HasStory) return;
        new PuzzleLibrary(catalog, NewScreen, ShowMain).Open();
    }

    private void ShowGallery(int page = 0)
    {
        NewScreen("Gallery").gameObject.AddComponent<PuzzleGallery>().Build(catalog, page,
            ShowGallery, index => ShowGalleryImage(index, page), ShowMain);
    }

    private void ShowGalleryImage(int index, int page)
    {
        var content = NewScreen("Gallery Image");
        Destroy(content.gameObject);
        screen.gameObject.AddComponent<GalleryImageViewer>().Build(catalog[index / 3].ImageForDifficulty(index % 3), () => ShowGallery(page));
    }

    private static void ExitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowSelection(bool story)
    {
        if (!story) { ShowLibrary(); return; }
        storyMode = true;
        if (HasStory) selected = StoryProgress.PreviewPuzzle(catalog);
        var content = NewScreen("Story Selection");
        Label(content, "MODO HISTORIA", 0, 402, 680, 60, 32, Ink);
        int storyDifficulty = catalog[selected].StoryDifficulty;
        if ((CompletedMask(selected) & (1 << storyDifficulty)) != 0)
            Preview(content, catalog[selected].ImageForDifficulty(storyDifficulty), 0, 153, 270);
        else Label(content, "?", 0, 153, 270, 270, 80, Muted);
        string location = HasStory ? catalog.Story[StoryProgress.Completed(catalog) ? 0 : StoryProgress.CurrentIndex(catalog)].title : catalog[selected].title;
        Label(content, location, 0, -17, 670, 52, 28, Ink);
        Button(content, "Continuar viaje", 0, -373, 460, 66, PlaySelected);
        Button(content, "Volver", 0, -447, 260, 48, ShowMain);
    }
    private void PlaySelected()
    {
        if (storyMode && HasStory)
        {
            if (!catalog.Story.Validate(catalog, out string error))
            {
                Debug.LogError(error, catalog);
                return;
            }
            StoryProgress.Begin(catalog);
            ScreenTransition.LoadScene(PuzzleSession.StoryScene);
            return;
        }
        if (selected >= Unlocked) return;
        PuzzleSession.Chapter = selected;
        PuzzleSession.StoryMode = storyMode;
        PuzzleSession.Difficulty = catalog[selected].StoryDifficulty;
        ScreenTransition.LoadScene(PuzzleSession.GameScenes[PuzzleSession.Difficulty]);
    }

}
