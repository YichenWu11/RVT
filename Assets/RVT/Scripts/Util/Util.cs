using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum ScaleFactor
{
    // origin
    One,

    // 1/2
    Half,

    // 1/4
    Quarter,

    // 1/8
    Eighth
}

public static class ScaleModeExtensions
{
    public static float ToFloat(this ScaleFactor mode)
    {
        switch (mode)
        {
            case ScaleFactor.Eighth:
                return 0.125f;
            case ScaleFactor.Quarter:
                return 0.25f;
            case ScaleFactor.Half:
                return 0.5f;
        }

        return 1.0f;
    }
}

public class Util
{
    public static Mesh BuildQuadMesh()
    {
        var vertexes = new List<Vector3>();
        var triangles = new List<int>();
        var uvs = new List<Vector2>();

        vertexes.Add(new Vector3(0, 1, 0.1f));
        uvs.Add(new Vector2(0, 1));
        vertexes.Add(new Vector3(0, 0, 0.1f));
        uvs.Add(new Vector2(0, 0));
        vertexes.Add(new Vector3(1, 0, 0.1f));
        uvs.Add(new Vector2(1, 0));
        vertexes.Add(new Vector3(1, 1, 0.1f));
        uvs.Add(new Vector2(1, 1));

        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);

        triangles.Add(2);
        triangles.Add(3);
        triangles.Add(0);

        var quadMesh = new Mesh();
        quadMesh.SetVertices(vertexes);
        quadMesh.SetUVs(0, uvs);
        quadMesh.SetTriangles(triangles, 0);

        return quadMesh;
    }

    public static void DrawTexture(Texture texture, string label = null)
    {
        if (texture == null)
            return;

        EditorGUILayout.Space();
        if (!string.IsNullOrEmpty(label)) EditorGUILayout.LabelField(label);
        EditorGUILayout.LabelField($"Size: {texture.width} X {texture.height}");
        EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(texture.width / (float)texture.height), texture);
    }
}