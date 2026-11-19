using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button)), DisallowMultipleComponent]
public class ButtonSound : MonoBehaviour
{
    private Button button;

    public static void Bind(Button target)
    {
        if (target.GetComponent<ButtonSound>() == null) target.gameObject.AddComponent<ButtonSound>();
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Play);
    }

    private void Play()
    {
        if (this != null && isActiveAndEnabled && button != null && button.IsInteractable()) GameAudio.Click();
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Play);
    }
}
