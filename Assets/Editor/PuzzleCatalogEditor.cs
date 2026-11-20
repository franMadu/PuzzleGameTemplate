using UnityEditor;

[CustomEditor(typeof(PuzzleCatalog)), CanEditMultipleObjects]
public class PuzzleCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        foreach (PuzzleCatalog catalog in targets)
            if (!catalog.ValidateStore(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
    }
}
