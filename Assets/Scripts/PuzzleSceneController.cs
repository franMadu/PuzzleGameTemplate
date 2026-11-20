using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static PuzzleUI;

public class PuzzleSceneController : MonoBehaviour
{
    [SerializeField] private PuzzleCatalog catalog;
    [SerializeField] private Puzzle puzzle;
    [SerializeField] private GameObject gameLayout;
    [SerializeField] private Text gameTitle;
    [SerializeField] private DialogueController dialogue;
    [SerializeField, Range(3, 5)] private int boardSize = 3;
    private int chapter;
    private bool storyMode;
    private string storyStepId;
    private int replayStep = -1;
    private int earnedStars;
    private bool SequenceStory => storyMode && catalog.Story != null && storyStepId != null;
    [SerializeField] private RectTransform victoryScreen;
    private Coroutine completionRoutine;
    [SerializeField] private Button completionContinue;
    private int Difficulty => boardSize - 3;

    private void Awake()
    {
        // Opening any difficulty scene directly in the editor uses the first image.
        storyMode = PuzzleSession.StoryMode;
        if (PuzzleSession.IsReplay)
        {
            replayStep = PuzzleSession.ReplayIndex;
            chapter = catalog.Story[replayStep].puzzleIndex;
        }
        else if (storyMode && catalog.Story != null)
        {
            int index = StoryProgress.CurrentIndex(catalog);
            if (index < catalog.Story.Count && catalog.Story[index].kind == StoryStepKind.Puzzle)
            {
                storyStepId = catalog.Story[index].id;
                chapter = catalog.Story[index].puzzleIndex;
            }
            else
            {
                ScreenTransition.LoadScene(PuzzleSession.StoryScene);
                enabled = false;
                return;
            }
        }
        else
        {
            chapter = Mathf.Clamp(PuzzleSession.Chapter, 0, catalog.Count - 1);
            // Directly opening a difficulty scene still supports the first free puzzle.
            if (!StarEconomy.CanPlayPuzzle(catalog, chapter) && !(chapter == 0 && catalog[chapter].starCost == 0))
            { ScreenTransition.LoadScene(PuzzleSession.MenuScene); enabled = false; return; }
        }
        if (catalog[chapter].starCost > 0 && !StarEconomy.OwnsPuzzle(catalog, chapter))
        { ScreenTransition.LoadScene(PuzzleSession.MenuScene); enabled = false; return; }
        if (storyMode)
        {
            PuzzleSession.Difficulty = catalog[chapter].StoryDifficulty;
            if (Difficulty != PuzzleSession.Difficulty)
            {
                ScreenTransition.LoadScene(PuzzleSession.GameScenes[PuzzleSession.Difficulty]);
                enabled = false;
                return;
            }
        }
        puzzle.Completed += OnCompleted;
        dialogue.ActiveChanged += OnDialogueActiveChanged;
        CanvasText.Set(gameTitle, Difficulty, catalog[chapter].title);
        MenuControls.Bind(gameLayout.transform, "Back to Menu",
            () => ReturnToMenu(PuzzleMenuPage.Main));
        PlayAgain();
    }

    private void Start()
    {
        if (storyMode && !SequenceStory && replayStep < 0)
            dialogue.PlaySmooth(catalog[chapter].introduction, catalog[chapter].image,
                () => GameAudio.PlayMusic(catalog[chapter].music, this));
    }

    private void OnDialogueActiveChanged(bool active)
    {
        if (puzzle != null) puzzle.SetInputBlocked(active);
    }

    private void OnDestroy()
    {
        GameAudio.StopMusic(this);
        if (puzzle != null) puzzle.Completed -= OnCompleted;
        if (dialogue != null) dialogue.ActiveChanged -= OnDialogueActiveChanged;
    }

    private void PlayAgain()
    {
        if (completionRoutine != null) { StopCoroutine(completionRoutine); completionRoutine = null; }
        completionContinue.gameObject.SetActive(false);
        var shuffle = gameLayout.transform.Find("Shuffle Button");
        if (shuffle != null) shuffle.gameObject.SetActive(true);
        dialogue.Cancel();
        if (victoryScreen != null)
        {
            victoryScreen.gameObject.SetActive(false);
        }
        gameLayout.SetActive(true);
        puzzle.Configure(catalog[chapter].ImageForDifficulty(Difficulty), boardSize);
        GameAudio.PlayMusic(catalog[chapter].music, this);
    }

    private void ReturnToMenu(PuzzleMenuPage page)
    {
        if (completionRoutine != null) { StopCoroutine(completionRoutine); completionRoutine = null; }
        puzzle.Suspend();
        PuzzleSession.Chapter = chapter;
        PuzzleSession.StoryMode = storyMode;
        PuzzleSession.MenuPage = page;
        PuzzleSession.EndReplay();
        ScreenTransition.LoadScene(PuzzleSession.MenuScene);
    }

    private void OnCompleted()
    {
        earnedStars = PuzzleProgress.Complete(chapter, Difficulty, catalog.Count, puzzle.Moves, puzzle.MinimumMoves, catalog.ProgressId,
            catalog[chapter].starCost == 0);
        if (SequenceStory)
        {
            StoryProgress.Advance(catalog, storyStepId);
        }
        if (replayStep >= 0) PuzzleSession.ReplayIndex = replayStep + 1;
        completionRoutine = StartCoroutine(FinishAfterReveal());
    }

    private IEnumerator FinishAfterReveal()
    {
        while (puzzle.IsRevealingCompletion) yield return null;
        completionRoutine = null;
        var shuffle = gameLayout.transform.Find("Shuffle Button");
        if (shuffle != null) shuffle.gameObject.SetActive(false);
        MenuControls.Bind(gameLayout.transform, "Continue After Reveal", () =>
        {
            if (ScreenTransition.IsBusy || puzzle.IsRevealingCompletion) return;
            completionContinue.interactable = false;
            ScreenTransition.Change(() =>
            {
                if (this == null) return;
                gameLayout.SetActive(false);
                if (storyMode && !SequenceStory)
                    dialogue.Play(catalog[chapter].conclusion, catalog[chapter].image, ShowVictory, true);
                else ShowVictory();
            });
        });
        completionContinue.interactable = true;
        completionContinue.gameObject.SetActive(true);
        ScreenTransition.Replace(null, (RectTransform)completionContinue.transform, false);
    }

    private void ShowVictory()
    {
        victoryScreen.gameObject.SetActive(true);
        victoryScreen.SetAsLastSibling();
        var content = victoryScreen.Find("Content");
        CanvasText.Set(MenuControls.Get<Text>(content, "Star Reward"), 0, earnedStars, StarEconomy.Balance(catalog));
        MenuControls.Get<Text>(content, "Title").text = catalog[chapter].title;
        MenuControls.SetTexture(MenuControls.Get<RawImage>(content, "Artwork"), catalog[chapter].ImageForDifficulty(Difficulty));
        MenuControls.Get<PuzzleStarsGraphic>(content, "Grade").Stars = puzzle.Stars;
        CanvasText.Set(MenuControls.Get<Text>(content, "Moves"), 0, puzzle.Moves, puzzle.MinimumMoves);
        MenuControls.Get<PuzzleStarsGraphic>(content, "Best/Stars").Stars = PuzzleProgress.BestStars(chapter, Difficulty, catalog.ProgressId);
        bool next = chapter + 1 < catalog.Count;
        var nextLabel = MenuControls.Get<Text>(content, "Next/Label");
        if (replayStep >= 0 || SequenceStory)
        {
            CanvasText.Set(nextLabel, replayStep >= 0 ? 0 : 1);
            MenuControls.Bind(content, "Next", () => ScreenTransition.LoadScene(PuzzleSession.StoryScene));
        }
        else if (storyMode && next)
        {
            CanvasText.Set(nextLabel, 2);
            MenuControls.Bind(content, "Next", () =>
            {
                chapter++;
                ReturnToMenu(PuzzleMenuPage.Story);
            });
        }
        else
        {
            CanvasText.Set(nextLabel, 3);
            MenuControls.Bind(content, "Next", () => ReturnToMenu(catalog[chapter].starCost > 0 ? PuzzleMenuPage.Shop : PuzzleMenuPage.Library));
        }
        MenuControls.Bind(content, "Play Again", () => ScreenTransition.Change(PlayAgain));
        MenuControls.Bind(content, "Back", () => ReturnToMenu(PuzzleMenuPage.Main));
    }
}
