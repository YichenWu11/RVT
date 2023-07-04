// using System.Collections;
// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
//
// [CustomEditor(typeof(CompressTest))]
// public class CompressTestEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         if (Application.isPlaying)
//         {
//             var tar = (CompressTest)target;
//             Util.DrawTexture(tar.unCompressed, "UnCompressed");
//             Util.DrawTexture(tar.compressed, "Compressed");
//             Util.DrawTexture(tar.compressed2D, "Compressed2D");
//         }
//         else
//         {
//             base.OnInspectorGUI();
//         }
//     }
// }