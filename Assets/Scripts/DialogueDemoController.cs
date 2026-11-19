using UnityEngine;
using UnityEngine.SceneManagement;
using static PuzzleUI;

public class DialogueDemoController : MonoBehaviour
{
    [SerializeField] private DialogueController dialogue;
    [SerializeField] private DialogueSequence conversation;
    [SerializeField] private Texture2D background;
    [SerializeField] private RectTransform finishScreen;

    private void Start()
    {
        var content = finishScreen.Find("Content");
        MenuControls.Bind(content, "Replay", Replay);
        MenuControls.Bind(content, "Back", () => ScreenTransition.LoadScene(PuzzleSession.MenuScene));
        Replay();
    }

    public void Replay()
    {
        ScreenTransition.Change(() =>
        {
            if (this == null) return;
            if (finishScreen != null) finishScreen.gameObject.SetActive(false);
            dialogue.Play(conversation, background, () => finishScreen.gameObject.SetActive(true), true);
        });
    }
}
