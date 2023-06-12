using UnityEngine;

public class QuadTreeNode
{
    public Bounds bound;

    public QuadTreeNode leftDown;
    public QuadTreeNode leftUp;
    public QuadTreeNode rightDown;
    public QuadTreeNode rightUp;
}

[ExecuteInEditMode]
public class QuadTree : MonoBehaviour
{
    public int width = 1024;
    public int length = 1024;
    public int minCellSize = 64;

    private int _maxMipLevel;
    private QuadTreeNode _root;

    private void Start()
    {
        _maxMipLevel = width / minCellSize;
        _root = new QuadTreeNode
        {
            bound = new Bounds(new Vector3(width / 2, 0, length / 2), new Vector3(width, 0, length))
        };

        BuildQuadTree();
    }

    private void Update()
    {
        _root = new QuadTreeNode
        {
            bound = new Bounds(new Vector3(width / 2, 0, length / 2), new Vector3(width, 0, length))
        };
        BuildQuadTree();
#if UNITY_EDITOR
        DebugDrawQuadTree();
#endif
    }

    private void DebugDrawQuadTree()
    {
        DebugDrawQuadNode(_root);
    }

    private void DebugDrawQuadNode(QuadTreeNode node)
    {
        if (node == null) return;
        var bound = node.bound;
        var cellSize = node.bound.size.x;
        var p1 = bound.center + new Vector3(-cellSize / 2, 0, -cellSize / 2);
        var p2 = bound.center + new Vector3(cellSize / 2, 0, -cellSize / 2);
        var p3 = bound.center + new Vector3(cellSize / 2, 0, cellSize / 2);
        var p4 = bound.center + new Vector3(-cellSize / 2, 0, cellSize / 2);
        Debug.DrawLine(p1, p2, Color.red);
        Debug.DrawLine(p2, p3, Color.red);
        Debug.DrawLine(p3, p4, Color.red);
        Debug.DrawLine(p4, p1, Color.red);
        DebugDrawQuadNode(node.leftDown);
        DebugDrawQuadNode(node.leftUp);
        DebugDrawQuadNode(node.rightDown);
        DebugDrawQuadNode(node.rightUp);
    }

    private void BuildQuadTree()
    {
        GenerateChildNodes(_root);
    }

    private void GenerateChildNodes(QuadTreeNode node)
    {
        // Debug.Log(
        //     $"GenerateNode : center{node.bound.center.x},{node.bound.center.z}, size{node.bound.size.x},{node.bound.size.z}");
        var curLevelQuadSize = Mathf.CeilToInt(node.bound.size.x);
        if (curLevelQuadSize == minCellSize)
            return;
        var nxtLevelQuadSize = curLevelQuadSize / 2;
        node.leftDown = new QuadTreeNode
        {
            bound = new Bounds(node.bound.center + new Vector3(-nxtLevelQuadSize / 2, 0, -nxtLevelQuadSize / 2),
                new Vector3(nxtLevelQuadSize, 0, nxtLevelQuadSize))
        };
        node.leftUp = new QuadTreeNode
        {
            bound = new Bounds(node.bound.center + new Vector3(-nxtLevelQuadSize / 2, 0, nxtLevelQuadSize / 2),
                new Vector3(nxtLevelQuadSize, 0, nxtLevelQuadSize))
        };
        node.rightDown = new QuadTreeNode
        {
            bound = new Bounds(node.bound.center + new Vector3(nxtLevelQuadSize / 2, 0, -nxtLevelQuadSize / 2),
                new Vector3(nxtLevelQuadSize, 0, nxtLevelQuadSize))
        };
        node.rightUp = new QuadTreeNode
        {
            bound = new Bounds(node.bound.center + new Vector3(nxtLevelQuadSize / 2, 0, nxtLevelQuadSize / 2),
                new Vector3(nxtLevelQuadSize, 0, nxtLevelQuadSize))
        };
        var pos = transform.position;
        var cameraPos = new Vector3(pos.x, 0, pos.z);
        if (node.leftDown.bound.Contains(cameraPos))
            GenerateChildNodes(node.leftDown);
        else if (node.leftUp.bound.Contains(cameraPos))
            GenerateChildNodes(node.leftUp);
        else if (node.rightDown.bound.Contains(cameraPos))
            GenerateChildNodes(node.rightDown);
        else if (node.rightUp.bound.Contains(cameraPos))
            GenerateChildNodes(node.rightUp);
    }
}