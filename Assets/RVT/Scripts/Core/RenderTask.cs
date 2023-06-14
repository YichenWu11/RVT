using System;
using System.Collections.Generic;

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

public class RenderTask
{
    // 一帧最多处理几个
    private readonly int _limit = 2;

    // 等待处理的请求.
    private readonly List<RenderTextureRequest> _pendingRequests = new();

    // 渲染完成的事件回调.
    public event Action<RenderTextureRequest> StartRenderTask;

    // 渲染取消的事件回调.
    public event Action<RenderTextureRequest> CancelRenderTask;

    public void Update()
    {
        if (_pendingRequests.Count <= 0)
            return;

        // 优先处理 mipmap 等级高的请求
        _pendingRequests.Sort((x, y) => x.MipLevel.CompareTo(y.MipLevel));

        var count = _limit;
        while (count > 0 && _pendingRequests.Count > 0)
        {
            count--;
            // 将第一个请求从等待队列移到运行队列
            var req = _pendingRequests[_pendingRequests.Count - 1];
            _pendingRequests.RemoveAt(_pendingRequests.Count - 1);

            // 开始渲染
            StartRenderTask?.Invoke(req);
        }
    }

    // 新建渲染请求
    public RenderTextureRequest Request(int x, int y, int mip)
    {
        // 是否已经在请求队列中
        foreach (var r in _pendingRequests)
            if (r.PageX == x && r.PageY == y && r.MipLevel == mip)
                return null;

        // 加入待处理列表
        var request = new RenderTextureRequest(x, y, mip);
        _pendingRequests.Add(request);

        return request;
    }

    public void Clear()
    {
        foreach (var r in _pendingRequests) CancelRenderTask?.Invoke(r);

        _pendingRequests.Clear();
    }
}