using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DecalRenderer : MonoBehaviour
{
    public class DecalInfo
    {
        public Vector2Int tileIndex; // tile 坐标
        public Vector2Int terrainTileIndex; // terrain tile 坐标
        public Vector2 innerOffset; // tile 内部偏移
        public int mipLevel;
    }

    public Rect regionRect;
    public bool EnableDecalRender = false;

    private Camera _mainCamera;

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
    }

    private void Update()
    {
        if (EnableDecalRender && Input.GetMouseButtonDown(0))
        {
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var hitPos = new Vector2(hit.point.x, hit.point.z);
                var info = CalculateDecalInfo(hitPos);

                // _rvt.ResetVT();

                var uv = hitPos / regionRect.size;
                var pageUV = new Vector2Int(
                    Mathf.FloorToInt(uv.x * _pageTable.TableSize),
                    Mathf.FloorToInt(uv.y * _pageTable.TableSize));
                _rvt.DrawDecalToTiledTexture(
                    new RectInt(
                        info.tileIndex.x * _tiledTexture.TileSizeWithBound,
                        info.tileIndex.y * _tiledTexture.TileSizeWithBound,
                        _tiledTexture.TileSizeWithBound,
                        _tiledTexture.TileSizeWithBound),
                    new RenderRequest(pageUV.x, pageUV.y, info.mipLevel), info);

                // Debug.Log(
                // $"hit {hit.collider.name} at ({info.tileIndex.x},{info.tileIndex.y}) offset({info.innerOffset.x} {info.innerOffset.y})");
            }
        }
    }

    private void DrawDecalToTiledTexture(RectInt drawPos)
    {
        throw new NotImplementedException();
    }

    private DecalInfo CalculateDecalInfo(Vector2 hitPos)
    {
        var uv = hitPos / regionRect.size;
        _lutInfo = _pageTable._lookupTexture.GetPixel(
            Mathf.FloorToInt(uv.x * _pageTable._lookupTexture.width),
            Mathf.FloorToInt(uv.y * _pageTable._lookupTexture.height));
        // Debug.Log(
        // $"uv : ({Mathf.FloorToInt(uv.x * _pageTable._lookupTexture.width)}, {Mathf.FloorToInt(uv.y * _pageTable._lookupTexture.height)})");

        // Debug.Log(
        // $"lut info : ({Mathf.FloorToInt(_lutInfo.r * 255.0f)},{Mathf.FloorToInt(_lutInfo.g * 255.0f)},{Mathf.FloorToInt(_lutInfo.b * 255.0f)})");

        return new DecalInfo()
        {
            tileIndex = new Vector2Int(
                Mathf.FloorToInt(_lutInfo.r * 255.0f),
                Mathf.FloorToInt(_lutInfo.g * 255.0f)),
            terrainTileIndex = new Vector2Int(
                Mathf.FloorToInt(uv.x * _pageTable._lookupTexture.width),
                Mathf.FloorToInt(uv.y * _pageTable._lookupTexture.height)),
            // terrainTileIndex = new Vector2(
            //     uv.x * _pageTable._lookupTexture.width,
            //     uv.y * _pageTable._lookupTexture.height),
            innerOffset = new Vector2(
                Util.Frac(uv.x * _pageTable.TableSize),
                Util.Frac(uv.y * _pageTable.TableSize)),
            mipLevel = Mathf.FloorToInt(_lutInfo.b * 255.0f)
        };
    }
}