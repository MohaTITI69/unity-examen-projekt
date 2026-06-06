using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class A_Star_Algorithm : MonoBehaviour
{
    [Header("Tillhörande Controller")]
    public PlayerController PlayerController;

    private class Node
    {
        public Vector2Int pos;
        public Node parent;

        public Node(Vector2Int pos, Node parent)
        {
            this.pos = pos;
            this.parent = parent;
        }
    }

    private void Start()
    {
        PlayerController = GetComponent<PlayerController>();
        Debug.Log("startar");
        SolveMaze();
        Debug.Log("Borde redan ha startat no?");
    }

    //|||||||||||||||||||||||||||||||||||||||||||||||chat gpt under||||||||||||||||||||||||||||||||||||||||||||



    //körs en gång i början
    private void SolveMaze()
    {
        Vector2Int start = PlayerController.currentPos;
        Vector2Int goal = PlayerController.correspondingMazeScript.endCell;


        List<Vector2Int> path = FindPath(start, goal);

        if (path == null)
        {
            Debug.LogError("No path found.");
            return;
        }


        Debug.Log("Path length: " + path.Count);

        StartCoroutine(PerformThePath(path));
    }


    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        List<Node> openList = new List<Node>();
        List<Vector2Int> visited = new List<Vector2Int>();

        openList.Add(new Node(start, null));
        visited.Add(start);

        while (openList.Count > 0)
        {
            Node current = openList[0];
            openList.RemoveAt(0);

            if (current.pos == goal)
                return BuildPath(current);

            foreach (Vector2Int neighbor in GetNeighbors(current.pos))
            {
                if (visited.Contains(neighbor))
                    continue;

                if (!IsWalkable(neighbor, goal))
                    continue;

                visited.Add(neighbor);
                openList.Add(new Node(neighbor, current));
            }
        }

        return null;
    }

    private List<Vector2Int> BuildPath(Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        Node current = endNode;

        while (current.parent != null)
        {
            path.Add(current.pos);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        return new List<Vector2Int>
        {
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.right,
            pos + Vector2Int.left
        };
    }

    private bool IsWalkable(Vector2Int pos, Vector2Int goal)
    {
        if (pos.x < 0 || pos.y < 0 ||
            pos.x >= PlayerController.correspondingMazeScript.maze.GetLength(0) ||
            pos.y >= PlayerController.correspondingMazeScript.maze.GetLength(1))
        {
            return false;
        }

        int cell = PlayerController.correspondingMazeScript.maze[pos.x, pos.y];

        return cell == 0 || cell == 3 || pos == goal;
    }

    private IEnumerator PerformThePath(List<Vector2Int> path)
    {

        //last minute adjustments, plz dont judge the bad code in here

        int stepsBeforeAblityUse = 0;

        if (PlayerController.usedAbility == false)
        {
            stepsBeforeAblityUse = Mathf.RoundToInt(Random.value * path.Count);
        }

        int nrOfSteps = (PlayerController.usedAbility ? path.Count : stepsBeforeAblityUse);
        
        Debug.Log("Number of steps before ability use: " + nrOfSteps + " ability is used: " + PlayerController.usedAbility);

        for (int i = 0; i < nrOfSteps; i++)
        {
            Vector2Int nextCell = path[i];
            Vector2Int direction = nextCell - PlayerController.currentPos;


            yield return new WaitUntil(() => !PlayerController.isMoving);

            if (direction == Vector2Int.up)
                StartCoroutine(PlayerController.goNorth());
            else if (direction == Vector2Int.down)
                StartCoroutine(PlayerController.goSouth());
            else if (direction == Vector2Int.right)
                StartCoroutine(PlayerController.goEast());
            else if (direction == Vector2Int.left)
                StartCoroutine(PlayerController.goWest());
            else
                Debug.LogError("Invalid movement direction: " + direction + "   player current pos: " + PlayerController.currentPos + "    next cell: " + nextCell);

        }

        if (PlayerController.currentPos == PlayerController.correspondingMazeScript.endCell)
        {
            PlayerController.correspondingMazeScript.addResult(PlayerController.numberOfSteps, true);
            PlayerController.correspondingMazeScript.setupAll();
            SolveMaze();
        }
        else if (PlayerController.usedAbility == false)
        {
            //last minute adjustment, plz dont jusdge the bad code
            Vector2Int tempStart = PlayerController.currentPos;
            Vector2Int tempGoal = PlayerController.correspondingMazeScript.endCell;
            int pathLength1 = FindPath(tempStart, tempGoal).Count;

            StartCoroutine(PlayerController.doAbility());

            int pathLength2 = FindPath(tempStart, tempGoal).Count;

            if (pathLength2 <  pathLength1)
            {
                PlayerController.correspondingMazeScript.improvedPathCount++;
            }

            SolveMaze();
        }

            yield return null;
    }
}
