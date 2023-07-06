using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
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
            Util.DrawTexture(rvt.decalRT, "DecalRT");

            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
        else
        {
            base.OnInspectorGUI();
        }
    }
}
#endif