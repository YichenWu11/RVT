using System;
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
    public RectInt Rect { get; set; }

    // 页数据
    public PageData Data { get; set; }

    // MipMap 等级
    public int MipLevel { get; }
}

// 页表节点
public class PageLevelTable
{
    // 当前层级的 Cell 总数量
    public int CellCount;

    public Vector2Int pageOffset;

    // 每个 Cell 占据的尺寸
    public int PerCellSize;

    public PageLevelTable(int mipLevel, int tableSize)
    {
        pageOffset = Vector2Int.zero;
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
    }

    public PageLevelTableNode[,] Cell { get; set; }

    private int MipLevel { get; }

    public void ChangeViewRect(Vector2Int offset, Action<Vector2Int> InvalidatePage)
    {
        if (Mathf.Abs(offset.x) >= CellCount || Mathf.Abs(offset.y) > CellCount ||
            offset.x % PerCellSize != 0 || offset.y % PerCellSize != 0)
        {
            for (var i = 0; i < CellCount; i++)
            for (var j = 0; j < CellCount; j++)
            {
                var transXY = GetTransXY(i, j);
                Cell[transXY.x, transXY.y].Data.LoadRequest = null;
                InvalidatePage(Cell[transXY.x, transXY.y].Data.TileIndex);
            }

            pageOffset = Vector2Int.zero;
            return;
        }

        offset.x /= PerCellSize;
        offset.y /= PerCellSize;

        #region clip map

        if (offset.x > 0)
            for (var i = 0; i < offset.x; i++)
            for (var j = 0; j < CellCount; j++)
            {
                var transXY = GetTransXY(i, j);
                Cell[transXY.x, transXY.y].Data.LoadRequest = null;
                InvalidatePage(Cell[transXY.x, transXY.y].Data.TileIndex);
            }
        else if (offset.x < 0)
            for (var i = 1; i <= -offset.x; i++)
            for (var j = 0; j < CellCount; j++)
            {
                var transXY = GetTransXY(CellCount - i, j);
                Cell[transXY.x, transXY.y].Data.LoadRequest = null;
                InvalidatePage(Cell[transXY.x, transXY.y].Data.TileIndex);
            }

        if (offset.y > 0)
            for (var i = 0; i < offset.y; i++)
            for (var j = 0; j < CellCount; j++)
            {
                var transXY = GetTransXY(j, i);
                Cell[transXY.x, transXY.y].Data.LoadRequest = null;
                InvalidatePage(Cell[transXY.x, transXY.y].Data.TileIndex);
            }
        else if (offset.y < 0)
            for (var i = 1; i <= -offset.y; i++)
            for (var j = 0; j < CellCount; j++)
            {
                var transXY = GetTransXY(j, CellCount - i);
                Cell[transXY.x, transXY.y].Data.LoadRequest = null;
                InvalidatePage(Cell[transXY.x, transXY.y].Data.TileIndex);
            }

        #endregion

        pageOffset += offset;
        while (pageOffset.x < 0) pageOffset.x += CellCount;
        while (pageOffset.y < 0) pageOffset.y += CellCount;
        pageOffset.x %= CellCount;
        pageOffset.y %= CellCount;
    }

    // 取x/y/mip完全一致的node，没有就返回null
    public PageLevelTableNode Get(int x, int y)
    {
        x /= PerCellSize;
        y /= PerCellSize;

        x = (x + pageOffset.x) % CellCount;
        y = (y + pageOffset.y) % CellCount;

        return Cell[x, y];
    }

    private Vector2Int GetTransXY(int x, int y)
    {
        return new Vector2Int(
            (x + pageOffset.x) % CellCount,
            (y + pageOffset.y) % CellCount);
    }
}