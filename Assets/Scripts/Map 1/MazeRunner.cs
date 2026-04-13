using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeRunner : MonoBehaviour
{
    private Maze maze;
    private GameObject seeker;
    private GameObject target;
    private LineRenderer lineRenderer;
    private GameObject[,] floorTiles;

    void Start()
    {
        maze = GetComponent<Maze>();
        Invoke(nameof(Setup), 0.1f);
    }

    void Setup()
    {
        // Spawn floor tiles on all walkable cells so we can color them
        floorTiles = new GameObject[maze.width, maze.height];
        float offsetX = (maze.width * maze.scale) / 2f;
        float offsetZ = (maze.height * maze.scale) / 2f;

        for (int z = 0; z < maze.height; z++)
        {
            for (int x = 0; x < maze.width; x++)
            {
                if (maze.map[x, z] == 0)
                {
                    Vector3 pos = new Vector3(x * maze.scale - offsetX, -maze.scale / 2f + 0.1f, z * maze.scale - offsetZ);
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_{x}_{z}";
                    tile.transform.position = pos;
                    tile.transform.localScale = new Vector3(maze.scale, 0.1f, maze.scale);
                    tile.GetComponent<Renderer>().material.color = Color.white;
                    Destroy(tile.GetComponent<BoxCollider>()); // no physics needed
                    floorTiles[x, z] = tile;
                }
            }
        }

        List<MapLocation> walkable = GetWalkableTiles();
        if (walkable.Count < 2)
        {
            Debug.LogError("Not enough walkable tiles!");
            return;
        }

        MapLocation startLoc = walkable[0];
        MapLocation goalLoc  = walkable[walkable.Count - 1];

        seeker = SpawnCapsule(startLoc, Color.green, "Seeker");
        target = SpawnCapsule(goalLoc, Color.red, "Target");

        var pathfinder = new AStarPathfinder(maze);
        StartCoroutine(pathfinder.FindPathVisual(startLoc, goalLoc, floorTiles, OnPathFound));
    }

    void OnPathFound(List<MapLocation> path)
    {
        if (path == null) return;

        // Color the final path yellow
        foreach (var loc in path)
            if (floorTiles[loc.x, loc.z] != null)
                floorTiles[loc.x, loc.z].GetComponent<Renderer>().material.color = Color.yellow;

        DrawPath(path);
        Debug.Log("Path found! Length: " + path.Count + " steps.");
    }

    List<MapLocation> GetWalkableTiles()
    {
        var list = new List<MapLocation>();
        for (int z = 0; z < maze.height; z++)
            for (int x = 0; x < maze.width; x++)
                if (maze.map[x, z] == 0)
                    list.Add(new MapLocation(x, z));
        return list;
    }

    GameObject SpawnCapsule(MapLocation loc, Color color, string name)
    {
        float offsetX = (maze.width * maze.scale) / 2f;
        float offsetZ = (maze.height * maze.scale) / 2f;
        Vector3 pos = new Vector3(loc.x * maze.scale - offsetX, maze.scale / 2f, loc.z * maze.scale - offsetZ);

        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = name;
        capsule.transform.position = pos;
        capsule.transform.localScale = Vector3.one * (maze.scale * 0.4f);
        capsule.GetComponent<Renderer>().material.color = color;
        return capsule;
    }

    void DrawPath(List<MapLocation> path)
    {
        GameObject lineObj = new GameObject("PathLine");
        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.positionCount = path.Count;
        lineRenderer.startWidth = 1f;
        lineRenderer.endWidth = 1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.yellow;
        lineRenderer.useWorldSpace = true;

        float offsetX = (maze.width * maze.scale) / 2f;
        float offsetZ = (maze.height * maze.scale) / 2f;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 pos = new Vector3(
                path[i].x * maze.scale - offsetX,
                3f,
                path[i].z * maze.scale - offsetZ
            );
            lineRenderer.SetPosition(i, pos);
        }
    }
}