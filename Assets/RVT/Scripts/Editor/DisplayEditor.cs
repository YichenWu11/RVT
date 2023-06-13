using UnityEditor;
using UnityEngine;

public abstract class DisplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (Application.isPlaying)
        {
            OnPlayingInspectorGUI();
        }
        else
        {
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }

    protected abstract void OnPlayingInspectorGUI();

    protected void DrawTexture(Texture texture, string label = null)
    {
        if (texture == null)
            return;

        EditorGUILayout.Space();
        if (!string.IsNullOrEmpty(label)) EditorGUILayout.LabelField(label);
        EditorGUILayout.LabelField($"Size: {texture.width} X {texture.height}");
        EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(texture.width / (float)texture.height), texture);
    }
}