using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(TiledTexture))]
public class TileTextureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (Application.isPlaying)
        {
            var tileTexture = (TiledTexture)target;

            // Util.DrawTexture(tileTexture.VTRTs[0], "Diffuse");
            Util.DrawTexture(tileTexture.VTRTs[1], "Normal");
            // Util.DrawTexture(tileTexture.VTs[1], "CompressedNormal");
            Util.DrawTexture(tileTexture.VTs[0], "CompressedDiffuse");
        }
        else
        {
            base.OnInspectorGUI();
        }
    }
}
#endif