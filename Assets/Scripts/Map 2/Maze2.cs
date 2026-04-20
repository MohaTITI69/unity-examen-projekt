using System.Collections.Generic;
using UnityEngine;

public class Maze2 : MonoBehaviour
{
    [Header("Maze Size")]
    public int width = 0;
    public int height = 0;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject Player1;

    [Header("Layout")]
    public float cellSize = 1f;
    public Transform floor;

    [Header("Start & goal(dont change")]
    public Vector2Int startCell;
    public Vector2Int endCell;

    Vector3 startPosition = Vector3.zero;

    public int[,] maze; //0 = path, 1 = vägg, 2 = spelarens nuvarande position, 3 = goal
    private System.Random random = new System.Random();

    void Start()
    {
        width = (int)transform.localScale.x; 
        height = (int)transform.localScale.z;

        GenerateMaze();
        BuildMaze();
        setupPlayers();
    }


    void setupPlayers()
    {
        maze[startCell.x, startCell.y] = 2;
        Instantiate(Player1, new Vector3(startCell.x, floor.position.y + 1, startCell.y) + startPosition, floor.rotation);
    }


    void GenerateMaze()
    {

        maze = new int[width, height];

        // Fill everything as walls first
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                maze[x, y] = 1;
            }
        }

        // Start carving from (1,1)
        startCell = new Vector2Int(1, 1);
        CarvePassagesFrom(startCell.x, startCell.y);
    }


    void generateRandomGoal()
    {
        while (true)
        {
            int x = random.Next(width + 1);
            int y = random.Next(height + 1);
            if (maze[x, y] == 0)
            {
                maze[x, y] = 3;
                //Skapa en grön kub så man kan se var målen är.
                break;
            }
        }
    }


    //genererar en path från start till mål(btw den är recursiv, parametrarna är kordinat den fortsätter generera från)
    void CarvePassagesFrom(int x, int y)
    {
        maze[x, y] = 0;
        int nx = -1;
        int ny = -1;

        List<Vector2Int> directions = new List<Vector2Int>
        {
            new Vector2Int(0, 2),   // up
            new Vector2Int(0, -2),  // down
            new Vector2Int(2, 0),   // right
            new Vector2Int(-2, 0)   // left
        };

        Shuffle(directions);



        foreach (Vector2Int dir in directions)
        {
            nx = x + dir.x;
            ny = y + dir.y;

            //dehär checkar så att nästa cell i den nuvarande riktningen inte är utanför mazen samt inte redan passerat
            if (IsInside(nx, ny) && maze[nx, ny] == 1)
            {
                // Remove wall between current cell and next cell
                maze[x + dir.x / 2, y + dir.y / 2] = 0;
                maze[nx, ny] = 0;

                endCell = new Vector2Int(nx, ny);

                CarvePassagesFrom(nx, ny);
            }
        }
        //after the loop, its reached the end of the path(the goal)
    }


    //checkar om kordinaten är innanför mazen
    bool IsInside(int x, int y)
    {
        return x > 0 && y > 0 && x < width - 1 && y < height - 1;
    }


    void Shuffle(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = random.Next(i, list.Count);
            Vector2Int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }


    //byger mazen från 2D griden vi skapat
    void BuildMaze()
    {
        startPosition = floor.transform.position - new Vector3(gameObject.transform.localScale.x / 2, 0, gameObject.transform.localScale.z / 2) + new Vector3(cellSize/2, 0, cellSize/2);


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (maze[x, y] == 1)
                {
                    Vector3 pos = startPosition + new Vector3(x * cellSize, 1, y * cellSize);

                    GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity);

                    wall.transform.localScale = new Vector3(cellSize, 1f, cellSize);

                    if (floor != null)
                    {
                        wall.transform.SetParent(floor);
                    }
                }
            }
        }
    }
}
