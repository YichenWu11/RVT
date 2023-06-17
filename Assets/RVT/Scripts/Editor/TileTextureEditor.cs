using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TiledTexture))]
public class TileTextureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (Application.isPlaying)
        {
            var tileTexture = (TiledTexture)target;

            Util.DrawTexture(tileTexture.VTRTs[0], "Diffuse");
            Util.DrawTexture(tileTexture.VTRTs[1], "Normal");
        }
        else
        {
            base.OnInspectorGUI();
        }
    }
}