using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DecalRenderer : MonoBehaviour
{
    public class DecalInfo
    {
        public Vector2 innerOffset; // tile 内部偏移
        public Vector2Int tileIndex; // tile 坐标
    }

    public Rect regionRect;

    private List<DecalInfo> _decalInfos = new();
    private Camera _mainCamera;

    private Texture2D _feedbackTexture;

    private PageTable _pageTable;
    private TiledTexture _tiledTexture;
    private RVTTerrain _rvt;

    private Color _lutInfo;

    private void Start()
    {
        _mainCamera = Camera.main;
        _tiledTexture = GetComponent<TiledTexture>();
        _pageTable = GetComponent<PageTable>();
        _rvt = GetComponent<RVTTerrain>();

        GetComponent<FeedbackReader>().OnFeedbackReadComplete += KeepFeedbackInfo;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var info = CalculateDecalInfo(new Vector2(hit.point.x, hit.point.z));
                _decalInfos.Add(info);
                // _rvt.Reset();

                // Debug.Log(
                // $"hit {hit.collider.name} at ({info.tileIndex.x},{info.tileIndex.y}) offset({info.innerOffset.x} {info.innerOffset.y})");
            }
        }
    }

    public bool ShouldDrawDecal(Vector2Int pos)
    {
        var vec = new Vector2Int(
            pos.x / _tiledTexture.TileSizeWithBound,
            pos.y / _tiledTexture.TileSizeWithBound
        );

        return _decalInfos.Any(decal => decal.tileIndex.Equals(vec));
    }

    private void KeepFeedbackInfo(Texture2D texture)
    {
        _feedbackTexture = texture;
    }

    private void DrawDecalToTiledTexture(RectInt drawPos)
    {
        throw new NotImplementedException();
    }

    private DecalInfo CalculateDecalInfo(Vector2 hitPos)
    {
        var uv = hitPos / regionRect.size;
        _lutInfo = _feedbackTexture.GetPixel(Mathf.FloorToInt(uv.x), Mathf.FloorToInt(uv.y)).linear;

        Debug.Log(
            $"lut info : ({Mathf.FloorToInt(_lutInfo.r * 255.0f)},{Mathf.FloorToInt(_lutInfo.g * 255.0f)},{Mathf.FloorToInt(_lutInfo.b * 255.0f)})");

        return new DecalInfo()
        {
            tileIndex = new Vector2Int(
                Mathf.FloorToInt(uv.x * _pageTable.TableSize),
                Mathf.FloorToInt(uv.y * _pageTable.TableSize)),
            innerOffset = new Vector2(
                Util.Frac(uv.x * _pageTable.TableSize) * _pageTable.TableSize,
                Util.Frac(uv.y * _pageTable.TableSize) * _pageTable.TableSize
            )
        };
    }
}