using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueOutfitIdAttribute))]
public class DialogueOutfitIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var character = FindCharacter(property);
        if (character == null || property.serializedObject.isEditingMultipleObjects)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        var setting = (DialogueOutfitIdAttribute)attribute;
        var ids = new List<string> { "" };
        var names = new List<string> { setting.PreservePrevious ? "Conservar outfit actual" : "Predeterminado" };
        if (setting.PreservePrevious)
        {
            ids.Add(DialogueCharacter.DefaultOutfitId);
            names.Add("Predeterminado (restablecer)");
        }
        foreach (var outfit in character.Outfits)
        {
            if (outfit == null || string.IsNullOrWhiteSpace(outfit.id)
                || string.Equals(outfit.id, DialogueCharacter.DefaultOutfitId, StringComparison.OrdinalIgnoreCase)) continue;
            ids.Add(outfit.id);
            names.Add(outfit.id);
        }
        DrawChoices(position, property, label, ids, names, setting.PreservePrevious);
    }

    internal static DialogueCharacter FindCharacter(SerializedProperty property)
    {
        int separator = property.propertyPath.LastIndexOf('.');
        string parent = separator >= 0 ? property.propertyPath.Substring(0, separator + 1) : "";
        return property.serializedObject.FindProperty(parent + "character")?.objectReferenceValue as DialogueCharacter;
    }

    internal static void DrawChoices(Rect position, SerializedProperty property, GUIContent label,
        List<string> ids, List<string> names, bool preservePrevious)
    {
        string current = property.stringValue ?? "";
        int selected = ids.FindIndex(id => string.Equals(id, current, StringComparison.OrdinalIgnoreCase));
        if (selected < 0 && !preservePrevious && string.Equals(current, DialogueCharacter.DefaultOutfitId, StringComparison.OrdinalIgnoreCase)) selected = 0;
        if (selected < 0)
        {
            selected = ids.Count;
            ids.Add(current);
            names.Add("No existe: " + current);
        }
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        int choice = EditorGUI.Popup(position, label.text, selected, names.ToArray());
        if (EditorGUI.EndChangeCheck()) property.stringValue = ids[choice];
        EditorGUI.EndProperty();
    }
}

[CustomPropertyDrawer(typeof(DialogueExpressionIdAttribute))]
public class DialogueExpressionIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var character = DialogueOutfitIdDrawer.FindCharacter(property);
        if (character == null || property.serializedObject.isEditingMultipleObjects)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }
        bool preserve = ((DialogueExpressionIdAttribute)attribute).PreservePrevious;
        var ids = new List<string> { "" };
        var names = new List<string> { preserve ? "Conservar expresión actual" : "Predeterminada (neutra)" };
        if (preserve)
        {
            ids.Add(DialogueCharacter.DefaultExpressionId);
            names.Add("Predeterminada (restablecer)");
        }
        foreach (string id in AvailableExpressionIds(property, character))
            if (!string.Equals(id, DialogueCharacter.DefaultExpressionId, StringComparison.OrdinalIgnoreCase))
            { ids.Add(id); names.Add(id); }
        DialogueOutfitIdDrawer.DrawChoices(position, property, label, ids, names, preserve);
    }

    internal static IEnumerable<string> AvailableExpressionIds(SerializedProperty property, DialogueCharacter character)
    {
        string outfitId = EffectiveOutfit(property, character);
        IReadOnlyList<DialogueExpression> expressions = character.Expressions;
        if (!string.IsNullOrWhiteSpace(outfitId)
            && !string.Equals(outfitId, DialogueCharacter.DefaultOutfitId, StringComparison.OrdinalIgnoreCase))
        {
            expressions = Array.Empty<DialogueExpression>();
            foreach (var outfit in character.Outfits)
                if (outfit != null && string.Equals(outfit.id, outfitId, StringComparison.OrdinalIgnoreCase))
                { expressions = outfit.expressions ?? Array.Empty<DialogueExpression>(); break; }
        }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var expression in expressions)
            if (expression != null && expression.portrait != null && !string.IsNullOrWhiteSpace(expression.id)
                && seen.Add(expression.id)) yield return expression.id;
    }

    // Read SerializedProperty values so outfit changes affect the dropdown immediately,
    // before the Inspector has applied them to the sequence asset.
    private static string EffectiveOutfit(SerializedProperty property, DialogueCharacter character)
    {
        var serialized = property.serializedObject;
        if (!(serialized.targetObject is DialogueSequence)) return null;
        string outfitId = null;
        var initial = serialized.FindProperty("initialOutfits");
        for (int i = 0; i < initial.arraySize; i++)
        {
            var selection = initial.GetArrayElementAtIndex(i);
            if (selection.FindPropertyRelative("character").objectReferenceValue == character)
                outfitId = selection.FindPropertyRelative("outfit").stringValue;
        }
        const string linePrefix = "lines.Array.data[";
        if (!property.propertyPath.StartsWith(linePrefix, StringComparison.Ordinal)) return outfitId;
        int end = property.propertyPath.IndexOf(']', linePrefix.Length);
        if (end < 0 || !int.TryParse(property.propertyPath.Substring(linePrefix.Length, end - linePrefix.Length), out int lineIndex))
            return outfitId;
        var lines = serialized.FindProperty("lines");
        // Include this line: its outfit change precedes both the speaker expression and reactions.
        for (int i = 0; i <= lineIndex && i < lines.arraySize; i++)
        {
            var line = lines.GetArrayElementAtIndex(i);
            if (line.FindPropertyRelative("character").objectReferenceValue != character) continue;
            string selected = line.FindPropertyRelative("outfit").stringValue;
            if (!string.IsNullOrWhiteSpace(selected)) outfitId = selected;
        }
        return outfitId;
    }
}

[CustomEditor(typeof(DialogueCharacter)), CanEditMultipleObjects]
public class DialogueCharacterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        foreach (DialogueCharacter character in targets)
        {
            if (!character.ValidateOutfits(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (!character.ValidateExpressions(out error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }
}

[CustomEditor(typeof(DialogueSequence)), CanEditMultipleObjects]
public class DialogueSequenceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        foreach (DialogueSequence sequence in targets)
            if (!sequence.ValidateAppearances(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
    }
}
