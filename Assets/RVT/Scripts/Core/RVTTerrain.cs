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
        // _tiledTexture.DrawTexture += DrawTexture; // 画 Tile 的事件

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
        _feedbackReader.ReadbackRequest(_feedbackRenderer.TargetTexture);
    }

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