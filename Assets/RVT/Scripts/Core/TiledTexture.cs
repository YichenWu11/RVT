using System;
using UnityEngine;

public class TiledTexture : MonoBehaviour
{
    private static readonly int VTDiffuse = Shader.PropertyToID("_VTDiffuse");
    private static readonly int VTNormal = Shader.PropertyToID("_VTNormal");
    private static readonly int VTTileParam = Shader.PropertyToID("_VTTileParam");

    // 区域尺寸: 横竖 (x, z) 方向上 Tile 的数量
    [SerializeField] private Vector2Int m_RegionSize;

    // 单个Tile的尺寸.
    [SerializeField] private int m_TileSize = 256;

    [SerializeField] private int m_PaddingSize = 4;

    // Tile 缓存池
    private readonly LruCache m_TilePool = new();

    public RenderTexture[] VTRTs { get; private set; }

    // 区域尺寸
    public Vector2Int RegionSize => m_RegionSize;

    // 单个Tile的尺寸
    public int TileSize => m_TileSize;

    // 填充尺寸
    // 每个Tile上下左右四个方向都要进行填充，用来支持硬件纹理过滤.
    // 所以Tile有效尺寸为(TileSize - PaddingSize * 2)
    public int PaddingSize => m_PaddingSize;

    public int TileSizeWithPadding => TileSize + PaddingSize * 2;

    public void Reset()
    {
        m_TilePool.Init(RegionSize.x * RegionSize.y);

        VTRTs = new RenderTexture[2];
        VTRTs[0] = new RenderTexture(RegionSize.x * TileSizeWithPadding, RegionSize.y * TileSizeWithPadding, 0);
        VTRTs[0].useMipMap = false;
        VTRTs[0].wrapMode = TextureWrapMode.Clamp;
        VTRTs[0].filterMode = FilterMode.Bilinear;
        Shader.SetGlobalTexture(VTDiffuse, VTRTs[0]);

        VTRTs[1] = new RenderTexture(RegionSize.x * TileSizeWithPadding, RegionSize.y * TileSizeWithPadding, 0);
        VTRTs[1].useMipMap = false;
        VTRTs[1].wrapMode = TextureWrapMode.Clamp;
        VTRTs[1].filterMode = FilterMode.Bilinear;
        Shader.SetGlobalTexture(VTNormal, VTRTs[1]);
    }

    // Tile 更新完成的事件回调
    public event Action<Vector2Int> OnTileUpdateComplete;

    // 画 Tile 的事件
    public event Action<RectInt, RenderTextureRequest> DoDrawTexture;

    public void Init()
    {
        m_TilePool.Init(RegionSize.x * RegionSize.y);

        VTRTs = new RenderTexture[2];
        VTRTs[0] = new RenderTexture(RegionSize.x * TileSizeWithPadding, RegionSize.y * TileSizeWithPadding, 0);
        VTRTs[0].useMipMap = false;
        VTRTs[0].wrapMode = TextureWrapMode.Clamp;
        Shader.SetGlobalTexture(VTDiffuse, VTRTs[0]);

        VTRTs[1] = new RenderTexture(RegionSize.x * TileSizeWithPadding, RegionSize.y * TileSizeWithPadding, 0);
        VTRTs[1].useMipMap = false;
        VTRTs[1].wrapMode = TextureWrapMode.Clamp;
        Shader.SetGlobalTexture(VTNormal, VTRTs[1]);

        // x: padding偏移量
        // y: tile有效区域的尺寸
        // zw: 1 / 区域尺寸
        Shader.SetGlobalVector(
            VTTileParam,
            new Vector4(
                PaddingSize,
                TileSize,
                RegionSize.x * TileSizeWithPadding,
                RegionSize.y * TileSizeWithPadding));
    }

    public Vector2Int RequestTile()
    {
        return IdToPos(m_TilePool.First);
    }

    public bool SetActive(Vector2Int tile)
    {
        var success = m_TilePool.SetActive(PosToId(tile));

        return success;
    }

    public void UpdateTile(Vector2Int tile, RenderTextureRequest request)
    {
        if (!SetActive(tile))
            return;
        DoDrawTexture?.Invoke(
            new RectInt(tile.x * TileSizeWithPadding, tile.y * TileSizeWithPadding, TileSizeWithPadding,
                TileSizeWithPadding),
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