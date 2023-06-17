using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FeedbackReader))]
public class FeedbackReaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (Application.isPlaying)
        {
            var reader = (FeedbackReader)target;
            Util.DrawTexture(reader.DebugTexture, "Mipmap Level Debug Texture");
        }
        else
        {
            base.OnInspectorGUI();
        }
    }
}