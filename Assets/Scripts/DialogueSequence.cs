using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum DialogueSpeakerSide { Left, Right, Narration }

[Serializable]
public class DialogueLine
{
    [Tooltip("Personaje que habla. Con reparto, su posición se elige automáticamente.")]
    public DialogueCharacter character;
    [Tooltip("Nombre manual cuando no se asigna Character.")]
    public string speaker;
    [TextArea(3, 8)] public string text;
    [Tooltip("Posición manual para diálogos sin reparto. Con Character, se calcula automáticamente.")]
    public DialogueSpeakerSide speakerSide;
    [Tooltip("Opcional: sustituye el personaje izquierdo. Vacío conserva el anterior.")]
    [FormerlySerializedAs("portrait")] public Sprite leftCharacter;
    [Tooltip("Opcional: sustituye el personaje derecho. Vacío conserva el anterior.")]
    public Sprite rightCharacter;
    public bool hideLeftCharacter;
    public bool hideRightCharacter;
}

[CreateAssetMenu(menuName = "One Hand Puzzle/Dialogue")]
public class DialogueSequence : ScriptableObject
{
    [Tooltip("Fondo propio de esta conversación. Si está vacío, se usa la imagen de la escena o del puzle.")]
    [SerializeField] private Texture2D background;
    [Tooltip("Música de fondo en bucle. Vacío deja esta conversación sin música.")]
    [SerializeField] private AudioClip music;
    [Tooltip("Reparto de la conversación. Los dos primeros aparecen al comenzar; los demás entran cuando hablan.")]
    [SerializeField] private DialogueCharacter[] characters;
    [Header("Posiciones manuales para conversaciones sin reparto")]
    [SerializeField] private Sprite leftCharacter;
    [SerializeField] private Sprite rightCharacter;
    [SerializeField] private DialogueFacing leftCharacterFacing;
    [SerializeField] private DialogueFacing rightCharacterFacing;
    [Header("Intervenciones")]
    [SerializeField] private DialogueLine[] lines;
    public Sprite LeftCharacter => leftCharacter;
    public Sprite RightCharacter => rightCharacter;
    public Texture2D Background => background;
    public AudioClip Music => music;
    public DialogueCharacter[] Characters => characters ?? Array.Empty<DialogueCharacter>();
    public bool LeftFacesLeft => leftCharacterFacing == DialogueFacing.Left;
    public bool RightFacesLeft => rightCharacterFacing == DialogueFacing.Left;
    public DialogueLine[] Lines => lines ?? Array.Empty<DialogueLine>();
}
