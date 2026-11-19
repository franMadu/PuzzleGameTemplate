using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PuzzleUI;

public class StorySceneController : MonoBehaviour
{
    [SerializeField] private PuzzleCatalog catalog;
    [SerializeField] private DialogueController dialogue;

    private IEnumerator Start()
    {
        GameAudio.PlayMusic(null);
        if (catalog == null || catalog.Story == null)
        {
            ShowError("Asigna un catálogo y una secuencia de historia.");
            yield break;
        }
        if (!catalog.Story.Validate(catalog, out string error))
        {
            ShowError(error);
            yield break;
        }
        if (PuzzleSession.IsReplay)
        {
            yield return Replay();
            yield break;
        }
        // Each puzzle uses the difficulty authored in the catalog, also when resuming.
        PuzzleSession.StoryMode = true;
        while (!StoryProgress.Completed(catalog))
        {
            var step = catalog.Story[StoryProgress.CurrentIndex(catalog)];
            if (step.kind == StoryStepKind.Puzzle)
            {
                PuzzleSession.Chapter = step.puzzleIndex;
                PuzzleSession.Difficulty = catalog[step.puzzleIndex].StoryDifficulty;
                ScreenTransition.LoadScene(PuzzleSession.GameScenes[PuzzleSession.Difficulty]);
                yield break;
            }
            bool finished = false;
            dialogue.PlaySmooth(step.conversation, null, () => finished = true);
            // A frame between conversations also prevents a single input from skipping two steps.
            while (!finished) yield return null;
            StoryProgress.Advance(catalog, step.id);
            yield return null;
        }
        var screen = Screen("Story Complete", (RectTransform)transform);
        var content = Rect("Content", screen, 0, 0, 720, 920);
        Label(content, "FIN DE LA HISTORIA", 0, 130, 680, 90, 38, Ink);
        Label(content, catalog.EndingText, 0, 15, 680, 120, 24, Muted);
        Button(content, "Menú principal", 0, -120, 420, 70, ReturnToMenu);
    }

    private IEnumerator Replay()
    {
        while (PuzzleSession.ReplayIndex <= PuzzleSession.ReplayEnd && PuzzleSession.ReplayIndex < catalog.Story.Count)
        {
            var step = catalog.Story[PuzzleSession.ReplayIndex];
            if (step.kind == StoryStepKind.Puzzle)
            {
                PuzzleSession.Chapter = step.puzzleIndex;
                ScreenTransition.LoadScene(PuzzleSession.GameScenes[PuzzleSession.Difficulty]);
                yield break;
            }
            bool finished = false;
            dialogue.PlaySmooth(step.conversation, null, () => finished = true);
            while (!finished) yield return null;
            PuzzleSession.ReplayIndex++;
            yield return null;
        }
        PuzzleSession.EndReplay();
        PuzzleSession.MenuPage = PuzzleMenuPage.Library;
        ScreenTransition.LoadScene(PuzzleSession.MenuScene);
    }

    private void ShowError(string error)
    {
        Debug.LogError(error, this);
        var screen = Screen("Story Configuration Error", (RectTransform)transform);
        Label(screen, error, 0, 50, 680, 160, 24, Ink);
        Button(screen, "Menú principal", 0, -100, 420, 70, ReturnToMenu);
    }

    private void ReturnToMenu()
    {
        PuzzleSession.StoryMode = false;
        PuzzleSession.MenuPage = PuzzleMenuPage.Main;
        ScreenTransition.LoadScene(PuzzleSession.MenuScene);
    }
}
