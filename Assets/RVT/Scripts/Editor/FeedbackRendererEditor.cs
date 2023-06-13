using UnityEditor;

[CustomEditor(typeof(FeedbackRenderer))]
public class FeedbackRendererEditor : DisplayEditor
{
    protected override void OnPlayingInspectorGUI()
    {
        var renderer = (FeedbackRenderer)target;
        DrawTexture(renderer.TargetTexture, "Feedback Texture");
    }
}