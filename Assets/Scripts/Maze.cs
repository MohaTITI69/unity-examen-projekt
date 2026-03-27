using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class MapLocation       
{
    public int x;
    public int z;

    public MapLocation(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public Vector2 ToVector()
    {
        return new Vector2(x, z);
    }

    public static MapLocation operator +(MapLocation a, MapLocation b)
       => new MapLocation(a.x + b.x, a.z + b.z);

    public override bool Equals(object obj)
    {
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            return false;
        else
            return x == ((MapLocation)obj).x && z == ((MapLocation)obj).z;
    }

    public override int GetHashCode()
    {
        return 0;
    }

}

public class Maze : MonoBehaviour
{
    public int width = 10;
    public int height = 10;

    public byte[,] map;

    public int scale = 6;

    public Material glowMat;

    public List<MapLocation> directions = new List<MapLocation>() {
                                            new MapLocation(1,0),
                                            new MapLocation(0,1),
                                            new MapLocation(-1,0),
                                            new MapLocation(0,-1) };

    void Start()
    {
        InitializeMap();
        Generate();
        DrawMap();
    }

    void InitializeMap()
    {
        map = new byte[width,height];
        for (int z = 0; z< height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                map[x,z] = 1;
            }
        }
    }

    void Generate()
    {
        Generate(5, 5);
    }
    void Generate(int x, int z){
        if (CountSquareNeighbours(x, z) >= 2) return;
        map[x, z] = 0;

        directions.Shuffle();

        Generate(x + directions[0].x, z + directions[0].z);
        Generate(x + directions[1].x, z + directions[1].z);
        Generate(x + directions[2].x, z + directions[2].z);
        Generate(x + directions[3].x, z + directions[3].z);
    }

    void DrawMap()
    {
        for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                if (map[x, z] == 1)
                {
                    float offsetX = (width * scale) / 2f;
                    float offsetZ = (height * scale) / 2f;
                    Vector3 pos = new Vector3(x * scale - offsetX, 0, z * scale - offsetZ);
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.localScale = new Vector3(scale, scale, scale);
                    wall.transform.position = pos;
                    wall.GetComponent<Renderer>().material = glowMat;
                }
            }
    }

    public int CountSquareNeighbours(int x, int z){
        int count = 0;
        if (x <= 0 || x >= width - 1 || z <= 0 || z >= height - 1) return 5;
        if (map[x - 1, z] == 0) count++;
        if (map[x + 1, z] == 0) count++;
        if (map[x, z + 1] == 0) count++;
        if (map[x, z - 1] == 0) count++;
        return count;
    }
}
