using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RVTTerrain))]
public class RVTTerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (Application.isPlaying)
        {
            var rvt = (RVTTerrain)target;

            // Util.DrawTexture(rvt.albedoTileRT, "AlbedoTile");
            // Util.DrawTexture(rvt.normalTileRT, "NormalTile");

            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
        else
        {
            base.OnInspectorGUI();
        }
    }
}