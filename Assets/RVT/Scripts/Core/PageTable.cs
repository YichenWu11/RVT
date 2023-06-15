using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class PageTable : MonoBehaviour
{
    private static readonly int VTLookupTex = Shader.PropertyToID("_VTLookupTex");
    private static readonly int VTPageParam = Shader.PropertyToID("_VTPageParam");

    private static readonly int PageInfo = Shader.PropertyToID("_PageInfo");
    private static readonly int ImageMvp = Shader.PropertyToID("_ImageMVP");

    // 页表尺寸
    [SerializeField] private int tableSize = 128;

    [SerializeField] private Shader debugShader;

    [SerializeField] private Shader drawLookupShader;

    // 当前活跃的页表
    private readonly Dictionary<Vector2Int, PageLevelTableNode> _activePages = new();

    private Material _debugMaterial;

    private Material _drawLookupMaterial;

    // 导出的页表寻址贴图
    private Texture2D _lookupTexture;

    // 页表层级结构
    private PageLevelTable[] _pageTable;

    private Mesh _quadMesh;

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

    public void Init(RenderTask task, int tileCount)
    {
        _renderTask = task;
        _renderTask.StartRenderTask += OnRenderTask;
        _renderTask.CancelRenderTask += OnRenderTaskCancel;

        _lookupTexture = new Texture2D(TableSize, TableSize, TextureFormat.RGBA32, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // _lookupTexture = new RenderTexture(TableSize, TableSize, 0)
        // {
        //     filterMode = FilterMode.Point,
        //     wrapMode = TextureWrapMode.Clamp
        // };

        _pageTable = new PageLevelTable[MaxMipLevel + 1];
        for (var i = 0; i <= MaxMipLevel; i++) _pageTable[i] = new PageLevelTable(i, TableSize);

        _drawLookupMaterial = new Material(drawLookupShader)
        {
            enableInstancing = true
        };

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

        _quadMesh = Util.BuildQuadMesh();

        _tileTexture = GetComponent<TiledTexture>();
        _tileTexture.OnTileUpdateComplete += InvalidatePage;
        GetComponent<FeedbackReader>().OnFeedbackReadComplete += ProcessFeedback;
        ActivatePage(0, 0, MaxMipLevel);
    }

    public void ChangeViewRect(Vector2Int offset)
    {
        for (var i = 0; i <= MaxMipLevel; i++) _pageTable[i].ChangeViewRect(offset, InvalidatePage);
        ActivatePage(0, 0, MaxMipLevel);
    }

    // 处理回读
    private void ProcessFeedback(Texture2D texture)
    {
        // 激活对应页表
        foreach (var color in texture.GetPixels())
            ActivatePage(
                Mathf.FloorToInt(color.r * 255.0f),
                Mathf.FloorToInt(color.g * 255.0f),
                Mathf.FloorToInt(color.b * 255.0f));

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

            // a位保存写入frame序号，用于检查pixels是否为当前帧写入的数据(避免旧数据残留)
            var c = new Color32((byte)page.Data.TileIndex.x, (byte)page.Data.TileIndex.y, (byte)page.MipLevel,
                currentFrame);
            for (var y = page.Rect.y; y < page.Rect.yMax; y++)
            for (var x = page.Rect.x; x < page.Rect.xMax; x++)
            {
                var id = y * TableSize + x;
                var curPixColor = _lookupTexture;
                if (pixels[id].b > c.b || // 写入mipmap等级最小的页表
                    pixels[id].a != currentFrame) // 当前帧还没有写入过数据
                    pixels[id] = c;
            }
        }

        _lookupTexture.Apply(false);

        UpdateDebugTexture();

        // // 将页表数据写入页表贴图
        // var currentFrame = (byte)Time.frameCount;
        // var drawList = new List<DrawPageInfo>();
        // foreach (var kv in _activePages)
        // {
        //     var page = kv.Value;
        //     // 只写入当前帧活跃的页表
        //     if (page.Data.ActiveFrame != Time.frameCount)
        //         continue;
        //
        //     var table = _pageTable[page.MipLevel];
        //     var offset = table.pageOffset;
        //     var perSize = table.PerCellSize;
        //     var lb = new Vector2Int(page.Rect.xMin - offset.x * perSize,
        //         page.Rect.yMin - offset.y * perSize);
        //     while (lb.x < 0) lb.x += TableSize;
        //     while (lb.y < 0) lb.y += TableSize;
        //
        //     drawList.Add(new DrawPageInfo
        //     {
        //         rect = new Rect(lb.x, lb.y, page.Rect.width, page.Rect.height),
        //         mip = page.MipLevel,
        //         drawPos = new Vector2((float)page.Data.TileIndex.x / 255,
        //             (float)page.Data.TileIndex.y / 255)
        //     });
        // }
        //
        // drawList.Sort((a, b) => -a.mip.CompareTo(b.mip));
        // if (drawList.Count == 0) return;
        //
        // var mats = new Matrix4x4[drawList.Count];
        // var pageInfos = new Vector4[drawList.Count];
        // for (var i = 0; i < drawList.Count; i++)
        // {
        //     var size = drawList[i].rect.width / TableSize;
        //     mats[i] = Matrix4x4.TRS(
        //         new Vector3(drawList[i].rect.x / TableSize, drawList[i].rect.y / TableSize),
        //         Quaternion.identity,
        //         new Vector3(size, size, size));
        //
        //     pageInfos[i] = new Vector4(drawList[i].drawPos.x, drawList[i].drawPos.y, drawList[i].mip / 255f, 0);
        // }
        //
        // Graphics.SetRenderTarget(_lookupTexture);
        // var tempCB = new CommandBuffer();
        // var block = new MaterialPropertyBlock();
        // block.SetVectorArray(PageInfo, pageInfos);
        // block.SetMatrixArray(ImageMvp, mats);
        // tempCB.DrawMeshInstanced(_quadMesh, 0, _drawLookupMaterial, 0, mats, mats.Length, block);
        // Graphics.ExecuteCommandBuffer(tempCB);
        //
        // UpdateDebugTexture();
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

        if (page.Data.IsReady)
        {
            // 激活对应的平铺贴图块
            _tileTexture.SetActive(page.Data.TileIndex);
            page.Data.ActiveFrame = Time.frameCount;
            return page;
        }

        return null;
    }

    // 加载页表
    private void LoadPage(int x, int y, PageLevelTableNode node)
    {
        if (node == null)
            return;

        // 正在加载中,不需要重复请求
        if (node.Data.LoadRequest != null)
            return;

        // 新建加载请求
        node.Data.LoadRequest = _renderTask.Request(x, y, node.MipLevel);
    }

    public void UpdatePage(Vector2Int center)
    {
        for (var i = 0; i < TableSize; i++)
        for (var j = 0; j < TableSize; j++)
        {
            var thisPos = new Vector2Int(i, j);
            var distance = thisPos - center;
            var absX = Mathf.Abs(distance.x);
            var absY = Mathf.Abs(distance.y);
            var absMax = Mathf.Max(absX, absY);
            var tempMipLevel = (int)Mathf.Floor(Mathf.Sqrt(2 * absMax));
            tempMipLevel = Mathf.Clamp(tempMipLevel, 0, MaxMipLevel);
            ActivatePage(i, j, tempMipLevel);
        }

        UpdateLookup();
    }

    private void OnRenderTask(RenderTextureRequest request)
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

    private void OnRenderTaskCancel(RenderTextureRequest request)
    {
        var node = _pageTable[request.MipLevel].Get(request.PageX, request.PageY);
        if (node == null || node.Data.LoadRequest != request)
            return;

        node.Data.LoadRequest = null;
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
        if (_lookupTexture == null || debugShader == null)
            return;

        if (_debugMaterial == null)
            _debugMaterial = new Material(debugShader);

        DebugTexture.DiscardContents();
        Graphics.Blit(_lookupTexture, DebugTexture, _debugMaterial);
#endif
    }

    private class DrawPageInfo
    {
        public Vector2 drawPos;
        public int mip;
        public Rect rect;
    }
}