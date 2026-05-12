using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public class PlayerController : MonoBehaviour
{
    //inteded to be assigned by maze script when created by it
    public Maze2 correspondingMazeScript;
    public Vector2Int currentPos;
    public bool isMoving = false;
    private float delayBetweenSteeps = 0.3f;//ändra tillbaka|||||||||||||||||||||||||||||
    public bool usedAbility = false;
    public bool noMovmentDelay;


    private void Start()
    {
        setup();
    }

    public void setup()
    {
        currentPos = getPlayerPos();
        usedAbility = false;
        updatePlayer();
    }

    Vector2Int getPlayerPos()
    {
        //y
        for (int y = 0; y < correspondingMazeScript.maze.GetLength(1); y++)
        {
            //x
            for (int x = 0; x < correspondingMazeScript.maze.GetLength(0); x++)
            {
                if (correspondingMazeScript.maze[x, y] == 2)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        Debug.LogError("SPELARE EXISTERAR INTE I MAZE");
        return new Vector2Int(0, 0);
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
            if (correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] == 0)
            {
                correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
                correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] = 2;
                currentPos.y++;
                updatePlayer();
            }
            //är goal
            else if (correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] == 3)
            {
                correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
                correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] = 2;
                currentPos.y++;
                updatePlayer();
            }

            if (noMovmentDelay)
            {
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSteeps);
            }

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
            if (correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] == 0)
            {
                correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
                correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] = 2;
                currentPos.y--;
                updatePlayer();
            }
            //är goal
            else if (correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] == 3)
            {
                correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
                correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] = 2;
                currentPos.y--;
                updatePlayer();
            }

            if (noMovmentDelay)
            {
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSteeps);
            }

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
            if (correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] == 0)
            {
                correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
                correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] = 2;
                currentPos.x++;
                updatePlayer();
            }
            //är goal
            else if (correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] == 3)
            {
                correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
                correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] = 2;
                currentPos.x++;
                updatePlayer();
            }

            if (noMovmentDelay)
            {
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSteeps);
            }

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
            if (correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] == 0)
            {
                correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
                correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] = 2;
                currentPos.x--;
                updatePlayer();
            }
            //är goal
            else if (correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] == 3)
            {
                correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
                correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] = 2;
                currentPos.x--;
                updatePlayer();
            }

            if (noMovmentDelay)
            {
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSteeps);
            }

            isMoving = false;
        }

        
    }


    public IEnumerator doAbility()
    {
        if (!usedAbility)
        {
            //up
            if ((correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] == 1) && (currentPos.y + 1 < correspondingMazeScript.maze.GetLength(1) - 1))
            {
                Destroy(correspondingMazeScript.walls[currentPos.x, currentPos.y + 1]);
                correspondingMazeScript.walls[currentPos.x, currentPos.y + 1] = null;
                correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] = 0;
            }

            //down
            if ((correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] == 1) && (currentPos.y - 1 > 0))
            {
                Destroy(correspondingMazeScript.walls[currentPos.x, currentPos.y - 1]);
                correspondingMazeScript.walls[currentPos.x, currentPos.y - 1] = null;
                correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] = 0;
            }

            //right
            if ((correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] == 1) && (currentPos.x + 1 < correspondingMazeScript.maze.GetLength(0) - 1))
            {
                Destroy(correspondingMazeScript.walls[currentPos.x + 1, currentPos.y]);
                correspondingMazeScript.walls[currentPos.x + 1, currentPos.y] = null;
                correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] = 0;
            }

            //left
            if ((correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] == 1) && (currentPos.x - 1 > 0))
            {
                Destroy(correspondingMazeScript.walls[currentPos.x - 1, currentPos.y]);
                correspondingMazeScript.walls[currentPos.x - 1, currentPos.y] = null;
                correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] = 0;
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


    public bool instantGoNorth()
    {
        //är path
        if (correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] == 0)
        {
            correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
            correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] = 2;
            currentPos.y++;
            updatePlayer();
            return true;
        }
        //är goal
        else if (correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] == 3)
        {
            correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
            correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] = 2;
            currentPos.y++;
            updatePlayer();
            //reachedGoal();   ta bort
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool instantGoSouth()
    {
        //är path
        if (correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] == 0)
        {
            correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
            correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] = 2;
            currentPos.y--;
            updatePlayer();
            return true;
        }
        //är goal
        else if (correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] == 3)
        {
            correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
            correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] = 2;
            currentPos.y--;
            updatePlayer();
            //reachedGoal();   ta bort
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool instantGoEast()
    {
        //är path
        if (correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] == 0)
        {
            correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
            correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] = 2;
            currentPos.x++;
            updatePlayer();
            return true;
        }
        //är goal
        else if (correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] == 3)
        {
            correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
            correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] = 2;
            currentPos.x++;
            updatePlayer();
            //reachedGoal();   ta bort
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool instantGoWest()
    {
        //är path
        if (correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] == 0)
        {
            correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
            correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] = 2;
            currentPos.x--;
            updatePlayer();
            return true;
        }
        //är goal
        else if (correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] == 3)
        {
            correspondingMazeScript.maze[currentPos.x, currentPos.y] = 0;
            correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] = 2;
            currentPos.x--;
            updatePlayer();
            //reachedGoal();   ta bort
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool instantDoAbility()
    {
        if (!usedAbility)
        {
            //up
            if ((correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] == 1) && (currentPos.y + 1 < correspondingMazeScript.maze.GetLength(1) - 1))
            {
                Destroy(correspondingMazeScript.walls[currentPos.x, currentPos.y + 1]);
                correspondingMazeScript.walls[currentPos.x, currentPos.y + 1] = null;
                correspondingMazeScript.maze[currentPos.x, currentPos.y + 1] = 0;
            }

            //down
            if ((correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] == 1) && (currentPos.y - 1 > 0))
            {
                Destroy(correspondingMazeScript.walls[currentPos.x, currentPos.y - 1]);
                correspondingMazeScript.walls[currentPos.x, currentPos.y - 1] = null;
                correspondingMazeScript.maze[currentPos.x, currentPos.y - 1] = 0;
            }

            //right
            if ((correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] == 1) && (currentPos.x + 1 < correspondingMazeScript.maze.GetLength(0) - 1))
            {
                Destroy(correspondingMazeScript.walls[currentPos.x + 1, currentPos.y]);
                correspondingMazeScript.walls[currentPos.x + 1, currentPos.y] = null;
                correspondingMazeScript.maze[currentPos.x + 1, currentPos.y] = 0;
            }

            //left
            if ((correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] == 1) && (currentPos.x - 1 > 0))
            {
                Destroy(correspondingMazeScript.walls[currentPos.x - 1, currentPos.y]);
                correspondingMazeScript.walls[currentPos.x - 1, currentPos.y] = null;
                correspondingMazeScript.maze[currentPos.x - 1, currentPos.y] = 0;
            }

            usedAbility = true;
            return true;
        }
        else
        {
            return false;
        }
    }

}
