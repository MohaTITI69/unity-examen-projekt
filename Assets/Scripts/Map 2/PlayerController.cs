using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public class PlayerController : MonoBehaviour
{
    //inteded to be assigned by maze script when created by it
    public Maze2 correspondingMazeScript;
    public Vector2Int currentPos;
    public int[,] maze;
    public GameObject[,] walls;
    public bool isMoving = false;
    private float delayBetweenSteeps = 0.1f;//ändra tillbaka|||||||||||||||||||||||||||||
    public bool usedAbility = false;


    private void Start()
    {
        setup();
    }

    void setup()
    {
        maze = correspondingMazeScript.maze;
        walls = correspondingMazeScript.walls;
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


    public IEnumerator goNorth()
    {
        if (isMoving)
        {
            yield return null;
        }
        else
        {
            isMoving = true;

            //är path
            if (maze[currentPos.x, currentPos.y + 1] == 0)
            {
                maze[currentPos.x, currentPos.y] = 0;
                maze[currentPos.x, currentPos.y + 1] = 2;
                currentPos.y++;
                updatePlayer();
            }
            //är goal
            else if (maze[currentPos.x, currentPos.y + 1] == 3)
            {
                maze[currentPos.x, currentPos.y] = 0;
                maze[currentPos.x, currentPos.y + 1] = 2;
                currentPos.y++;
                updatePlayer();
                reachedGoal();
            }

            yield return new WaitForSeconds(delayBetweenSteeps);

            isMoving = false;
        }

    }


    public IEnumerator goSouth()
    {
        if (isMoving)
        {
            yield return null;
        }
        else
        {
            isMoving = true;

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

            yield return new WaitForSeconds(delayBetweenSteeps);

            isMoving = false;
        }


        
    }


    public IEnumerator goEast()
    {
        if (isMoving)
        {
            yield return null;
        }
        else
        {
            isMoving = true;

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

            yield return new WaitForSeconds(delayBetweenSteeps);

            isMoving = false;
        }
    }


    public IEnumerator goWest()
    {
        if (isMoving)
        {
            yield return null;
        }
        else
        {
            isMoving = true;

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

            yield return new WaitForSeconds(delayBetweenSteeps);

            isMoving = false;
        }

        
    }


    public IEnumerator doAbility()
    {
        if (!usedAbility)
        {
            //up
            if ((maze[currentPos.x, currentPos.y + 1] == 1) && (currentPos.y + 1 < maze.GetLength(1) - 1))
            {
                Destroy(walls[currentPos.x, currentPos.y + 1]);
                walls[currentPos.x, currentPos.y + 1] = null;
                maze[currentPos.x, currentPos.y + 1] = 0;
            }

            //down
            if ((maze[currentPos.x, currentPos.y - 1] == 1) && (currentPos.y - 1 > 0))
            {
                Destroy(walls[currentPos.x, currentPos.y - 1]);
                walls[currentPos.x, currentPos.y - 1] = null;
                maze[currentPos.x, currentPos.y - 1] = 0;
            }

            //right
            if ((maze[currentPos.x + 1, currentPos.y] == 1) && (currentPos.x + 1 < maze.GetLength(0) - 1))
            {
                Destroy(walls[currentPos.x + 1, currentPos.y]);
                walls[currentPos.x + 1, currentPos.y] = null;
                maze[currentPos.x + 1, currentPos.y] = 0;
            }

            //left
            if ((maze[currentPos.x - 1, currentPos.y] == 1) && (currentPos.x - 1 > 0))
            {
                Destroy(walls[currentPos.x - 1, currentPos.y]);
                walls[currentPos.x - 1, currentPos.y] = null;
                maze[currentPos.x - 1, currentPos.y] = 0;
            }

            usedAbility = true;
        }
        
        yield break;
    }


    public void pressedUp(CallbackContext context)
    {
        if (context.performed)
        {
            StartCoroutine(goNorth());
        }
    }

    public void pressedDown(CallbackContext context)
    {
        if (context.performed)
        {
            StartCoroutine(goSouth());
        }
    }

    public void pressedLeft(CallbackContext context)
    {
        if (context.performed)
        {
            StartCoroutine(goWest());
        }
    }

    public void pressedRight(CallbackContext context)
    {
        if (context.performed)
        {
            StartCoroutine(goEast());
        }
    }


    public void pressedAbility(CallbackContext context)
    {
        if (context.performed)
        {
            StartCoroutine(doAbility());
        }
    }
}
