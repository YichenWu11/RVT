using UnityEngine;

[ExecuteInEditMode]
public class GridDivide : MonoBehaviour
{
    public int width = 1024;
    public int length = 1024;
    public int cellSize = 128;

    private Bounds[] _cellBounds;


    private void Start()
    {
        var xNum = width / cellSize;
        var zNum = length / cellSize;
        _cellBounds = new Bounds[xNum * zNum];
        for (var coordX = 0; coordX < xNum; ++coordX)
        for (var coordZ = 0; coordZ < zNum; ++coordZ)
        {
            var center = new Vector3(coordX * cellSize + cellSize / 2, 0, coordZ * cellSize + cellSize / 2);
            var size = new Vector3(cellSize, 0, cellSize);
            _cellBounds[coordX * xNum + coordZ] = new Bounds(center, size);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        foreach (var cell in _cellBounds)
        {
            var p1 = cell.center + new Vector3(-cellSize / 2, 0, -cellSize / 2);
            var p2 = cell.center + new Vector3(cellSize / 2, 0, -cellSize / 2);
            var p3 = cell.center + new Vector3(cellSize / 2, 0, cellSize / 2);
            var p4 = cell.center + new Vector3(-cellSize / 2, 0, cellSize / 2);
            Debug.DrawLine(p1, p2, Color.red);
            Debug.DrawLine(p2, p3, Color.red);
            Debug.DrawLine(p3, p4, Color.red);
            Debug.DrawLine(p4, p1, Color.red);
        }
#endif
    }
}