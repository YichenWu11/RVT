using System;
using UnityEngine;
using UnityEngine.Profiling;

public class RVTTerrain : MonoBehaviour
{
    private static readonly int VTRegionRect = Shader.PropertyToID("_VTRegionRect");
    private static readonly int BlendTile = Shader.PropertyToID("_BlendTile");
    private static readonly int Blend = Shader.PropertyToID("_Blend");
    private static readonly int DecalOffset0 = Shader.PropertyToID("_DecalOffset0");

    // RVT Settings
    [Header("RVT Settings")] public bool EnableRVTUpdate = true;
    public bool EnableUseRVTLit = true;

    [Space] public Terrain terrain;

    // Terrain Region 占据的 Rect
    public Rect regionRect = new(0, 0, 1024, 1024);

    // 贴图绘制材质
    public Material drawTextureMaterial;

    private readonly int _feedbackInterval = 8;

    private readonly RenderTask _renderTask = new();

    // Feedback Pass Renderer & Reader
    private FeedbackReader _feedbackReader;
    private FeedbackRenderer _feedbackRenderer;

    // Decal Renderer
    private DecalRenderer _decalRenderer;

    // helper mesh
    private Mesh _quadMesh;

    // TiledTexture
    private TiledTexture _tiledTexture;

    // TiledTexture 尺寸
    private Vector2Int _tiledTextureSize;

    // 页表
    private PageTable _pageTable;

    // From TiledTexture
    private RenderBuffer _VTDepthBuffer;
    private RenderBuffer[] _VTTileBuffer;

    private void Start()
    {
        _pageTable = GetComponent<PageTable>();
        _feedbackRenderer = GetComponent<FeedbackRenderer>();
        _feedbackReader = GetComponent<FeedbackReader>();
        _tiledTexture = GetComponent<TiledTexture>();
        _decalRenderer = GetComponent<DecalRenderer>();

        Shader.SetGlobalVector(
            VTRegionRect,
            new Vector4(regionRect.xMin, regionRect.yMin, regionRect.width, regionRect.height));

        _tiledTexture.Init();
        _tiledTexture.DrawTexture += DrawTiledTexture;

        _pageTable.Init(_renderTask);

        _quadMesh = Util.BuildQuadMesh();

        _VTTileBuffer = new RenderBuffer[2];
        _VTTileBuffer[0] = _tiledTexture.VTRTs[0].colorBuffer;
        _VTTileBuffer[1] = _tiledTexture.VTRTs[1].colorBuffer;
        _VTDepthBuffer = _tiledTexture.VTRTs[0].depthBuffer;
        _tiledTextureSize = new Vector2Int(_tiledTexture.VTRTs[0].width, _tiledTexture.VTRTs[0].height);
    }

    private void Update()
    {
        if (EnableUseRVTLit) Shader.EnableKeyword("_USE_RVT_LIT");
        else Shader.DisableKeyword("_USE_RVT_LIT");
        if (!EnableRVTUpdate) return;

        Profiler.BeginSample("RVT");
        _feedbackReader.UpdateRequest();
        if (_feedbackReader.CanRead && Time.frameCount % _feedbackInterval == 0)
        {
            Profiler.BeginSample("Feedback Render");
            _feedbackRenderer.FeedbackCamera.Render();
            Profiler.EndSample();

            _feedbackReader.ReadbackRequest(_feedbackRenderer.TargetTexture);
        }

        _renderTask.Update();
        Profiler.EndSample();
    }

    private void DrawTiledTexture(RectInt drawPos, RenderRequest request)
    {
        DrawTiledTextureImpl(drawPos, request);
    }

    public void DrawDecalToTiledTexture(RectInt drawPos, RenderRequest request, DecalRenderer.DecalInfo decalInfo)
    {
        DrawTiledTextureImpl(drawPos, request, true, decalInfo);
    }

    private void DrawTiledTextureImpl(
        RectInt drawPos,
        RenderRequest request,
        bool decal = false,
        DecalRenderer.DecalInfo decalInfo = null)
    {
        var x = request.PageX;
        var y = request.PageY;
        var perCellSize = (int)Mathf.Pow(2, request.MipLevel);

        // 转换到对应 Mip 层级页表上格子坐标
        x -= x % perCellSize;
        y -= y % perCellSize;

        var boundOffset =
            (float)_tiledTexture.BoundSize / _tiledTexture.TileSize * perCellSize *
            (regionRect.width / _pageTable.TableSize);

        var realRect = new Rect(
            regionRect.xMin + (float)x / _pageTable.TableSize * regionRect.width - boundOffset,
            regionRect.yMin + (float)y / _pageTable.TableSize * regionRect.height - boundOffset,
            regionRect.width / _pageTable.TableSize * perCellSize + 2.0f * boundOffset,
            regionRect.width / _pageTable.TableSize * perCellSize + 2.0f * boundOffset);


        var terrainRect = Rect.zero;
        terrainRect.xMin = terrain.transform.position.x;
        terrainRect.yMin = terrain.transform.position.z;
        terrainRect.width = terrain.terrainData.size.x;
        terrainRect.height = terrain.terrainData.size.z;

        if (!realRect.Overlaps(terrainRect))
            return;

        var needDrawRect = realRect;
        needDrawRect.xMin = Mathf.Max(realRect.xMin, terrainRect.xMin);
        needDrawRect.yMin = Mathf.Max(realRect.yMin, terrainRect.yMin);
        needDrawRect.xMax = Mathf.Min(realRect.xMax, terrainRect.xMax);
        needDrawRect.yMax = Mathf.Min(realRect.yMax, terrainRect.yMax);

        var scaleFactor = drawPos.width / realRect.width;
        var position = new Rect(drawPos.x + (needDrawRect.xMin - realRect.xMin) * scaleFactor,
            drawPos.y + (needDrawRect.yMin - realRect.yMin) * scaleFactor,
            needDrawRect.width * scaleFactor,
            needDrawRect.height * scaleFactor);
        var blendOffset = new Vector4(
            needDrawRect.width / terrainRect.width,
            needDrawRect.height / terrainRect.height,
            (needDrawRect.xMin - terrainRect.xMin) / terrainRect.width,
            (needDrawRect.yMin - terrainRect.yMin) / terrainRect.height);

        var l = position.x * 2.0f / _tiledTextureSize.x - 1;
        var r = (position.x + position.width) * 2.0f / _tiledTextureSize.x - 1;
        var b = position.y * 2.0f / _tiledTextureSize.y - 1;
        var t = (position.y + position.height) * 2.0f / _tiledTextureSize.y - 1;
        var mvpMatrix = Util.GetTileMatrix(l, r, b, t);

        Graphics.SetRenderTarget(_VTTileBuffer, _VTDepthBuffer);
        // drawTextureMaterial.SetMatrix(Shader.PropertyToID("_ImageMVP"), mvpMatrix);
        drawTextureMaterial.SetMatrix(Shader.PropertyToID("_ImageMVP"), GL.GetGPUProjectionMatrix(mvpMatrix, true));
        drawTextureMaterial.SetVector(BlendTile, blendOffset);

        var alphamap = terrain.terrainData.alphamapTextures[0];
        drawTextureMaterial.SetTexture(Blend, alphamap);

        var terrainData = terrain.terrainData;
        const float tileTexScale = 10.0f;
        var tileOffset = new Vector4(
            terrainData.size.x / tileTexScale * blendOffset.x,
            terrainData.size.z / tileTexScale * blendOffset.y,
            terrainData.size.x / tileTexScale * blendOffset.z,
            terrainData.size.z / tileTexScale * blendOffset.w);

        for (var layerIndex = 0; layerIndex < terrain.terrainData.terrainLayers.Length; layerIndex++)
        {
            var layer = terrainData.terrainLayers[layerIndex];
            drawTextureMaterial.SetVector($"_TileOffset{layerIndex + 1}", tileOffset);
            drawTextureMaterial.SetTexture($"_Diffuse{layerIndex + 1}", layer.diffuseTexture);
            drawTextureMaterial.SetTexture($"_Normal{layerIndex + 1}", layer.normalMapTexture);
        }

        if (decalInfo != null)
        {
            var decalScale = Mathf.Pow(2, decalInfo.mipLevel);
            Shader.SetGlobalVector(DecalOffset0, new Vector4(
                decalScale, decalScale, decalInfo.innerOffset.x, decalInfo.innerOffset.x
            ));
        }

        // active pass 0 or 1 of material
        drawTextureMaterial.SetPass(decal ? 1 : 0);
        // TODO: batching
        Graphics.DrawMeshNow(_quadMesh, Matrix4x4.identity);
    }

    public void ResetVT()
    {
        _tiledTexture.Reset();
        _VTTileBuffer = new RenderBuffer[2];
        _VTTileBuffer[0] = _tiledTexture.VTRTs[0].colorBuffer;
        _VTTileBuffer[1] = _tiledTexture.VTRTs[1].colorBuffer;
        _VTDepthBuffer = _tiledTexture.VTRTs[0].depthBuffer;
        _tiledTextureSize = new Vector2Int(_tiledTexture.VTRTs[0].width, _tiledTexture.VTRTs[0].height);
        _pageTable.Reset();
    }
}