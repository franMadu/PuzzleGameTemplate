using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueOutfitIdAttribute))]
public class DialogueOutfitIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        int separator = property.propertyPath.LastIndexOf('.');
        string parent = separator >= 0 ? property.propertyPath.Substring(0, separator + 1) : "";
        var character = property.serializedObject.FindProperty(parent + "character")?.objectReferenceValue as DialogueCharacter;
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
        string current = property.stringValue ?? "";
        int selected = ids.FindIndex(id => string.Equals(id, current, StringComparison.OrdinalIgnoreCase));
        if (selected < 0 && !setting.PreservePrevious && string.Equals(current, DialogueCharacter.DefaultOutfitId, StringComparison.OrdinalIgnoreCase)) selected = 0;
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

[CustomEditor(typeof(DialogueCharacter)), CanEditMultipleObjects]
public class DialogueCharacterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        foreach (DialogueCharacter character in targets)
            if (!character.ValidateOutfits(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
    }
}

[CustomEditor(typeof(DialogueSequence)), CanEditMultipleObjects]
public class DialogueSequenceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        foreach (DialogueSequence sequence in targets)
            if (!sequence.ValidateOutfits(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
    }
}
