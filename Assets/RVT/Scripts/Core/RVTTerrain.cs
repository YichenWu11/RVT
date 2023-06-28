using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class RVTTerrain : MonoBehaviour
{
    private static readonly int VTRegionRect = Shader.PropertyToID("_VTRegionRect");
    private static readonly int BlendTile = Shader.PropertyToID("_BlendTile");
    private static readonly int Blend = Shader.PropertyToID("_Blend");

    // RVT Settings
    [Header("RVT Settings")] public bool EnableRVTUpdate = true;
    public bool EnableUseRVTLit = true;

    [Space] public List<Terrain> TerrainList = new();

    // Terrain Region 占据的 Rect
    public Rect regionRect = new(0, 0, 1024, 1024);

    // 贴图绘制材质
    public Material DrawTextureMaterial;

    private readonly int _feedbackInterval = 8;

    private readonly RenderTask _renderTask = new();

    // Feedback Pass Renderer & Reader
    private FeedbackReader _feedbackReader;
    private FeedbackRenderer _feedbackRenderer;

    // helper mesh
    private Mesh _quadMesh;

    // TiledTexture
    private TiledTexture _tiledTexture;

    // TiledTexture 尺寸
    private Vector2Int _tiledTextureSize;

    // 可视距离
    private float _viewDistance;

    // 页表
    private PageTable PageTable;

    // From TiledTexture
    private RenderBuffer VTDepthBuffer;
    private RenderBuffer[] VTTileBuffer;

    private void Start()
    {
        PageTable = GetComponent<PageTable>();
        _feedbackRenderer = GetComponent<FeedbackRenderer>();
        _feedbackReader = GetComponent<FeedbackReader>();
        _tiledTexture = GetComponent<TiledTexture>();

        Shader.SetGlobalVector(
            VTRegionRect,
            new Vector4(regionRect.xMin, regionRect.yMin, regionRect.width, regionRect.height));

        _tiledTexture.Init();
        _tiledTexture.DrawTexture += DrawTiledTexture;

        PageTable.Init(_renderTask);

        _quadMesh = Util.BuildQuadMesh();

        VTTileBuffer = new RenderBuffer[2];
        VTTileBuffer[0] = _tiledTexture.VTRTs[0].colorBuffer;
        VTTileBuffer[1] = _tiledTexture.VTRTs[1].colorBuffer;
        VTDepthBuffer = _tiledTexture.VTRTs[0].depthBuffer;
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
        var x = request.PageX;
        var y = request.PageY;
        var perCellSize = (int)Mathf.Pow(2, request.MipLevel);

        // 转换到对应 Mip 层级页表上格子坐标
        x -= x % perCellSize;
        y -= y % perCellSize;

        var boundOffset =
            (float)_tiledTexture.BoundSize / _tiledTexture.TileSize * perCellSize *
            (regionRect.width / PageTable.TableSize);

        var realRect = new Rect(
            regionRect.xMin + (float)x / PageTable.TableSize * regionRect.width - boundOffset,
            regionRect.yMin + (float)y / PageTable.TableSize * regionRect.height - boundOffset,
            regionRect.width / PageTable.TableSize * perCellSize + 2.0f * boundOffset,
            regionRect.width / PageTable.TableSize * perCellSize + 2.0f * boundOffset);

        foreach (var terrain in TerrainList)
        {
            var terrainRect = Rect.zero;
            terrainRect.xMin = terrain.transform.position.x;
            terrainRect.yMin = terrain.transform.position.z;
            terrainRect.width = terrain.terrainData.size.x;
            terrainRect.height = terrain.terrainData.size.z;

            if (!realRect.Overlaps(terrainRect))
                continue;

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
            var scaleOffset = new Vector4(
                needDrawRect.width / terrainRect.width,
                needDrawRect.height / terrainRect.height,
                (needDrawRect.xMin - terrainRect.xMin) / terrainRect.width,
                (needDrawRect.yMin - terrainRect.yMin) / terrainRect.height);

            /*
             * Unity 中的矩阵是列主序的；即，变换矩阵的位置在最后一列中， 前三列包含 x、y 和 z 轴。数据访问方式如下： 行 + (列*4)
             */

            var l = position.x * 2.0f / _tiledTextureSize.x - 1;
            var r = (position.x + position.width) * 2.0f / _tiledTextureSize.x - 1;
            var b = position.y * 2.0f / _tiledTextureSize.y - 1;
            var t = (position.y + position.height) * 2.0f / _tiledTextureSize.y - 1;
            var mvpMatrix = new Matrix4x4
            {
                m00 = r - l,
                m03 = l,
                m11 = t - b,
                m13 = b,
                m23 = -1,
                m33 = 1
            };

            Graphics.SetRenderTarget(VTTileBuffer, VTDepthBuffer);
            // DrawTextureMaterial.SetMatrix(Shader.PropertyToID("_ImageMVP"), mvpMatrix);
            DrawTextureMaterial.SetMatrix(Shader.PropertyToID("_ImageMVP"), GL.GetGPUProjectionMatrix(mvpMatrix, true));
            DrawTextureMaterial.SetVector(BlendTile, scaleOffset);

            var alphamap = terrain.terrainData.alphamapTextures[0];
            DrawTextureMaterial.SetTexture(Blend, alphamap);
            for (var layerIndex = 0; layerIndex < terrain.terrainData.terrainLayers.Length; layerIndex++)
            {
                var terrainData = terrain.terrainData;
                var layer = terrainData.terrainLayers[layerIndex];
                var curScale = new Vector2(
                    terrainData.size.x / layer.tileSize.x,
                    terrainData.size.z / layer.tileSize.y);
                var tileOffset = new Vector4(
                    curScale.x * scaleOffset.x,
                    curScale.y * scaleOffset.y,
                    curScale.x * scaleOffset.z,
                    curScale.y * scaleOffset.w);
                DrawTextureMaterial.SetVector($"_TileOffset{layerIndex + 1}", tileOffset);
                DrawTextureMaterial.SetTexture($"_Diffuse{layerIndex + 1}", layer.diffuseTexture);
                DrawTextureMaterial.SetTexture($"_Normal{layerIndex + 1}", layer.normalMapTexture);
            }

            // active pass 0 of material
            DrawTextureMaterial.SetPass(0);
            Graphics.DrawMeshNow(_quadMesh, Matrix4x4.identity);
        }
    }
}