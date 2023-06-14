using System.Collections.Generic;
using UnityEngine;

public class RVTTerrain : MonoBehaviour
{
    // Shader 参数 ID
    private static readonly int VTRealRect = Shader.PropertyToID("_VTRealRect"); // 可视 Rect
    private static readonly int BlendTile = Shader.PropertyToID("_BlendTile");
    private static readonly int Blend = Shader.PropertyToID("_Blend");

    // 可视区域半径
    public float ViewRadius = 500;

    // Terrain 列表
    public List<Terrain> TerrainList = new();

    // 贴图绘制材质
    public Material DrawTextureMaterial;

    [HideInInspector] public PageTable PageTable;

    // From TiledTexture
    private RenderBuffer _depthBuffer;

    // Feedback Pass Renderer & Reader
    private FeedbackReader _feedbackReader;
    private FeedbackRenderer _feedbackRenderer;
    private Mesh _quadMesh;

    // 可视区域 Rect
    private Rect _realTotalRect;

    private RenderTask _renderTask;

    // TiledTexture
    private TiledTexture _tiledTexture;

    // TiledTexture 尺寸
    private Vector2Int _tiledTextureSize;

    // 可视距离
    private float _viewDistance;
    private RenderBuffer[] VTTileBuffer;


    // 设置可视 Rect & 设置 Shader _VTRealRect 参数
    public Rect RealTotalRect
    {
        get => _realTotalRect;
        set
        {
            _realTotalRect = value;
            Shader.SetGlobalVector(
                VTRealRect,
                new Vector4(_realTotalRect.xMin, _realTotalRect.yMin, _realTotalRect.width, _realTotalRect.height));
        }
    }

    private float CellSize => 2 * ViewRadius / PageTable.TableSize;

    private void Start()
    {
        PageTable = GetComponent<PageTable>();
        _feedbackRenderer = GetComponent<FeedbackRenderer>();
        _feedbackReader = GetComponent<FeedbackReader>();

        var fixedRectCenter = GetFixedCenter(GetFixedPos(transform.position));
        RealTotalRect = new Rect(fixedRectCenter.x - ViewRadius, fixedRectCenter.y - ViewRadius, 2 * ViewRadius,
            2 * ViewRadius);

        _renderTask = new RenderTask();

        _tiledTexture = GetComponent<TiledTexture>();
        _tiledTexture.Init();
        _tiledTexture.DrawTexture += DrawTexture; // 画 Tile 的事件

        PageTable.Init(_renderTask, _tiledTexture.RegionSize.x * _tiledTexture.RegionSize.y);

        _quadMesh = Util.BuildQuadMesh();

        VTTileBuffer = new RenderBuffer[2];
        VTTileBuffer[0] = _tiledTexture.VTRTs[0].colorBuffer;
        VTTileBuffer[1] = _tiledTexture.VTRTs[1].colorBuffer;
        _depthBuffer = _tiledTexture.VTRTs[0].depthBuffer;
        _tiledTextureSize = new Vector2Int(_tiledTexture.VTRTs[0].width, _tiledTexture.VTRTs[0].height);
    }

    private void Update()
    {
        var fixedPos = GetFixedPos(transform.position);
        // Debug.Log($"RealTotalRect:{RealTotalRect.center.x},{RealTotalRect.center.y}");
        // Debug.Log($"fixedPos:{fixedPos.x},{fixedPos.y}");
        var xDistance = fixedPos.x - RealTotalRect.center.x;
        var yDistance = fixedPos.y - RealTotalRect.center.y;

        // 需要调整 Rect
        if (Mathf.Abs(xDistance) > _viewDistance || Mathf.Abs(yDistance) > _viewDistance)
        {
            var fixedCenter = GetFixedCenter(fixedPos);
            if (fixedCenter != RealTotalRect.center)
            {
                _renderTask.Clear();
                var oldCenter = new Vector2Int(
                    (int)RealTotalRect.center.x,
                    (int)RealTotalRect.center.y);
                RealTotalRect = new Rect(
                    fixedCenter.x - ViewRadius,
                    fixedCenter.y - ViewRadius,
                    2 * ViewRadius,
                    2 * ViewRadius);
                PageTable.ChangeViewRect((fixedCenter - oldCenter) / (2 * (int)ViewRadius / PageTable.TableSize));

                return;
            }
        }

        _feedbackReader.UpdateRequest();
        _feedbackRenderer.FeedbackCamera.Render();
        if (_feedbackReader.CanRead) _feedbackReader.ReadbackRequest(_feedbackRenderer.TargetTexture);
        // PageTable.UpdatePage(GetPageSector(fixedPos, RealTotalRect));

        _renderTask.Update();
    }

    private void DrawTexture(RectInt drawPos, RenderTextureRequest request)
    {
        // var x = request.PageX;
        // var y = request.PageY;
        // var perSize = (int)Mathf.Pow(2, request.MipLevel);
        // x -= x % perSize;
        // y -= y % perSize;
        // var tableSize = PageTable.TableSize;
        // var paddingEffect = _tiledTexture.PaddingSize * perSize * (RealTotalRect.width / tableSize) /
        //                     _tiledTexture.TileSize;
        // var realRect = new Rect(RealTotalRect.xMin + (float)x / tableSize * RealTotalRect.width - paddingEffect,
        //     RealTotalRect.yMin + (float)y / tableSize * RealTotalRect.height - paddingEffect,
        //     RealTotalRect.width / tableSize * perSize + 2f * paddingEffect,
        //     RealTotalRect.width / tableSize * perSize + 2f * paddingEffect);
        // var terRect = Rect.zero;
        // foreach (var ter in TerrainList)
        // {
        //     if (!ter.isActiveAndEnabled) continue;
        //     terRect.xMin = ter.transform.position.x;
        //     terRect.yMin = ter.transform.position.z;
        //     terRect.width = ter.terrainData.size.x;
        //     terRect.height = ter.terrainData.size.z;
        //     if (!realRect.Overlaps(terRect)) continue;
        //     var needDrawRect = realRect;
        //     needDrawRect.xMin = Mathf.Max(realRect.xMin, terRect.xMin);
        //     needDrawRect.yMin = Mathf.Max(realRect.yMin, terRect.yMin);
        //     needDrawRect.xMax = Mathf.Min(realRect.xMax, terRect.xMax);
        //     needDrawRect.yMax = Mathf.Min(realRect.yMax, terRect.yMax);
        //     var scaleFactor = drawPos.width / realRect.width;
        //     var position = new Rect(drawPos.x + (needDrawRect.xMin - realRect.xMin) * scaleFactor,
        //         drawPos.y + (needDrawRect.yMin - realRect.yMin) * scaleFactor,
        //         needDrawRect.width * scaleFactor,
        //         needDrawRect.height * scaleFactor);
        //     var scaleOffset = new Vector4(
        //         needDrawRect.width / terRect.width,
        //         needDrawRect.height / terRect.height,
        //         (needDrawRect.xMin - terRect.xMin) / terRect.width,
        //         (needDrawRect.yMin - terRect.yMin) / terRect.height);
        //     // 构建变换矩阵
        //     var l = position.x * 2.0f / _tiledTextureSize.x - 1;
        //     var r = (position.x + position.width) * 2.0f / _tiledTextureSize.x - 1;
        //     var b = position.y * 2.0f / _tiledTextureSize.y - 1;
        //     var t = (position.y + position.height) * 2.0f / _tiledTextureSize.y - 1;
        //     var mat = new Matrix4x4();
        //     mat.m00 = r - l;
        //     mat.m03 = l;
        //     mat.m11 = t - b;
        //     mat.m13 = b;
        //     mat.m23 = -1;
        //     mat.m33 = 1;
        //
        //     // 绘制贴图
        //     Graphics.SetRenderTarget(VTTileBuffer, _depthBuffer);
        //     DrawTextureMaterial.SetMatrix(Shader.PropertyToID("_ImageMVP"), GL.GetGPUProjectionMatrix(mat, true));
        //     DrawTextureMaterial.SetVector(BlendTile, scaleOffset);
        //     var layerIndex = 0;
        //     foreach (var alphamap in ter.terrainData.alphamapTextures)
        //     {
        //         DrawTextureMaterial.SetTexture(Blend, alphamap);
        //         var index = 1;
        //         for (; layerIndex < ter.terrainData.terrainLayers.Length && index <= 4; layerIndex++)
        //         {
        //             var layer = ter.terrainData.terrainLayers[layerIndex];
        //             var nowScale = new Vector2(ter.terrainData.size.x / layer.tileSize.x,
        //                 ter.terrainData.size.z / layer.tileSize.y);
        //             var tileOffset = new Vector4(nowScale.x * scaleOffset.x,
        //                 nowScale.y * scaleOffset.y, scaleOffset.z * nowScale.x, scaleOffset.w * nowScale.y);
        //             DrawTextureMaterial.SetVector($"_TileOffset{index}", tileOffset);
        //             DrawTextureMaterial.SetTexture($"_Diffuse{index}", layer.diffuseTexture);
        //             DrawTextureMaterial.SetTexture($"_Normal{index}", layer.normalMapTexture);
        //             index++;
        //         }
        //
        //         var tempCB = new CommandBuffer();
        //         tempCB.DrawMesh(_quadMesh, Matrix4x4.identity, DrawTextureMaterial, 0, layerIndex <= 4 ? 0 : 1);
        //         Graphics.ExecuteCommandBuffer(tempCB); // DEBUG
        //     }
        // }
    }

    // private Vector2Int GetPageSector(Vector2 pos, Rect realRect)
    // {
    //     var sector = new Vector2Int((int)pos.x, (int)pos.y) -
    //                  new Vector2Int((int)realRect.xMin, (int)realRect.yMin);
    //     sector.x = (int)(sector.x / CellSize);
    //     sector.y = (int)(sector.y / CellSize);
    //     return sector;
    // }

    private Vector2Int GetPageSector(Vector2 pos, Rect realRect)
    {
        var sector = new Vector2Int((int)pos.x, (int)pos.y) -
                     new Vector2Int((int)realRect.xMin, (int)realRect.yMin);
        sector.x = (int)(sector.x / CellSize);
        sector.y = (int)(sector.y / CellSize);
        return sector;
    }

    // 不加 0.5f
    private Vector2Int GetFixedCenter(Vector2Int pos)
    {
        return new Vector2Int(
            (int)Mathf.Floor(pos.x / _viewDistance) * (int)_viewDistance,
            (int)Mathf.Floor(pos.y / _viewDistance) * (int)_viewDistance);
    }

    private Vector2Int GetFixedPos(Vector3 pos)
    {
        return new Vector2Int(
            (int)Mathf.Floor(pos.x / CellSize) * (int)CellSize,
            (int)Mathf.Floor(pos.z / CellSize) * (int)CellSize);
    }
}