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
    private bool SequenceStory => storyMode && catalog.Story != null && storyStepId != null;
    private RectTransform victoryScreen;
    private Coroutine completionRoutine;
    private Button completionContinue;
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
        else chapter = Mathf.Clamp(PuzzleSession.Chapter, 0, PuzzleProgress.Unlocked(catalog.Count, catalog.ProgressId) - 1);
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
        gameTitle.text = $"{catalog[chapter].title} · {PuzzleSession.DifficultyNames[Difficulty]}";
        gameTitle.fontSize = 28;
        Button((RectTransform)gameLayout.transform, "Volver al menú", -184, -423, 220, 54,
            () => ReturnToMenu(PuzzleMenuPage.Main));
        var shuffle = gameLayout.transform.Find("Shuffle Button") as RectTransform;
        if (shuffle != null) shuffle.anchoredPosition = new Vector2(140, -423);
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
        if (completionContinue != null) { Destroy(completionContinue.gameObject); completionContinue = null; }
        var shuffle = gameLayout.transform.Find("Shuffle Button");
        if (shuffle != null) shuffle.gameObject.SetActive(true);
        dialogue.Cancel();
        if (victoryScreen != null)
        {
            victoryScreen.gameObject.SetActive(false);
            Destroy(victoryScreen.gameObject);
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
        PuzzleProgress.Complete(chapter, Difficulty, catalog.Count, puzzle.Moves, puzzle.MinimumMoves, catalog.ProgressId);
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
        completionContinue = Button((RectTransform)gameLayout.transform, "Continuar", 140, -423, 220, 54, () =>
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
        completionContinue.name = "Continue After Reveal";
        ScreenTransition.Replace(null, (RectTransform)completionContinue.transform);
    }

    private void ShowVictory()
    {
        victoryScreen = Screen("Puzzle Complete", (RectTransform)transform);
        var content = Rect("Content", victoryScreen, 0, 0, 720, 920);
        Label(content, catalog[chapter].title, 0, 377, 680, 70, 34, Ink);
        Preview(content, catalog[chapter].ImageForDifficulty(Difficulty), 0, 183, 250);
        Label(content, $"NOTA {puzzle.Grade}", 0, -26, 680, 78, 48, Ink);
        Label(content, $"Movimientos: {puzzle.Moves} · Mínimo: {puzzle.MinimumMoves}", 0, -104, 680, 44, 23, Ink);
        Label(content, $"Mejor: {PuzzleProgress.BestGrade(chapter, Difficulty, catalog.ProgressId)}", 0, -156, 680, 40, 21, Muted);
        bool next = chapter + 1 < catalog.Count;
        if (replayStep >= 0)
            Button(content, "Continuar", 0, -291, 460, 66, () => ScreenTransition.LoadScene(PuzzleSession.StoryScene));
        else if (SequenceStory)
            Button(content, "Continuar historia", 0, -291, 460, 66, () => ScreenTransition.LoadScene(PuzzleSession.StoryScene));
        else if (storyMode && next)
            Button(content, "Siguiente capítulo", 0, -291, 460, 66, () =>
            {
                chapter++;
                ReturnToMenu(PuzzleMenuPage.Story);
            });
        else
            Button(content, "Elegir puzle y dificultad", 0, -291, 460, 66, () => ReturnToMenu(PuzzleMenuPage.Library));
        Button(content, "Volver a jugar", 0, -371, 460, 60, () => ScreenTransition.Change(PlayAgain));
        Button(content, "Menú principal", 0, -447, 300, 48, () => ReturnToMenu(PuzzleMenuPage.Main));
    }
}
