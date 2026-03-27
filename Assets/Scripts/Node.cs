using System;

public class Node : IHeapItem<Node>
{
    public MapLocation location;
    public Node parent;

    public int gCost;
    public int hCost;
    public int fCost => gCost + hCost;

    int _heapIndex;
    public int heapIndex
    {
        get { return _heapIndex; }
        set { _heapIndex = value; }
    }

    public Node(MapLocation loc, Node parent, int g, int h)
    {
        location = loc;
        this.parent = parent;
        gCost = g;
        hCost = h;
    }

    public int CompareTo(Node other)
    {
        int compare = fCost.CompareTo(other.fCost);
        if (compare == 0)
            compare = hCost.CompareTo(other.hCost); // tiebreak: prefer closer to goal
        return -compare; // lower cost = higher priority
    }
}