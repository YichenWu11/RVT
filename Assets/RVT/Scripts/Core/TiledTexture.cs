using System;
using UnityEngine;

public class TiledTexture : MonoBehaviour
{
    private static readonly int VTDiffuse = Shader.PropertyToID("_VTDiffuse");
    private static readonly int VTNormal = Shader.PropertyToID("_VTNormal");
    private static readonly int VTTileParam = Shader.PropertyToID("_VTTileParam");

    // 区域尺寸: 横竖 (x, z) 方向上 Tile 的数量
    [SerializeField] private Vector2Int regionSize;

    // 单个Tile的尺寸.
    [SerializeField] private int tileSize = 256;

    // 填充
    [SerializeField] private int boundSize = 4;

    // Tile 缓存池
    private readonly LruCache _tilePool = new();

    public int TileSizeWithBound => TileSize + boundSize * 2;

    public RenderTexture[] VTRTs { get; private set; }

    // 区域尺寸
    public Vector2Int RegionSize => regionSize;

    // 单个Tile的尺寸
    public int TileSize => tileSize;

    public int BoundSize => boundSize;

    // Tile 更新完成的事件回调
    public event Action<Vector2Int> OnTileUpdateComplete;

    // 画 Tile 的事件
    public event Action<RectInt, RenderTextureRequest> DrawTexture;

    public void Init()
    {
        _tilePool.Init(RegionSize.x * RegionSize.y);

        VTRTs = new RenderTexture[2];
        VTRTs[0] = new RenderTexture(RegionSize.x * TileSizeWithBound, RegionSize.y * TileSizeWithBound, 0)
        {
            useMipMap = false,
            wrapMode = TextureWrapMode.Clamp
        };
        Shader.SetGlobalTexture(VTDiffuse, VTRTs[0]);

        VTRTs[1] = new RenderTexture(RegionSize.x * TileSizeWithBound, RegionSize.y * TileSizeWithBound, 0)
        {
            useMipMap = false,
            wrapMode = TextureWrapMode.Clamp
        };
        Shader.SetGlobalTexture(VTNormal, VTRTs[1]);

        // x: padding偏移量
        // y: tile有效区域的尺寸
        // z: 区域尺寸.x
        // w: 区域尺寸.y
        Shader.SetGlobalVector(
            VTTileParam,
            new Vector4(
                boundSize,
                TileSize,
                RegionSize.x * TileSizeWithBound,
                RegionSize.y * TileSizeWithBound));
    }

    public Vector2Int RequestTile()
    {
        return IdToPos(_tilePool.First);
    }

    public bool SetActive(Vector2Int tile)
    {
        var success = _tilePool.SetActive(PosToId(tile));

        return success;
    }

    public void UpdateTile(Vector2Int tile, RenderTextureRequest request)
    {
        if (!SetActive(tile))
            return;
        DrawTexture?.Invoke(
            new RectInt(
                tile.x * TileSizeWithBound,
                tile.y * TileSizeWithBound,
                TileSizeWithBound,
                TileSizeWithBound),
            request);
        OnTileUpdateComplete?.Invoke(tile);
    }

    private Vector2Int IdToPos(int id)
    {
        return new Vector2Int(id % RegionSize.x, id / RegionSize.x);
    }

    private int PosToId(Vector2Int tile)
    {
        return tile.y * RegionSize.x + tile.x;
    }
}