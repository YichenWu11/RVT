using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class PageTable : MonoBehaviour
{
    private static readonly int VTLookupTex = Shader.PropertyToID("_VTLookupTex");
    private static readonly int VTPageParam = Shader.PropertyToID("_VTPageParam");

    // 页表尺寸
    [SerializeField] private int tableSize = 128;

    [SerializeField] private Material debugMaterial;

    // 当前活跃的页表
    private readonly Dictionary<Vector2Int, PageLevelTableNode> _activePages = new();

    // 导出的页表寻址贴图
    private Texture2D _lookupTexture;

    // 页表层级结构
    private PageLevelTable[] _pageTable;

    // RT Job对象
    private RenderTask _renderTask;

    // TiledTexture
    private TiledTexture _tileTexture;

    // 调试贴图
    public RenderTexture DebugTexture { get; private set; }

    // 页表尺寸.
    public int TableSize => tableSize;

    // 最大mipmap等级
    public int MaxMipLevel => (int)Mathf.Log(TableSize, 2);

    public void Init(RenderTask task)
    {
        _renderTask = task;
        _renderTask.StartRenderTask += OnRenderTask;

        _lookupTexture = new Texture2D(TableSize, TableSize, TextureFormat.RGBA32, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        _pageTable = new PageLevelTable[MaxMipLevel + 1];
        for (var i = 0; i <= MaxMipLevel; i++) _pageTable[i] = new PageLevelTable(i, TableSize);

        Shader.SetGlobalTexture(
            VTLookupTex,
            _lookupTexture);
        Shader.SetGlobalVector(
            VTPageParam,
            new Vector4(
                TableSize,
                1.0f / TableSize,
                MaxMipLevel,
                0));

        // 创建 DebugTexture
#if UNITY_EDITOR
        DebugTexture = new RenderTexture(TableSize, TableSize, 0, GraphicsFormat.R8G8B8A8_UNorm)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
#endif

        _tileTexture = GetComponent<TiledTexture>();
        _tileTexture.OnTileUpdateComplete += InvalidatePage;
        GetComponent<FeedbackReader>().OnFeedbackReadComplete += ProcessFeedback;
    }

    // 处理回读
    private void ProcessFeedback(Texture2D texture)
    {
        foreach (var color in texture.GetRawTextureData<Color32>())
            ActivatePage(color.r, color.g, color.b);

        UpdateLookup();
    }

    private void UpdateLookup()
    {
        var pixels = _lookupTexture.GetRawTextureData<Color32>();
        // 将页表数据写入页表贴图
        var currentFrame = (byte)Time.frameCount;
        foreach (var kv in _activePages)
        {
            var page = kv.Value;

            // 只写入当前帧活跃的页表
            if (page.Data.ActiveFrame != Time.frameCount)
                continue;

            var color = new Color32(
                (byte)page.Data.TileIndex.x,
                (byte)page.Data.TileIndex.y,
                (byte)page.MipLevel,
                currentFrame);
            for (var y = page.Rect.y; y < page.Rect.yMax; y++)
            for (var x = page.Rect.x; x < page.Rect.xMax; x++)
            {
                var id = y * TableSize + x;
                if (pixels[id].b > color.b || pixels[id].a != currentFrame)
                    pixels[id] = color;
            }
        }

        // 将改动同步到 GPU 端
        _lookupTexture.Apply(false);

        UpdateDebugTexture();
    }

    // 激活页表
    private PageLevelTableNode ActivatePage(int x, int y, int mip)
    {
        if (mip > MaxMipLevel || mip < 0 || x < 0 || y < 0 || x >= TableSize || y >= TableSize)
            return null;

        // 找到当前页表
        var page = _pageTable[mip].Get(x, y);
        if (page == null) return null;
        if (!page.Data.IsReady)
        {
            LoadPage(x, y, page);

            // 向上找到最近的父节点
            while (mip < MaxMipLevel && !page.Data.IsReady)
            {
                mip++;
                page = _pageTable[mip].Get(x, y);
            }
        }

        if (!page.Data.IsReady) return null;
        // 激活对应的平铺贴图块
        _tileTexture.SetActive(page.Data.TileIndex);
        page.Data.ActiveFrame = Time.frameCount;
        return page;
    }

    // 加载页表
    private void LoadPage(int x, int y, PageLevelTableNode node)
    {
        if (node == null)
            return;

        if (node.Data.LoadRequest != null)
            return;

        // 加载请求
        node.Data.LoadRequest = _renderTask.Request(x, y, node.MipLevel);
    }

    private void OnRenderTask(RenderRequest request)
    {
        var node = _pageTable[request.MipLevel].Get(request.PageX, request.PageY);
        if (node == null || node.Data.LoadRequest != request)
            return;

        node.Data.LoadRequest = null;

        var id = _tileTexture.RequestTile();
        _tileTexture.UpdateTile(id, request);

        node.Data.TileIndex = id;
        _activePages[id] = node;
    }

    // 将页表置为非活跃状态
    private void InvalidatePage(Vector2Int id)
    {
        if (!_activePages.TryGetValue(id, out var node))
            return;

        node.Data.ResetTileIndex();
        _activePages.Remove(id);
    }

    private void UpdateDebugTexture()
    {
#if UNITY_EDITOR
        if (debugMaterial == null)
            return;

        DebugTexture.DiscardContents();
        Graphics.Blit(_lookupTexture, DebugTexture, debugMaterial);
#endif
    }
}