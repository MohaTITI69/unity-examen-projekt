using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public class A_StarPlayerScript : MonoBehaviour
{
    //inteded to be assigned by maze script when created by it
    public Maze2 correspondingMazeScript;
    public Vector2Int currentPos;
    public int[,] maze;


    private void Start()
    {
        setup();
    }

    void setup()
    {
        maze = correspondingMazeScript.maze;
        currentPos = getPlayerPos();
        
    }

    Vector2Int getPlayerPos()
    {
        //y
        for (int y = 0; y < maze.GetLength(1); y++)
        {
            //x
            for (int x = 0; x < maze.GetLength(0); x++)
            {
                if (maze[x, y] == 2)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        Debug.LogError("SPELARE EXISTERAR INTE I MAZE");
        return new Vector2Int(0, 0);
    }


    void reachedGoal()//fortsätt vidare sen
    {
        Debug.Log("Nådde målet");
    }


    void updatePlayer()
    {
        gameObject.transform.position = correspondingMazeScript.getPos(currentPos.x, currentPos.y);
    }


    public void goNorth(CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("TRycker go north");
            //är path
            if (maze[currentPos.x, currentPos.y + 1] == 0)
            {
                maze[currentPos.x, currentPos.y] = 0;
                maze[currentPos.x, currentPos.y + 1] = 2;
                currentPos.y++;
                updatePlayer();
                Debug.Log("Gått north1");
            }
            //är goal
            else if (maze[currentPos.x, currentPos.y + 1] == 3)
            {
                maze[currentPos.x, currentPos.y] = 0;
                maze[currentPos.x, currentPos.y + 1] = 2;
                currentPos.y++;
                updatePlayer();
                reachedGoal();
                Debug.Log("Gått north2");
            }
        }
    }


    public void goSouth(CallbackContext context)
    {
        if (context.performed)
        {
            //är path
            if (maze[currentPos.x, currentPos.y - 1] == 0)
            {
                maze[currentPos.x, currentPos.y] = 0;
                maze[currentPos.x, currentPos.y - 1] = 2;
                currentPos.y--;
                updatePlayer();
            }
            //är goal
            else if (maze[currentPos.x, currentPos.y - 1] == 3)
            {
                maze[currentPos.x, currentPos.y] = 0;
                maze[currentPos.x, currentPos.y - 1] = 2;
                currentPos.y--;
                updatePlayer();
                reachedGoal();
            }
        }
    }


    public void goEast(CallbackContext context)
    {
        if (context.performed)
        {
            //är path
            if (maze[currentPos.x + 1, currentPos.y] == 0)
            {
                maze[currentPos.x, currentPos.y] = 0;
                maze[currentPos.x + 1, currentPos.y] = 2;
                currentPos.x++;
                updatePlayer();
            }
            //är goal
            else if (maze[currentPos.x + 1, currentPos.y] == 3)
            {
                maze[currentPos.x, currentPos.y] = 0;
                maze[currentPos.x + 1, currentPos.y] = 2;
                currentPos.x++;
                updatePlayer();
                reachedGoal();
            }
        }
    }


    public void goWest(CallbackContext context)
    {
        if (context.performed)
        {
            //är path
            if (maze[currentPos.x - 1, currentPos.y] == 0)
            {
                maze[currentPos.x, currentPos.y] = 0;
                maze[currentPos.x - 1, currentPos.y] = 2;
                currentPos.x--;
                updatePlayer();
            }
            //är goal
            else if (maze[currentPos.x - 1, currentPos.y] == 3)
            {
                maze[currentPos.x, currentPos.y] = 0;
                maze[currentPos.x - 1, currentPos.y] = 2;
                currentPos.x--;
                updatePlayer();
                reachedGoal();
            }
        }
    }

}
