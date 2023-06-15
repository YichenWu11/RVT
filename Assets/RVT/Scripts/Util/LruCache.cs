public class LruCache
{
    private NodeInfo[] allNodes;
    private NodeInfo head;
    private NodeInfo tail;

    public int First => head.id;

    public void Init(int count)
    {
        allNodes = new NodeInfo[count];
        for (var i = 0; i < count; i++)
            allNodes[i] = new NodeInfo
            {
                id = i
            };
        for (var i = 0; i < count; i++)
        {
            allNodes[i].Next = i + 1 < count ? allNodes[i + 1] : null;
            allNodes[i].Prev = i != 0 ? allNodes[i - 1] : null;
        }

        head = allNodes[0];
        tail = allNodes[count - 1];
    }

    public bool SetActive(int id)
    {
        if (id < 0 || id >= allNodes.Length)
            return false;

        var node = allNodes[id];
        if (node == tail) return true;

        Remove(node);
        AddLast(node);
        return true;
    }

    private void AddLast(NodeInfo node)
    {
        var lastTail = tail;
        lastTail.Next = node;
        tail = node;
        node.Prev = lastTail;
    }

    private void Remove(NodeInfo node)
    {
        if (head == node)
        {
            head = node.Next;
        }
        else
        {
            node.Prev.Next = node.Next;
            node.Next.Prev = node.Prev;
        }
    }

    public class NodeInfo
    {
        public int id;
        public NodeInfo Next { get; set; }
        public NodeInfo Prev { get; set; }
    }
}