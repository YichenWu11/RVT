using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal.Internal;

public class RVTTerrain : MonoBehaviour
{
    private static readonly int VTRegionRect = Shader.PropertyToID("_VTRegionRect");
    private static readonly int BlendTile = Shader.PropertyToID("_BlendTile");
    private static readonly int Blend = Shader.PropertyToID("_Blend");
    private static readonly int DecalOffset0 = Shader.PropertyToID("_DecalOffset0");
    private static readonly int TileAlbedo = Shader.PropertyToID("_TileAlbedo");
    private static readonly int TileNormal = Shader.PropertyToID("_TileNormal");

    // RVT Settings
    [Header("RVT Settings")] public bool EnableRVTUpdate = true;
    public bool EnableUseRVTLit = true;
    public bool EnableVTCompression = false;

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
    private Mesh _fullScreenQuadMesh;

    // TiledTexture
    private TiledTexture _tiledTexture;

    // TiledTexture 尺寸
    private Vector2Int _tiledTextureSize;

    // 页表
    private PageTable _pageTable;

    // From TiledTexture
    private RenderBuffer[] _VTTileBuffer;
    private RenderBuffer _VTDepthBuffer;

    // Tile
    [HideInInspector] public RenderTexture albedoTileRT;
    [HideInInspector] public RenderTexture normalTileRT;
    private RenderBuffer[] _tileBuffer;
    private RenderBuffer _depthBuffer;

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
        _fullScreenQuadMesh = Util.BuildFullScreenQuadMesh();

        albedoTileRT = new RenderTexture(_tiledTexture.TileSizeWithBound, _tiledTexture.TileSizeWithBound, 0)
        {
            useMipMap = false,
            wrapMode = TextureWrapMode.Clamp
        };
        albedoTileRT.Create();
        normalTileRT = new RenderTexture(_tiledTexture.TileSizeWithBound, _tiledTexture.TileSizeWithBound, 0)
        {
            useMipMap = false,
            wrapMode = TextureWrapMode.Clamp
        };
        normalTileRT.Create();

        _tileBuffer = new RenderBuffer[2];
        _tileBuffer[0] = albedoTileRT.colorBuffer;
        _tileBuffer[1] = normalTileRT.colorBuffer;
        _depthBuffer = albedoTileRT.depthBuffer;

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

    private int temp = 0;

    private void DrawTiledTexture(RectInt drawPos, RenderRequest request)
    {
        // if (temp > 10) return;
        DrawTiledTextureImplAno(drawPos, request);
        // temp++;
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
        // var x = request.PageX;
        // var y = request.PageY;
        // var perCellSize = (int)Mathf.Pow(2, request.MipLevel);
        //
        // // 转换到对应 Mip 层级页表上格子坐标
        // x -= x % perCellSize;
        // y -= y % perCellSize;
        //
        // var boundOffset =
        //     (float)_tiledTexture.BoundSize / _tiledTexture.TileSize * perCellSize *
        //     (regionRect.width / _pageTable.TableSize);
        //
        // var realRect = new Rect(
        //     regionRect.xMin + (float)x / _pageTable.TableSize * regionRect.width - boundOffset,
        //     regionRect.yMin + (float)y / _pageTable.TableSize * regionRect.height - boundOffset,
        //     regionRect.width / _pageTable.TableSize * perCellSize + 2.0f * boundOffset,
        //     regionRect.width / _pageTable.TableSize * perCellSize + 2.0f * boundOffset);
        //
        //
        // var terrainRect = Rect.zero;
        // terrainRect.xMin = terrain.transform.position.x;
        // terrainRect.yMin = terrain.transform.position.z;
        // terrainRect.width = terrain.terrainData.size.x;
        // terrainRect.height = terrain.terrainData.size.z;
        //
        // if (!realRect.Overlaps(terrainRect))
        //     return;
        //
        // var needDrawRect = realRect;
        // needDrawRect.xMin = Mathf.Max(realRect.xMin, terrainRect.xMin);
        // needDrawRect.yMin = Mathf.Max(realRect.yMin, terrainRect.yMin);
        // needDrawRect.xMax = Mathf.Min(realRect.xMax, terrainRect.xMax);
        // needDrawRect.yMax = Mathf.Min(realRect.yMax, terrainRect.yMax);
        //
        // var scaleFactor = drawPos.width / realRect.width;
        // var posRect = new Rect(drawPos.x,
        //     drawPos.y,
        //     needDrawRect.width * scaleFactor,
        //     needDrawRect.height * scaleFactor);
        // var blendOffset = new Vector4(
        //     needDrawRect.width / terrainRect.width,
        //     needDrawRect.height / terrainRect.height,
        //     (needDrawRect.xMin - terrainRect.xMin) / terrainRect.width,
        //     (needDrawRect.yMin - terrainRect.yMin) / terrainRect.height);
        //
        // var mvpMatrix = Util.GetTileMatrix(posRect, _tiledTextureSize);
        //
        // Graphics.SetRenderTarget(_VTTileBuffer, _VTDepthBuffer);
        // // drawTextureMaterial.SetMatrix(Shader.PropertyToID("_ImageMVP"), mvpMatrix);
        // drawTextureMaterial.SetMatrix(Shader.PropertyToID("_ImageMVP"), GL.GetGPUProjectionMatrix(mvpMatrix, true));
        // drawTextureMaterial.SetVector(BlendTile, blendOffset);
        //
        // var alphamap = terrain.terrainData.alphamapTextures[0];
        // drawTextureMaterial.SetTexture(Blend, alphamap);
        //
        // var terrainData = terrain.terrainData;
        // const float tileTexScale = 10.0f;
        // var tileOffset = new Vector4(
        //     terrainData.size.x / tileTexScale * blendOffset.x,
        //     terrainData.size.z / tileTexScale * blendOffset.y,
        //     terrainData.size.x / tileTexScale * blendOffset.z,
        //     terrainData.size.z / tileTexScale * blendOffset.w);
        //
        // for (var layerIndex = 0; layerIndex < terrain.terrainData.terrainLayers.Length; layerIndex++)
        // {
        //     var layer = terrainData.terrainLayers[layerIndex];
        //     drawTextureMaterial.SetVector($"_TileOffset{layerIndex + 1}", tileOffset);
        //     drawTextureMaterial.SetTexture($"_Diffuse{layerIndex + 1}", layer.diffuseTexture);
        //     drawTextureMaterial.SetTexture($"_Normal{layerIndex + 1}", layer.normalMapTexture);
        // }
        //
        // if (decalInfo != null)
        // {
        //     var decalScale = Mathf.Pow(2, decalInfo.mipLevel);
        //     Shader.SetGlobalVector(DecalOffset0, new Vector4(
        //         decalScale, decalScale, decalInfo.innerOffset.x, decalInfo.innerOffset.x
        //     ));
        // }
        //
        // // active pass 0 or 1 of material
        // drawTextureMaterial.SetPass(decal ? 1 : 0);
        // // TODO: batching
        // Graphics.DrawMeshNow(_quadMesh, Matrix4x4.identity);
    }

    private void DrawTiledTextureImplAno(
        RectInt drawPos,
        RenderRequest request,
        bool decal = false,
        DecalRenderer.DecalInfo decalInfo = null)
    {
        #region Render Single Tile

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
        var posRect = new Rect(drawPos.x,
            drawPos.y,
            needDrawRect.width * scaleFactor,
            needDrawRect.height * scaleFactor);
        var blendOffset = new Vector4(
            needDrawRect.width / terrainRect.width,
            needDrawRect.height / terrainRect.height,
            (needDrawRect.xMin - terrainRect.xMin) / terrainRect.width,
            (needDrawRect.yMin - terrainRect.yMin) / terrainRect.height);

        Graphics.SetRenderTarget(_tileBuffer, _depthBuffer);
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

        drawTextureMaterial.SetPass(2);
        Graphics.DrawMeshNow(_fullScreenQuadMesh, Matrix4x4.identity);

        #endregion

        #region Copy Tile To TiledTexture

        if (EnableVTCompression)
        {
            // compress
            var compressAlbedoTile = TextureCompressUtil.CompressRT2RT(_tiledTexture.compressShader, albedoTileRT);
            var compressNormalTile = TextureCompressUtil.CompressRT2RT(_tiledTexture.compressShader, normalTileRT);

            // copy 2 vt
            var tileX = drawPos.xMin / _tiledTexture.TileSizeWithBound;
            var tileY = drawPos.yMin / _tiledTexture.TileSizeWithBound;

            Graphics.CopyTexture(
                compressAlbedoTile, 0, 0, 0, 0, compressAlbedoTile.width, compressAlbedoTile.height,
                _tiledTexture.VTs[0], 0, 0,
                tileX * _tiledTexture.TileSizeWithBound, tileY * _tiledTexture.TileSizeWithBound);

            Graphics.CopyTexture(
                compressNormalTile, 0, 0, 0, 0, compressNormalTile.width, compressNormalTile.height,
                _tiledTexture.VTs[1], 0, 0,
                tileX * _tiledTexture.TileSizeWithBound, tileY * _tiledTexture.TileSizeWithBound);

            compressAlbedoTile.Release();
            compressNormalTile.Release();
        }
        else
        {
            // var tileX = drawPos.xMin / _tiledTexture.TileSizeWithBound;
            // var tileY = drawPos.yMin / _tiledTexture.TileSizeWithBound;
            //
            // Graphics.CopyTexture(
            //     albedoTileRT, 0, 0, 0, 0, albedoTileRT.width, albedoTileRT.height,
            //     _tiledTexture.VTRTs[0], 0, 0,
            //     tileX * _tiledTexture.TileSizeWithBound, tileY * _tiledTexture.TileSizeWithBound);
            //
            // Graphics.CopyTexture(
            //     normalTileRT, 0, 0, 0, 0, normalTileRT.width, normalTileRT.height,
            //     _tiledTexture.VTRTs[1], 0, 0,
            //     tileX * _tiledTexture.TileSizeWithBound, tileY * _tiledTexture.TileSizeWithBound);

            Graphics.SetRenderTarget(_VTTileBuffer, _VTDepthBuffer);

            Shader.SetGlobalTexture(TileAlbedo, albedoTileRT);
            Shader.SetGlobalTexture(TileNormal, normalTileRT);

            var mvpMatrix = Util.GetTileMatrix(posRect, _tiledTextureSize);
            drawTextureMaterial.SetMatrix(Shader.PropertyToID("_ImageMVP"), GL.GetGPUProjectionMatrix(mvpMatrix, true));

            drawTextureMaterial.SetPass(3);
            Graphics.DrawMeshNow(_quadMesh, Matrix4x4.identity);
        }

        #endregion
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