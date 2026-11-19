using UnityEngine;
using UnityEngine.SceneManagement;
using static PuzzleUI;

public class DialogueDemoController : MonoBehaviour
{
    [SerializeField] private DialogueController dialogue;
    [SerializeField] private DialogueSequence conversation;
    [SerializeField] private Texture2D background;
    private RectTransform finishScreen;

    private void Start()
    {
        finishScreen = Screen("Dialogue Demo Finished", (RectTransform)transform);
        var content = Rect("Content", finishScreen, 0, 0, 720, 920);
        Button(content, "Repetir diálogo", 0, -80, 420, 70, Replay);
        Button(content, "Volver al menú", 0, -175, 420, 70, () => ScreenTransition.LoadScene(PuzzleSession.MenuScene));
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
