using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RVTTerrain : MonoBehaviour
{
    private static readonly int VTRegionRect = Shader.PropertyToID("_VTRegionRect"); // 可视 Rect
    private static readonly int BlendTile = Shader.PropertyToID("_BlendTile");
    private static readonly int Blend = Shader.PropertyToID("_Blend");

    public List<Terrain> TerrainList = new();

    public int TotalWidth = 1024; // 区域总宽度
    public int TotalLength = 1024; // 区域总长度
    public Vector2 LeftDownCorner = new(0, 0); // 区域左下角世界坐标

    // 贴图绘制材质
    public Material DrawTextureMaterial;

    // 页表
    [HideInInspector] public PageTable PageTable;

    private readonly RenderTask _renderTask = new();

    // Feedback Pass Renderer & Reader
    private FeedbackReader _feedbackReader;
    private FeedbackRenderer _feedbackRenderer;

    // helper mesh
    private Mesh _quadMesh;

    // Terrain Region 占据的 Rect
    private Rect _regionRect;

    // TiledTexture
    private TiledTexture _tiledTexture;

    // TiledTexture 尺寸
    private Vector2Int _tiledTextureSize;

    // 可视距离
    private float _viewDistance;

    private bool isDraw;

    // From TiledTexture
    private RenderBuffer VTDepthBuffer;
    private RenderBuffer[] VTTileBuffer;

    private void Start()
    {
        PageTable = GetComponent<PageTable>();
        _feedbackRenderer = GetComponent<FeedbackRenderer>();
        _feedbackReader = GetComponent<FeedbackReader>();
        _tiledTexture = GetComponent<TiledTexture>();

        _regionRect = new Rect(LeftDownCorner.x, LeftDownCorner.y, TotalWidth, TotalLength);
        Shader.SetGlobalVector(
            VTRegionRect,
            new Vector4(_regionRect.xMin, _regionRect.yMin, _regionRect.width, _regionRect.height));

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
        _feedbackReader.UpdateRequest();
        if (_feedbackReader.CanRead)
        {
            _feedbackRenderer.FeedbackCamera.Render();
            _feedbackReader.ReadbackRequest(_feedbackRenderer.TargetTexture);
        }

        _renderTask.Update();
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
            _tiledTexture.BoundSize * perCellSize * (_regionRect.width / PageTable.TableSize) /
            _tiledTexture.TileSize;
        var realRect = new Rect(
            _regionRect.xMin + (float)x / PageTable.TableSize * _regionRect.width - boundOffset,
            _regionRect.yMin + (float)y / PageTable.TableSize * _regionRect.height - boundOffset,
            _regionRect.width / PageTable.TableSize * perCellSize + 2.0f * boundOffset,
            _regionRect.width / PageTable.TableSize * perCellSize + 2.0f * boundOffset);
        var terrainRect = Rect.zero;

        foreach (var terrain in TerrainList)
        {
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

            var cmd = new CommandBuffer();
            cmd.DrawMesh(_quadMesh, Matrix4x4.identity, DrawTextureMaterial, 0);
            Graphics.ExecuteCommandBuffer(cmd);
        }
    }
}