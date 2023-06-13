using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PageTable : MonoBehaviour
{
    private static readonly int VTLookupTex = Shader.PropertyToID("_VTLookupTex");
    private static readonly int VTPageParam = Shader.PropertyToID("_VTPageParam");
    private static readonly int PageInfo = Shader.PropertyToID("_PageInfo");
    private static readonly int ImageMvp = Shader.PropertyToID("_ImageMVP");

    // 页表尺寸
    [SerializeField] private int m_TableSize;

    // 调试着色器.
    // 用于在编辑器中显示贴图 mipmap 等级
    [SerializeField] private Shader m_DebugShader;

    [SerializeField] private Shader m_DrawLookup;

    // 当前活跃的页表
    private readonly Dictionary<Vector2Int, TableNodeCell> m_ActivePages = new();

    private Material drawLookupMat;

    private Material m_DebugMaterial;

    // 导出的页表寻址贴图
    private RenderTexture m_LookupTexture;

    // 页表层级结构
    private PageLevelTable[] m_PageTable;

    // RT Job对象
    private RenderTask m_RenderTask;

    // TiledTexture
    private TiledTexture m_TileTexture;

    private Mesh mQuad;

    // 调试贴图
    public RenderTexture DebugTexture { get; private set; }

    // 页表尺寸.
    public int TableSize => m_TableSize;

    public bool UseFeed { get; set; } = true;

    // 最大mipmap等级
    public int MaxMipLevel => (int)Mathf.Log(TableSize, 2);

    public void Reset()
    {
        for (var i = 0; i <= MaxMipLevel; i++)
        for (var j = 0; j < m_PageTable[i].NodeCellCount; j++)
        for (var k = 0; k < m_PageTable[i].NodeCellCount; k++)
            InvalidatePage(m_PageTable[i].Cell[j, k].Payload.TileIndex);
        m_ActivePages.Clear();
    }

    public void Init(RenderTask job, int tileCount)
    {
        m_RenderTask = job;
        m_RenderTask.StartRenderJob += OnRenderJob;
        m_RenderTask.CancelRenderJob += OnRenderJobCancel;

        m_LookupTexture = new RenderTexture(TableSize, TableSize, 0);
        m_LookupTexture.filterMode = FilterMode.Point;
        m_LookupTexture.wrapMode = TextureWrapMode.Clamp;

        m_PageTable = new PageLevelTable[MaxMipLevel + 1];
        for (var i = 0; i <= MaxMipLevel; i++) m_PageTable[i] = new PageLevelTable(i, TableSize);
        drawLookupMat = new Material(m_DrawLookup);
        drawLookupMat.enableInstancing = true;

        Shader.SetGlobalTexture(
            VTLookupTex,
            m_LookupTexture);
        Shader.SetGlobalVector(
            VTPageParam,
            new Vector4(
                TableSize,
                1.0f / TableSize,
                MaxMipLevel,
                0));

        InitDebugTexture(TableSize, TableSize);
        InitializeQuadMesh();

        m_TileTexture = GetComponent<TiledTexture>();
        m_TileTexture.OnTileUpdateComplete += InvalidatePage;
        GetComponent<FeedbackReader>().OnFeedbackReadComplete += ProcessFeedback;
        ActivatePage(0, 0, MaxMipLevel);
    }

    public void ChangeViewRect(Vector2Int offset)
    {
        for (var i = 0; i <= MaxMipLevel; i++) m_PageTable[i].ChangeViewRect(offset, InvalidatePage);

        ActivatePage(0, 0, MaxMipLevel);
    }

    public void UpdatePage(Vector2Int center)
    {
        if (UseFeed) return;

        for (var i = 0; i < TableSize; i++)
        for (var j = 0; j < TableSize; j++)
        {
            var thisPos = new Vector2Int(i, j);
            var ManhattanDistance = thisPos - center;
            var absX = Mathf.Abs(ManhattanDistance.x);
            var absY = Mathf.Abs(ManhattanDistance.y);
            var absMax = Mathf.Max(absX, absY);
            var tempMipLevel = (int)Mathf.Floor(Mathf.Sqrt(2 * absMax));
            tempMipLevel = Mathf.Clamp(tempMipLevel, 0, MaxMipLevel);
            ActivatePage(i, j, tempMipLevel);
        }

        UpdateLookup();
    }

    private void InitializeQuadMesh()
    {
        var quadVertexList = new List<Vector3>();
        var quadTriangleList = new List<int>();
        var quadUVList = new List<Vector2>();

        quadVertexList.Add(new Vector3(0, 1, 0.1f));
        quadUVList.Add(new Vector2(0, 1));
        quadVertexList.Add(new Vector3(0, 0, 0.1f));
        quadUVList.Add(new Vector2(0, 0));
        quadVertexList.Add(new Vector3(1, 0, 0.1f));
        quadUVList.Add(new Vector2(1, 0));
        quadVertexList.Add(new Vector3(1, 1, 0.1f));
        quadUVList.Add(new Vector2(1, 1));

        quadTriangleList.Add(0);
        quadTriangleList.Add(1);
        quadTriangleList.Add(2);

        quadTriangleList.Add(2);
        quadTriangleList.Add(3);
        quadTriangleList.Add(0);

        mQuad = new Mesh();
        mQuad.SetVertices(quadVertexList);
        mQuad.SetUVs(0, quadUVList);
        mQuad.SetTriangles(quadTriangleList, 0);
    }

    // 处理回读数据
    private void ProcessFeedback(Texture2D texture)
    {
        if (!UseFeed) return;

        // 激活对应页表
        foreach (var c in texture.GetRawTextureData<Color32>()) ActivatePage(c.r, c.g, c.b);

        UpdateLookup();
    }

    private void UpdateLookup()
    {
        // 将页表数据写入页表贴图
        var currentFrame = (byte)Time.frameCount;
        var drawList = new List<DrawPageInfo>();
        foreach (var kv in m_ActivePages)
        {
            var page = kv.Value;
            // 只写入当前帧活跃的页表
            if (page.Payload.ActiveFrame != Time.frameCount)
                continue;

            var table = m_PageTable[page.MipLevel];
            var offset = table.pageOffset;
            var perSize = table.PerCellSize;
            var lb = new Vector2Int(page.Rect.xMin - offset.x * perSize,
                page.Rect.yMin - offset.y * perSize);
            while (lb.x < 0) lb.x += TableSize;
            while (lb.y < 0) lb.y += TableSize;

            drawList.Add(new DrawPageInfo
            {
                rect = new Rect(lb.x, lb.y, page.Rect.width, page.Rect.height),
                mip = page.MipLevel,
                drawPos = new Vector2((float)page.Payload.TileIndex.x / 255,
                    (float)page.Payload.TileIndex.y / 255)
            });
        }

        drawList.Sort((a, b) => -a.mip.CompareTo(b.mip));
        if (drawList.Count == 0) return;

        var mats = new Matrix4x4[drawList.Count];
        var pageInfos = new Vector4[drawList.Count];
        for (var i = 0; i < drawList.Count; i++)
        {
            var size = drawList[i].rect.width / TableSize;
            mats[i] = Matrix4x4.TRS(
                new Vector3(drawList[i].rect.x / TableSize, drawList[i].rect.y / TableSize),
                Quaternion.identity,
                new Vector3(size, size, size));

            pageInfos[i] = new Vector4(drawList[i].drawPos.x, drawList[i].drawPos.y, drawList[i].mip / 255f, 0);
        }

        Graphics.SetRenderTarget(m_LookupTexture);
        var tempCB = new CommandBuffer();
        var block = new MaterialPropertyBlock();
        block.SetVectorArray(PageInfo, pageInfos);
        block.SetMatrixArray(ImageMvp, mats);
        tempCB.DrawMeshInstanced(mQuad, 0, drawLookupMat, 0, mats, mats.Length, block);
        Graphics.ExecuteCommandBuffer(tempCB);
        UpdateDebugTexture();
    }

    // 激活页表
    private TableNodeCell ActivatePage(int x, int y, int mip)
    {
        if (mip > MaxMipLevel || mip < 0 || x < 0 || y < 0 || x >= TableSize || y >= TableSize)
            return null;
        // 找到当前页表
        var page = m_PageTable[mip].Get(x, y);
        if (page == null) return null;
        if (!page.Payload.IsReady)
        {
            LoadPage(x, y, page);

            // 向上找到最近的父节点
            while (mip < MaxMipLevel && !page.Payload.IsReady)
            {
                mip++;
                page = m_PageTable[mip].Get(x, y);
            }
        }

        if (page.Payload.IsReady)
        {
            // 激活对应的平铺贴图块
            m_TileTexture.SetActive(page.Payload.TileIndex);
            page.Payload.ActiveFrame = Time.frameCount;
            return page;
        }

        return null;
    }

    // 加载页表
    private void LoadPage(int x, int y, TableNodeCell node)
    {
        if (node == null)
            return;

        // 正在加载中,不需要重复请求
        if (node.Payload.LoadRequest != null)
            return;

        // 新建加载请求
        node.Payload.LoadRequest = m_RenderTask.Request(x, y, node.MipLevel);
    }

    // 开始渲染
    private void OnRenderJob(RenderTextureRequest request)
    {
        // 找到对应页表
        var node = m_PageTable[request.MipLevel].Get(request.PageX, request.PageY);
        if (node == null || node.Payload.LoadRequest != request)
            return;

        node.Payload.LoadRequest = null;

        var id = m_TileTexture.RequestTile();
        m_TileTexture.UpdateTile(id, request);

        node.Payload.TileIndex = id;
        m_ActivePages[id] = node;
    }

    // 取消渲染
    private void OnRenderJobCancel(RenderTextureRequest request)
    {
        // 找到对应页表
        var node = m_PageTable[request.MipLevel].Get(request.PageX, request.PageY);
        if (node == null || node.Payload.LoadRequest != request)
            return;

        node.Payload.LoadRequest = null;
    }

    // 将页表置为非活跃状态
    private void InvalidatePage(Vector2Int id)
    {
        if (!m_ActivePages.TryGetValue(id, out var node))
            return;

        node.Payload.ResetTileIndex();
        m_ActivePages.Remove(id);
    }

    private void InitDebugTexture(int w, int h)
    {
#if UNITY_EDITOR
        DebugTexture = new RenderTexture(w, h, 0);
        DebugTexture.wrapMode = TextureWrapMode.Clamp;
        DebugTexture.filterMode = FilterMode.Point;
#endif
    }

    private void UpdateDebugTexture()
    {
#if UNITY_EDITOR
        if (m_LookupTexture == null || m_DebugShader == null)
            return;

        if (m_DebugMaterial == null)
            m_DebugMaterial = new Material(m_DebugShader);

        DebugTexture.DiscardContents();
        Graphics.Blit(m_LookupTexture, DebugTexture, m_DebugMaterial);
#endif
    }

    private class DrawPageInfo
    {
        public Vector2 drawPos;
        public int mip;
        public Rect rect;
    }
}