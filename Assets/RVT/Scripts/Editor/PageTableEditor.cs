using UnityEditor;

[CustomEditor(typeof(PageTable))]
public class PageTableEditor : DisplayEditor
{
    protected override void OnPlayingInspectorGUI()
    {
        var table = (PageTable)target;
        DrawTexture(table.DebugTexture, "Lookup Texture");
    }
}