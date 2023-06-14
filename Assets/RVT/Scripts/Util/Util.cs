using System.Collections.Generic;
using UnityEngine;

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
}