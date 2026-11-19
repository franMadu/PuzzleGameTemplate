using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueFacing { Right, Left }

[Serializable]
public class DialogueExpression
{
    [Tooltip("Identificador estable, por ejemplo feliz, triste o enfadado. No usar default.")]
    public string id;
    [Tooltip("Sprite completo con esta expresión y el vestuario correspondiente. Usa la orientación del retrato de ese vestuario.")]
    public Sprite portrait;
}

[Serializable]
public class DialogueOutfit
{
    [Tooltip("Identificador estable y único dentro del personaje, por ejemplo viaje o gala. No usar default.")]
    public string id;
    public Sprite portrait;
    [Tooltip("Dirección hacia la que mira el dibujo original de este outfit.")]
    public DialogueFacing facing;
    public DialogueExpression[] expressions;
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

public class DialogueExpressionIdAttribute : PropertyAttribute
{
    public bool PreservePrevious { get; }
    public DialogueExpressionIdAttribute(bool preservePrevious = false) => PreservePrevious = preservePrevious;
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
    [Tooltip("Expresiones con el vestuario predeterminado. Cada outfit tiene su propia lista de expresiones.")]
    [SerializeField] private DialogueExpression[] expressions;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Portrait => portrait;
    public bool FacesLeft => facing == DialogueFacing.Left;
    public const string DefaultOutfitId = "default";
    public const string DefaultExpressionId = "default";
    public IReadOnlyList<DialogueOutfit> Outfits => outfits ?? Array.Empty<DialogueOutfit>();
    public IReadOnlyList<DialogueExpression> Expressions => expressions ?? Array.Empty<DialogueExpression>();

    public IEnumerable<string> ExpressionIds
    {
        get
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var expression in Expressions)
                if (expression != null && !string.IsNullOrWhiteSpace(expression.id) && ids.Add(expression.id)) yield return expression.id;
            foreach (var outfit in Outfits)
                if (outfit?.expressions != null)
                    foreach (var expression in outfit.expressions)
                        if (expression != null && !string.IsNullOrWhiteSpace(expression.id) && ids.Add(expression.id)) yield return expression.id;
        }
    }

    public bool HasExpression(string expressionId)
    {
        if (string.IsNullOrWhiteSpace(expressionId) || string.Equals(expressionId, DefaultExpressionId, StringComparison.OrdinalIgnoreCase)) return true;
        foreach (string id in ExpressionIds)
            if (string.Equals(id, expressionId, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public bool TryGetAppearance(string outfitId, string expressionId, out DialogueAppearance appearance)
    {
        bool validOutfit = TryGetAppearance(outfitId, out appearance);
        if (!validOutfit) return false;
        if (string.IsNullOrWhiteSpace(expressionId) || string.Equals(expressionId, DefaultExpressionId, StringComparison.OrdinalIgnoreCase)) return true;
        IReadOnlyList<DialogueExpression> available = Expressions;
        if (!string.IsNullOrWhiteSpace(outfitId) && !string.Equals(outfitId, DefaultOutfitId, StringComparison.OrdinalIgnoreCase))
            foreach (var outfit in Outfits)
                if (outfit != null && string.Equals(outfit.id, outfitId, StringComparison.OrdinalIgnoreCase))
                { available = outfit.expressions ?? Array.Empty<DialogueExpression>(); break; }
        foreach (var expression in available)
            if (expression != null && string.Equals(expression.id, expressionId, StringComparison.OrdinalIgnoreCase))
            {
                if (expression.portrait == null) return false;
                appearance = new DialogueAppearance(expression.portrait, appearance.FacesLeft);
                return true;
            }
        // A missing expression uses the current outfit's neutral portrait, never a different outfit.
        return false;
    }

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

    public bool ValidateExpressions(out string error)
    {
        if (!ValidateExpressionList(Expressions, "predeterminado", out error)) return false;
        foreach (var outfit in Outfits)
            if (outfit != null && !ValidateExpressionList(outfit.expressions ?? Array.Empty<DialogueExpression>(), outfit.id, out error)) return false;
        return true;
    }

    private bool ValidateExpressionList(IReadOnlyList<DialogueExpression> list, string outfit, out string error)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { DefaultExpressionId };
        foreach (var expression in list)
        {
            if (expression == null || string.IsNullOrWhiteSpace(expression.id) || expression.id != expression.id.Trim())
            { error = DisplayName + ": cada expresión necesita un ID sin espacios al principio ni al final (outfit " + outfit + ")."; return false; }
            if (!ids.Add(expression.id))
            { error = DisplayName + ": ID de expresión duplicado o reservado: " + expression.id + " (outfit " + outfit + ")."; return false; }
            if (expression.portrait == null)
            { error = DisplayName + ": falta el sprite de la expresión " + expression.id + " (outfit " + outfit + ")."; return false; }
        }
        error = null;
        return true;
    }
}
