using UnityEngine;
using UnityEngine.UI;

// The dialogue's appearance is authored on the existing Body object in the Canvas.
[RequireComponent(typeof(Text)), DisallowMultipleComponent]
public class DialogueTextStyle : MonoBehaviour
{
    [Header("Diálogo hablado")]
    [SerializeField] private Color spokenColor = new Color(0.94f, 0.9f, 0.8f, 1);
    [SerializeField] private FontStyle spokenFontStyle = FontStyle.Normal;
    [Header("Pensamiento")]
    [SerializeField] private Color thoughtColor = new Color(0.7f, 0.82f, 1, 1);
    [SerializeField] private FontStyle thoughtFontStyle = FontStyle.Italic;

    public FontStyle GetFontStyle(bool isThought) => isThought ? thoughtFontStyle : spokenFontStyle;

    public void Apply(bool isThought)
    {
        var label = GetComponent<Text>();
        label.color = isThought ? thoughtColor : spokenColor;
        label.fontStyle = GetFontStyle(isThought);
    }
}
