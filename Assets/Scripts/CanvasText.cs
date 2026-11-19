using UnityEngine;
using UnityEngine.UI;

// State labels and value formats are authored beside the Text component in the Canvas.
[RequireComponent(typeof(Text)), DisallowMultipleComponent]
public class CanvasText : MonoBehaviour
{
    [SerializeField, TextArea, InspectorName("Textos por estado")]
    [Tooltip("Cada entrada corresponde a un estado. {0}, {1}, etc. se sustituyen por los valores del juego.")]
    private string[] formats;

    public static void Set(Text target, int state = 0, params object[] values)
    {
        var source = target.GetComponent<CanvasText>();
        string format = source.formats[state];
        target.text = values.Length == 0 ? format : string.Format(format, values);
    }
}
