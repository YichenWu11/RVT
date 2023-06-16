using UnityEngine;

// 页数据
public class PageData
{
    private static readonly Vector2Int SInvalidTileIndex = new(-1, -1);

    // 激活的帧序号
    public int ActiveFrame;

    // 渲染请求
    public RenderTextureRequest LoadRequest;

    // 对应 TiledTexture 中的 id
    public Vector2Int TileIndex = SInvalidTileIndex;

    // 是否处于可用状态
    public bool IsReady => TileIndex != SInvalidTileIndex;

    // 重置页表数据
    public void ResetTileIndex()
    {
        TileIndex = SInvalidTileIndex;
    }
}

public class PageLevelTableNode
{
    public PageLevelTableNode(int x, int y, int width, int height, int mip)
    {
        Rect = new RectInt(x, y, width, height);
        MipLevel = mip;
        Data = new PageData();
    }

    // 占据的 Rect 区域
    public RectInt Rect { get; }

    // 页数据
    public PageData Data { get; }

    // MipMap 等级
    public int MipLevel { get; }
}

// 页表节点
public class PageLevelTable
{
    // 当前层级的 Cell 总数量
    public int CellCount;

    // 每个 Cell 占据的尺寸
    public int PerCellSize;

    public PageLevelTable(int mipLevel, int tableSize)
    {
        MipLevel = mipLevel;
        PerCellSize = (int)Mathf.Pow(2, mipLevel);
        CellCount = tableSize / PerCellSize;
        Cell = new PageLevelTableNode[CellCount, CellCount];
        for (var i = 0; i < CellCount; i++)
        for (var j = 0; j < CellCount; j++)
            Cell[i, j] = new PageLevelTableNode(
                i * PerCellSize,
                j * PerCellSize,
                PerCellSize,
                PerCellSize,
                MipLevel);
        // Debug.Log($"MipLevel {mipLevel} : {PerCellSize} {tableSize}");
    }

    public PageLevelTableNode[,] Cell { get; }

    private int MipLevel { get; }

    public PageLevelTableNode Get(int x, int y)
    {
        x /= PerCellSize;
        y /= PerCellSize;

        x %= CellCount;
        y %= CellCount;

        return Cell[x, y];
    }
}