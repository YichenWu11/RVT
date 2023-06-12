public class RenderTextureRequest
{
    public RenderTextureRequest(int x, int y, int mip)
    {
        PageX = x;
        PageY = y;
        MipLevel = mip;
    }

    // 页表 X 坐标
    public int PageX { get; }

    // 页表 Y 坐标
    public int PageY { get; }

    // mipmap 等级
    public int MipLevel { get; }
}