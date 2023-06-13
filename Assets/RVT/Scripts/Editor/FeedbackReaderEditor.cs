using UnityEditor;

[CustomEditor(typeof(FeedbackReader))]
public class FeedbackReaderEditor : DisplayEditor
{
    protected override void OnPlayingInspectorGUI()
    {
        var reader = (FeedbackReader)target;
        DrawTexture(reader.DebugTexture, "Mipmap Level Debug Texture");
    }
}