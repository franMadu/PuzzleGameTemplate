using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static PuzzleUI;

public class StorySceneController : MonoBehaviour
{
    [SerializeField] private PuzzleCatalog catalog;
    [SerializeField] private DialogueController dialogue;
    [SerializeField] private RectTransform endingScreen;
    [SerializeField] private RectTransform errorScreen;

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
        endingScreen.gameObject.SetActive(true);
        endingScreen.SetAsLastSibling();
        var content = endingScreen.Find("Content");
        MenuControls.Get<Text>(content, "Ending").text = catalog.EndingText;
        MenuControls.Bind(content, "Back", ReturnToMenu);
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
        errorScreen.gameObject.SetActive(true);
        errorScreen.SetAsLastSibling();
        var content = errorScreen.Find("Content");
        MenuControls.Get<Text>(content, "Error").text = error;
        MenuControls.Bind(content, "Back", ReturnToMenu);
    }

    private void ReturnToMenu()
    {
        PuzzleSession.StoryMode = false;
        PuzzleSession.MenuPage = PuzzleMenuPage.Main;
        ScreenTransition.LoadScene(PuzzleSession.MenuScene);
    }
}
