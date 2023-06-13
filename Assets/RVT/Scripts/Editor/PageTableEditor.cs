// using UnityEditor;
// using UnityEngine;
//
// [CustomEditor(typeof(PageTable))]
// public class PageTableEditor : DisplayEditor
// {
//     protected override void OnPlayingInspectorGUI()
//     {
//         var table = (PageTable)target;
//         DrawTexture(table.DebugTexture, "Lookup Texture");
//     }
//
//     private void DrawPreviewTexture(Texture texture)
//     {
//         if (texture == null)
//             return;
//
//         EditorGUILayout.Space();
//         EditorGUILayout.LabelField(string.Format("Texture Size: {0} X {1}", texture.width, texture.height));
//         EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect((float)texture.width / texture.height), texture);
//     }
// }

