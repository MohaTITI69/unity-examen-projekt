using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder
{
    private Maze maze;

    public AStarPathfinder(Maze maze)
    {
        this.maze = maze;
    }

    public IEnumerator FindPathVisual(
        MapLocation start,
        MapLocation goal,
        GameObject[,] floorTiles,
        System.Action<List<MapLocation>> onComplete)
    {
        Node[,] nodeGrid = new Node[maze.width, maze.height];
        for (int x = 0; x < maze.width; x++)
            for (int z = 0; z < maze.height; z++)
                nodeGrid[x, z] = new Node(new MapLocation(x, z), null, 0, 0);

        Node startNode = nodeGrid[start.x, start.z];
        Node goalNode  = nodeGrid[goal.x, goal.z];

        int maxSize = maze.width * maze.height;
        Heap<Node> openSet      = new Heap<Node>(maxSize);
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(start, goal);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node current = openSet.RemoveFirst();
            closedSet.Add(current);

            // Color closed = red
            ColorTile(floorTiles, current.location, Color.red);

            if (current == goalNode)
            {
                List<MapLocation> path = RetracePath(startNode, goalNode);
                onComplete(path);
                yield break;
            }

            foreach (var dir in maze.directions)
            {
                int nx = current.location.x + dir.x;
                int nz = current.location.z + dir.z;

                if (!IsWalkable(nx, nz)) continue;

                Node neighbour = nodeGrid[nx, nz];
                if (closedSet.Contains(neighbour)) continue;

                int newG = current.gCost + 1;

                if (newG < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost  = newG;
                    neighbour.hCost  = GetDistance(neighbour.location, goal);
                    neighbour.parent = current;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                        // Color open = blue
                        ColorTile(floorTiles, neighbour.location, Color.blue);
                    }
                    else
                        openSet.UpdateItem(neighbour);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogWarning("A*: No path found.");
        onComplete(null);
    }

    void ColorTile(GameObject[,] floorTiles, MapLocation loc, Color color)
    {
        if (floorTiles == null) return;
        GameObject tile = floorTiles[loc.x, loc.z];
        if (tile != null)
            tile.GetComponent<Renderer>().material.color = color;
    }

    List<MapLocation> RetracePath(Node startNode, Node endNode)
    {
        var path = new List<MapLocation>();
        Node current = endNode;
        while (current != startNode)
        {
            path.Add(current.location);
            current = current.parent;
        }
        path.Add(startNode.location);
        path.Reverse();
        return path;
    }

    int GetDistance(MapLocation a, MapLocation b)
    {
        int distX = Mathf.Abs(a.x - b.x);
        int distZ = Mathf.Abs(a.z - b.z);
        return 10 * (distX + distZ);
    }

    bool IsWalkable(int x, int z)
    {
        if (x < 0 || x >= maze.width || z < 0 || z >= maze.height) return false;
        return maze.map[x, z] == 0;
    }
}