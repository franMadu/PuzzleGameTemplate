using UnityEngine;
using UnityEngine.UI;

public class PuzzleMenu : MonoBehaviour
{
    [SerializeField] private PuzzleCatalog catalog;
    [SerializeField] private RectTransform titleScreen;
    [SerializeField] private RectTransform mainScreen;
    [SerializeField] private RectTransform galleryScreen;
    [SerializeField] private RectTransform imageScreen;
    [SerializeField] private RectTransform selectionScreen;
    [SerializeField] private RectTransform settingsScreen;
    [SerializeField] private RectTransform chaptersScreen;
    [SerializeField] private RectTransform subchaptersScreen;
    [SerializeField] private RectTransform difficultyScreen;
    [SerializeField] private RectTransform shopScreen;
    private RectTransform screen;
    private int selected;
    private bool storyMode;

    private int Unlocked => PuzzleProgress.Unlocked(catalog.Count, catalog.ProgressId);
    private int CompletedMask(int index) => PuzzleProgress.CompletedMask(index, catalog.ProgressId);
    private bool HasStory => catalog.Story != null && catalog.Story.Count > 0;

    private void Start()
    {
        foreach (var panel in new[] { titleScreen, mainScreen, galleryScreen, imageScreen,
                     selectionScreen, settingsScreen, chaptersScreen, subchaptersScreen, difficultyScreen, shopScreen })
            panel.gameObject.SetActive(false);
        GameAudio.PlayMusic(null);
        selected = Mathf.Clamp(PuzzleSession.Chapter, 0, Unlocked - 1);
        var page = PuzzleSession.MenuPage;
        PuzzleSession.MenuPage = PuzzleMenuPage.Main;
        if (page == PuzzleMenuPage.Main)
        {
            if (PuzzleSession.TitleDismissed) ShowMain();
            else ShowTitle();
        }
        else if (page == PuzzleMenuPage.Library) ShowLibrary();
        else if (page == PuzzleMenuPage.Shop) ShowShop(0, true);
        else ShowSelection(true);
    }

    private RectTransform Open(RectTransform panel)
    {
        var previous = screen;
        screen = panel;
        screen.gameObject.SetActive(true);
        screen.SetAsLastSibling();
        if (previous != screen) ScreenTransition.Replace(previous, screen, false);
        return (RectTransform)screen.Find("Content");
    }

    private void ShowTitle()
    {
        var content = Open(titleScreen);
        MenuControls.SafeArea(content);
        MenuControls.Get<Text>(content, "Title").text = catalog.GameTitle;
        MenuControls.Bind(content, "Start", () =>
        {
            PuzzleSession.TitleDismissed = true;
            ShowMain();
        });
    }

    public void ShowMain()
    {
        PuzzleSession.TitleDismissed = true;
        var content = Open(mainScreen);
        MenuControls.SafeArea(content);
        var texture = GamePreferences.MenuBackground(catalog);
        MenuControls.Get<Text>(content, "Wallet/Amount").text = StarEconomy.Balance(catalog).ToString();
        MenuControls.Bind(content, "Shop", () => ShowShop());
        var image = MenuControls.Get<RawImage>(mainScreen, "Menu Background/Artwork");
        MenuControls.SetTexture(image, texture, false);
        if (texture != null)
            image.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        MenuControls.Bind(content, "Settings", () =>
        {
            var settings = Open(settingsScreen);
            settings.GetComponent<SettingsMenu>().Build(ShowMain);
        });
        MenuControls.Bind(content, "Gallery", () => ShowGallery());
        MenuControls.Bind(content, "Story", () =>
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
        MenuControls.Bind(content, "Recap", () =>
        {
            PuzzleSession.LibraryChapter = -1;
            ShowLibrary();
        }).interactable = HasStory;
    }

    private void ShowLibrary()
    {
        if (!HasStory) return;
        new PuzzleLibrary(catalog, name => Open(name == "Chapter Selection" ? chaptersScreen :
            name == "Subchapter Selection" ? subchaptersScreen : difficultyScreen), ShowMain).Open();
    }

    private void ShowShop(int page = 0, bool puzzles = false)
    {
        Open(shopScreen).GetComponent<StarShop>().Build(catalog, page, puzzles,
            ShowShop, index => ShowShopPuzzle(index, 0, page), ShowMain);
    }

    private void ShowShopPuzzle(int index, int difficulty, int page)
    {
        if (!StarEconomy.OwnsPuzzle(catalog, index)) return;
        var content = Open(difficultyScreen);
        MenuControls.Get<Text>(content, "Title").text = catalog[index].title;
        for (int i = 0; i < 3; i++)
        {
            int choice = i;
            MenuControls.Bind(content, "Difficulty " + i, () => ShowShopPuzzle(index, choice, page)).GetComponent<Image>().color
                = i == difficulty ? PuzzleUI.Accent : PuzzleUI.Panel;
        }
        bool completed = (CompletedMask(index) & (1 << difficulty)) != 0;
        var image = catalog[index].ImageForDifficulty(difficulty);
        MenuControls.SetTexture(MenuControls.Get<RawImage>(content, "Artwork"), completed ? image : null);
        content.Find("Hidden").gameObject.SetActive(!completed || image == null);
        CanvasText.Set(MenuControls.Get<Text>(content, "Grade"), completed ? 1 : 0);
        var stars = MenuControls.Get<PuzzleStarsGraphic>(content, "Grade/Stars");
        stars.Stars = PuzzleProgress.BestStars(index, difficulty, catalog.ProgressId);
        stars.gameObject.SetActive(completed);
        MenuControls.Bind(content, "Play", () =>
        {
            if (!StarEconomy.CanPlayPuzzle(catalog, index)) return;
            PuzzleSession.EndReplay();
            PuzzleSession.Chapter = index;
            PuzzleSession.Difficulty = difficulty;
            PuzzleSession.StoryMode = false;
            ScreenTransition.LoadScene(PuzzleSession.GameScenes[difficulty]);
        }).interactable = image != null;
        MenuControls.Bind(content, "Back", () => ShowShop(page, true));
    }

    private void ShowGallery(int page = 0, GalleryCategory category = GalleryCategory.Puzzles)
    {
        Open(galleryScreen).GetComponent<PuzzleGallery>().Build(catalog, page,
            next => ShowGallery(next, category), index => ShowGalleryImage(index, page, category), ShowMain,
            category, next => ShowGallery(0, next));
    }

    private void ShowGalleryImage(int index, int page, GalleryCategory category)
    {
        Open(imageScreen);
        if (category == GalleryCategory.Wallpapers)
            imageScreen.GetComponent<GalleryImageViewer>().Build(catalog.WallpaperAt(index).image,
                () => ShowGallery(page, category), () => GamePreferences.SetMenuBackground(catalog, index),
                GamePreferences.MenuBackgroundIndex(catalog) == index);
        else
            imageScreen.GetComponent<GalleryImageViewer>().Build(catalog[index / 3].ImageForDifficulty(index % 3),
                () => ShowGallery(page, category));
    }

    private void ShowSelection(bool story)
    {
        if (!story) { ShowLibrary(); return; }
        storyMode = true;
        if (HasStory) selected = StoryProgress.PreviewPuzzle(catalog);
        var content = Open(selectionScreen);
        int storyDifficulty = catalog[selected].StoryDifficulty;
        bool completed = (CompletedMask(selected) & (1 << storyDifficulty)) != 0;
        MenuControls.SetTexture(MenuControls.Get<RawImage>(content, "Artwork"),
            completed ? catalog[selected].ImageForDifficulty(storyDifficulty) : null);
        content.Find("Hidden").gameObject.SetActive(!completed);
        MenuControls.Get<Text>(content, "Location").text = HasStory
            ? catalog.Story[StoryProgress.Completed(catalog) ? 0 : StoryProgress.CurrentIndex(catalog)].title
            : catalog[selected].title;
        MenuControls.Bind(content, "Continue", PlaySelected);
        MenuControls.Bind(content, "Back", ShowMain);
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

