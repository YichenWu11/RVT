using UnityEngine;

[ExecuteInEditMode]
public class SetParam : MonoBehaviour
{
    private static readonly int RvtSetColor = Shader.PropertyToID("_RVT_SET_COLOR");
    private static readonly int RvtSetTEX = Shader.PropertyToID("_RVT_SET_TEX");

    public Color color = Color.red;
    public Texture2D tex;

    // Update is called once per frame
    private void Update()
    {
        Shader.SetGlobalColor(RvtSetColor, color);
        Shader.SetGlobalTexture(RvtSetTEX, tex);
    }
}