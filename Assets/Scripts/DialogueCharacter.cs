using UnityEngine;

public enum DialogueFacing { Right, Left }

[CreateAssetMenu(menuName = "One Hand Puzzle/Dialogue Character")]
public class DialogueCharacter : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite portrait;
    [Tooltip("Dirección hacia la que mira el dibujo original, antes de reflejarlo.")]
    [SerializeField] private DialogueFacing facing;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Portrait => portrait;
    public bool FacesLeft => facing == DialogueFacing.Left;
}
