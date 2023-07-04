using System;
using System.Collections.Generic;

public class RenderRequest
{
    public RenderRequest(int x, int y, int mip)
    {
        PageX = x;
        PageY = y;
        MipLevel = mip;
    }

    // 页表 x 坐标
    public int PageX { get; }

    // 页表 y 坐标
    public int PageY { get; }

    // mipmap 等级
    public int MipLevel { get; }
}

public class RenderTask
{
    // 每帧处理数量限制
    private readonly int _limit = 10;

    // 等待处理的请求
    private readonly List<RenderRequest> _pendingRequests = new();

    // 开始渲染的事件
    public event Action<RenderRequest> StartRenderTask;

    public void Update()
    {
        if (_pendingRequests.Count <= 0)
            return;

        _pendingRequests.Sort((lhs, rhs) => -lhs.MipLevel.CompareTo(rhs.MipLevel));

        var count = _limit;
        while (count > 0 && _pendingRequests.Count > 0)
        {
            count--;
            var request = _pendingRequests[0];
            _pendingRequests.RemoveAt(0);

            // 开始渲染
            StartRenderTask?.Invoke(request);
        }
    }

    // 渲染请求
    public RenderRequest Request(int x, int y, int mip)
    {
        // 是否已经在请求队列中
        foreach (var r in _pendingRequests)
            if (r.PageX == x && r.PageY == y && r.MipLevel == mip)
                return null;

        var request = new RenderRequest(x, y, mip);
        _pendingRequests.Add(request);

        return request;
    }
}