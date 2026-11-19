using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum DialogueSpeakerSide { Left, Right, Narration }

[Serializable]
public class DialogueOutfitSelection
{
    public DialogueCharacter character;
    [DialogueOutfitId]
    public string outfit;
}

[Serializable]
public class DialogueLine
{
    [Tooltip("Personaje que habla. Con reparto, su posición se elige automáticamente.")]
    public DialogueCharacter character;
    [Tooltip("Vacío conserva el outfit actual. Elegir Predeterminado vuelve al retrato base del personaje.")]
    [DialogueOutfitId(true)] public string outfit;
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
    [Tooltip("Outfit inicial por personaje en esta conversación. Los personajes sin entrada usan su retrato predeterminado.")]
    [SerializeField] private DialogueOutfitSelection[] initialOutfits;
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
    public DialogueOutfitSelection[] InitialOutfits => initialOutfits ?? Array.Empty<DialogueOutfitSelection>();
    public bool LeftFacesLeft => leftCharacterFacing == DialogueFacing.Left;
    public bool RightFacesLeft => rightCharacterFacing == DialogueFacing.Left;
    public DialogueLine[] Lines => lines ?? Array.Empty<DialogueLine>();

    public bool ValidateOutfits(out string error)
    {
        var actors = new System.Collections.Generic.HashSet<DialogueCharacter>();
        foreach (var actor in Characters) if (actor != null) actors.Add(actor);
        foreach (var line in Lines) if (line != null && line.character != null) actors.Add(line.character);
        var configured = new System.Collections.Generic.HashSet<DialogueCharacter>();
        foreach (var selection in InitialOutfits)
        {
            if (selection == null || selection.character == null)
            { error = name + ": asigna Character en cada outfit inicial."; return false; }
            if (!configured.Add(selection.character))
            { error = name + ": outfit inicial repetido para " + selection.character.DisplayName; return false; }
            if (!actors.Contains(selection.character))
            { error = name + ": el personaje del outfit inicial debe participar en Characters o Lines: " + selection.character.DisplayName; return false; }
            if (!selection.character.TryGetAppearance(selection.outfit, out _))
            { error = name + ": outfit inicial desconocido o sin retrato: " + selection.outfit; return false; }
        }
        foreach (var actor in actors)
            if (!actor.ValidateOutfits(out error)) return false;
        for (int i = 0; i < Lines.Length; i++)
        {
            var line = Lines[i];
            if (line == null || string.IsNullOrWhiteSpace(line.text) || string.IsNullOrWhiteSpace(line.outfit)) continue;
            if (line.character == null || !line.character.TryGetAppearance(line.outfit, out _))
            { error = name + ": revisa Character y Outfit en la intervención " + (i + 1); return false; }
        }
        error = null;
        return true;
    }
}
