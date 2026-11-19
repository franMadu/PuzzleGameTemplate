using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueFacing { Right, Left }

[Serializable]
public class DialogueOutfit
{
    [Tooltip("Identificador estable y único dentro del personaje, por ejemplo viaje o gala. No usar default.")]
    public string id;
    public Sprite portrait;
    [Tooltip("Dirección hacia la que mira el dibujo original de este outfit.")]
    public DialogueFacing facing;
}

public readonly struct DialogueAppearance
{
    public Sprite Portrait { get; }
    public bool FacesLeft { get; }
    public DialogueAppearance(Sprite portrait, bool facesLeft)
    { Portrait = portrait; FacesLeft = facesLeft; }
}

// The Inspector displays the available IDs from the adjacent Character field.
public class DialogueOutfitIdAttribute : PropertyAttribute
{
    public bool PreservePrevious { get; }
    public DialogueOutfitIdAttribute(bool preservePrevious = false) => PreservePrevious = preservePrevious;
}

[CreateAssetMenu(menuName = "One Hand Puzzle/Dialogue Character")]
public class DialogueCharacter : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite portrait;
    [Tooltip("Dirección hacia la que mira el dibujo original, antes de reflejarlo.")]
    [SerializeField] private DialogueFacing facing;
    [Tooltip("Variantes de vestuario del mismo personaje. Portrait y Facing siguen siendo su apariencia predeterminada.")]
    [SerializeField] private DialogueOutfit[] outfits;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Portrait => portrait;
    public bool FacesLeft => facing == DialogueFacing.Left;
    public const string DefaultOutfitId = "default";
    public IReadOnlyList<DialogueOutfit> Outfits => outfits ?? Array.Empty<DialogueOutfit>();

    public bool TryGetAppearance(string outfitId, out DialogueAppearance appearance)
    {
        appearance = new DialogueAppearance(portrait, FacesLeft);
        if (string.IsNullOrWhiteSpace(outfitId) || string.Equals(outfitId, DefaultOutfitId, StringComparison.OrdinalIgnoreCase)) return true;
        foreach (var outfit in Outfits)
            if (outfit != null && string.Equals(outfit.id, outfitId, StringComparison.OrdinalIgnoreCase))
            {
                if (outfit.portrait == null) return false;
                appearance = new DialogueAppearance(outfit.portrait, outfit.facing == DialogueFacing.Left);
                return true;
            }
        return false;
    }

    public bool ValidateOutfits(out string error)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { DefaultOutfitId };
        foreach (var outfit in Outfits)
        {
            if (outfit == null || string.IsNullOrWhiteSpace(outfit.id) || outfit.id != outfit.id.Trim())
            { error = DisplayName + ": cada outfit necesita un ID sin espacios al principio ni al final."; return false; }
            if (!ids.Add(outfit.id))
            { error = DisplayName + ": ID de outfit duplicado o reservado: " + outfit.id; return false; }
            if (outfit.portrait == null)
            { error = DisplayName + ": falta el retrato del outfit " + outfit.id; return false; }
        }
        error = null;
        return true;
    }
}
