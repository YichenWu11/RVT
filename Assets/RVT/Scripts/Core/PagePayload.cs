using UnityEngine;

// 页表数据
public class PagePayload
{
    private static readonly Vector2Int s_InvalidTileIndex = new(-1, -1);

    // 激活的帧序号
    public int ActiveFrame;

    // 渲染请求
    public RenderTextureRequest LoadRequest;

    // 对应 TiledTexture 中的 id
    public Vector2Int TileIndex = s_InvalidTileIndex;

    // 是否处于可用状态
    public bool IsReady => TileIndex != s_InvalidTileIndex;

    // 重置页表数据
    public void ResetTileIndex()
    {
        TileIndex = s_InvalidTileIndex;
    }
}