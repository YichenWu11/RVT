using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RVTTerrain : MonoBehaviour
{
    private static readonly int VTRealRect = Shader.PropertyToID("_VTRealRect");
    private static readonly int BlendTile = Shader.PropertyToID("_BlendTile");
    private static readonly int Blend = Shader.PropertyToID("_Blend");

    public bool UseFeed = true;
    public float Radius = 500;

    public ScaleFactor ChangeViewDis = ScaleFactor.Eighth;
    public List<Terrain> TerrainList = new();
    public Texture2D defaultNormal;

    // 贴图绘制材质
    public Material m_DrawTextureMateral;

    [HideInInspector] public PageTable PageTable;

    private float changeViewDis;
    private FeedbackReader feedbackReader;
    private FeedbackRenderer feedbackRender;
    private RenderBuffer mDepthBuffer;
    private Mesh mQuad;
    private RenderBuffer[] mVTTileBuffer;
    private Rect realTotalRect;
    private RenderTextureJob rtJob;
    private TiledTexture tiledTex;
    private Vector2Int tileTexSize;

    public Rect RealTotalRect
    {
        get => realTotalRect;
        set
        {
            realTotalRect = value;

            Shader.SetGlobalVector(
                VTRealRect,
                new Vector4(realTotalRect.xMin, realTotalRect.yMin, realTotalRect.width, realTotalRect.height));
        }
    }

    public float CellSize => 2 * Radius / PageTable.TableSize;

    private void Start()
    {
        PageTable = GetComponent<PageTable>();
        PageTable.UseFeed = UseFeed;
        changeViewDis = ChangeViewDis.ToFloat() * 2 * Radius;
        var fixedCenter = GetFixedCenter(GetFixedPos(transform.position));
        RealTotalRect = new Rect(fixedCenter.x - Radius, fixedCenter.y - Radius, 2 * Radius, 2 * Radius);
        rtJob = new RenderTextureJob();
        tiledTex = GetComponent<TiledTexture>();
        PageTable.Init(rtJob, tiledTex.RegionSize.x * tiledTex.RegionSize.y);
        tiledTex.DoDrawTexture += DrawTexture;
        feedbackRender = GetComponent<FeedbackRenderer>();
        feedbackReader = GetComponent<FeedbackReader>();
        InitializeQuadMesh();

        tiledTex.Init();
        mVTTileBuffer = new RenderBuffer[2];
        mVTTileBuffer[0] = tiledTex.VTRTs[0].colorBuffer;
        mVTTileBuffer[1] = tiledTex.VTRTs[1].colorBuffer;
        mDepthBuffer = tiledTex.VTRTs[0].depthBuffer;
        tileTexSize = new Vector2Int(tiledTex.VTRTs[0].width, tiledTex.VTRTs[0].height);
    }

    private void Update()
    {
        var fixedPos = GetFixedPos(transform.position);
        var xDiff = fixedPos.x - RealTotalRect.center.x;
        var yDiff = fixedPos.y - RealTotalRect.center.y;
        if (Mathf.Abs(xDiff) > changeViewDis || Mathf.Abs(yDiff) > changeViewDis)
        {
            var fixedCenter = GetFixedCenter(fixedPos);
            if (fixedCenter != RealTotalRect.center)
            {
                rtJob.ClearJob();
                var oldCenter = new Vector2Int((int)RealTotalRect.center.x,
                    (int)RealTotalRect.center.y);
                RealTotalRect = new Rect(fixedCenter.x - Radius, fixedCenter.y - Radius, 2 * Radius, 2 * Radius);
                PageTable.ChangeViewRect((fixedCenter - oldCenter) / (2 * (int)Radius / PageTable.TableSize));
                if (UseFeed)
                {
                    feedbackRender.FeedbackCamera.Render();
                    feedbackReader.NewRequest(feedbackRender.TargetTexture, true);
                    feedbackReader.UpdateRequest();
                    rtJob.Update();
                    feedbackReader.UpdateRequest();
                }
                else
                {
                    PageTable.UpdatePage(GetPageSector(fixedPos, RealTotalRect));
                    rtJob.Update();
                    PageTable.UpdatePage(GetPageSector(fixedPos, RealTotalRect));
                }

                return;
            }
        }

        if (UseFeed)
        {
            feedbackReader.UpdateRequest();
            if (feedbackReader.CanRead)
            {
                feedbackRender.FeedbackCamera.Render();
                feedbackReader.NewRequest(feedbackRender.TargetTexture);
            }
        }
        else
        {
            PageTable.UpdatePage(GetPageSector(fixedPos, RealTotalRect));
        }

        rtJob.Update();
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

    private void DrawTexture(RectInt drawPos, RenderTextureRequest request)
    {
        var x = request.PageX;
        var y = request.PageY;
        var perSize = (int)Mathf.Pow(2, request.MipLevel);
        x = x - x % perSize;
        y = y - y % perSize;
        var tableSize = PageTable.TableSize;
        var paddingEffect = tiledTex.PaddingSize * perSize * (RealTotalRect.width / tableSize) / tiledTex.TileSize;
        var realRect = new Rect(RealTotalRect.xMin + (float)x / tableSize * RealTotalRect.width - paddingEffect,
            RealTotalRect.yMin + (float)y / tableSize * RealTotalRect.height - paddingEffect,
            RealTotalRect.width / tableSize * perSize + 2f * paddingEffect,
            RealTotalRect.width / tableSize * perSize + 2f * paddingEffect);
        var terRect = Rect.zero;
        foreach (var ter in TerrainList)
        {
            if (!ter.isActiveAndEnabled) continue;
            terRect.xMin = ter.transform.position.x;
            terRect.yMin = ter.transform.position.z;
            terRect.width = ter.terrainData.size.x;
            terRect.height = ter.terrainData.size.z;
            if (!realRect.Overlaps(terRect)) continue;
            var needDrawRect = realRect;
            needDrawRect.xMin = Mathf.Max(realRect.xMin, terRect.xMin);
            needDrawRect.yMin = Mathf.Max(realRect.yMin, terRect.yMin);
            needDrawRect.xMax = Mathf.Min(realRect.xMax, terRect.xMax);
            needDrawRect.yMax = Mathf.Min(realRect.yMax, terRect.yMax);
            var scaleFactor = drawPos.width / realRect.width;
            var position = new Rect(drawPos.x + (needDrawRect.xMin - realRect.xMin) * scaleFactor,
                drawPos.y + (needDrawRect.yMin - realRect.yMin) * scaleFactor,
                needDrawRect.width * scaleFactor,
                needDrawRect.height * scaleFactor);
            var scaleOffset = new Vector4(
                needDrawRect.width / terRect.width,
                needDrawRect.height / terRect.height,
                (needDrawRect.xMin - terRect.xMin) / terRect.width,
                (needDrawRect.yMin - terRect.yMin) / terRect.height);
            // 构建变换矩阵
            var l = position.x * 2.0f / tileTexSize.x - 1;
            var r = (position.x + position.width) * 2.0f / tileTexSize.x - 1;
            var b = position.y * 2.0f / tileTexSize.y - 1;
            var t = (position.y + position.height) * 2.0f / tileTexSize.y - 1;
            var mat = new Matrix4x4();
            mat.m00 = r - l;
            mat.m03 = l;
            mat.m11 = t - b;
            mat.m13 = b;
            mat.m23 = -1;
            mat.m33 = 1;

            // 绘制贴图
            Graphics.SetRenderTarget(mVTTileBuffer, mDepthBuffer);
            m_DrawTextureMateral.SetMatrix(Shader.PropertyToID("_ImageMVP"), GL.GetGPUProjectionMatrix(mat, true));
            m_DrawTextureMateral.SetVector(BlendTile, scaleOffset);
            var layerIndex = 0;
            foreach (var alphamap in ter.terrainData.alphamapTextures)
            {
                m_DrawTextureMateral.SetTexture(Blend, alphamap);
                var index = 1;
                for (; layerIndex < ter.terrainData.terrainLayers.Length && index <= 4; layerIndex++)
                {
                    var layer = ter.terrainData.terrainLayers[layerIndex];
                    var nowScale = new Vector2(ter.terrainData.size.x / layer.tileSize.x,
                        ter.terrainData.size.z / layer.tileSize.y);
                    var tileOffset = new Vector4(nowScale.x * scaleOffset.x,
                        nowScale.y * scaleOffset.y, scaleOffset.z * nowScale.x, scaleOffset.w * nowScale.y);
                    m_DrawTextureMateral.SetVector($"_TileOffset{index}", tileOffset);
                    m_DrawTextureMateral.SetTexture($"_Diffuse{index}", layer.diffuseTexture);
                    m_DrawTextureMateral.SetTexture($"_Normal{index}",
                        layer.normalMapTexture ? layer.normalMapTexture : defaultNormal);
                    index++;
                }

                var tempCB = new CommandBuffer();
                tempCB.DrawMesh(mQuad, Matrix4x4.identity, m_DrawTextureMateral, 0, layerIndex <= 4 ? 0 : 1);
                Graphics.ExecuteCommandBuffer(tempCB); // DEBUG
            }
        }
    }

    public void Rest()
    {
        tiledTex.Reset();
        mVTTileBuffer = new RenderBuffer[2];
        mVTTileBuffer[0] = tiledTex.VTRTs[0].colorBuffer;
        mVTTileBuffer[1] = tiledTex.VTRTs[1].colorBuffer;
        mDepthBuffer = tiledTex.VTRTs[0].depthBuffer;
        tileTexSize = new Vector2Int(tiledTex.VTRTs[0].width, tiledTex.VTRTs[0].height);
        PageTable.Reset();
    }

    private Vector2Int GetPageSector(Vector2 pos, Rect realRect)
    {
        var sector = new Vector2Int((int)pos.x, (int)pos.y) -
                     new Vector2Int((int)realRect.xMin, (int)realRect.yMin);
        sector.x = (int)(sector.x / CellSize);
        sector.y = (int)(sector.y / CellSize);
        return sector;
    }

    private Vector2Int GetFixedCenter(Vector2Int pos)
    {
        return new Vector2Int((int)Mathf.Floor(pos.x / changeViewDis + 0.5f) * (int)changeViewDis,
            (int)Mathf.Floor(pos.y / changeViewDis + 0.5f) * (int)changeViewDis);
    }

    private Vector2Int GetFixedPos(Vector2 pos)
    {
        return new Vector2Int((int)Mathf.Floor(pos.x / CellSize + 0.5f) * (int)CellSize,
            (int)Mathf.Floor(pos.y / CellSize + 0.5f) * (int)CellSize);
    }

    private Vector2Int GetFixedPos(Vector3 pos)
    {
        return new Vector2Int((int)Mathf.Floor(pos.x / CellSize + 0.5f) * (int)CellSize,
            (int)Mathf.Floor(pos.z / CellSize + 0.5f) * (int)CellSize);
    }
}