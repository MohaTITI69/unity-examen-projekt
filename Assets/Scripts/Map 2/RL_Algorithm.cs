using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

public class RL_Algorithm : Agent
{
    private PlayerController player;
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();

    private const int MAZE_SIZE = 9; // For 9x9 maze
    private const float STEP_PENALTY = -0.01f;
    private const float INVALID_MOVE_PENALTY = -0.08f;
    private const float GOAL_REWARD = 10f;
    private const float ABILITY_REWARD = 0f;
    private const float REVISIT_PENALTY = -0.001f;
    private bool firstRun = true;
    private bool episodeSucceeded = false;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    public override void Initialize()
    {
        player = GetComponent<PlayerController>();
    }

    public override void OnEpisodeBegin()
    {
        if (player.correspondingMazeScript == null)
        {
            Debug.LogError("RL Agent has no Maze2 reference!");
            return;
        }

        if (firstRun)
        {
            firstRun = false;
        }
        else
        {
            player.correspondingMazeScript.addResult(player.numberOfSteps, episodeSucceeded);
            player.correspondingMazeScript.setupAll();
            episodeSucceeded = false;
        }

            visitedCells.Clear();
        visitedCells.Add(player.currentPos);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (player.correspondingMazeScript.maze == null)
        {
            // Emergency filler so ML-Agents does not crash
            for (int i = 0; i < 84; i++)
                sensor.AddObservation(0f);

            return;
        }

        Vector2Int pos = player.currentPos;
        Vector2Int goal = player.correspondingMazeScript.endCell;

        // Full 9x9 maze observation = 81 values
        for (int y = 0; y < MAZE_SIZE; y++)
        {
            for (int x = 0; x < MAZE_SIZE; x++)
            {
                if (x >= player.correspondingMazeScript.maze.GetLength(0) || y >= player.correspondingMazeScript.maze.GetLength(1))
                {
                    sensor.AddObservation(1f); // treat outside as wall
                    continue;
                }

                int cell = player.correspondingMazeScript.maze[x, y];

                // Normalize:
                // 0 path  -> 0.0
                // 1 wall  -> 0.33
                // 2 player -> 0.66
                // 3 goal -> 1.0
                sensor.AddObservation(cell / 3f);
            }
        }

        // Riktning till målet(normaliserad enhetsvektor)
        sensor.AddObservation((goal.x - pos.x) / (float)MAZE_SIZE);
        sensor.AddObservation((goal.y - pos.y) / (float)MAZE_SIZE);

        // Har vi special kraften?
        sensor.AddObservation(player.usedAbility ? 0f : 1f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (player.correspondingMazeScript.maze == null)
            return;

        AddReward(STEP_PENALTY);

        int action = actions.DiscreteActions[0];

        Vector2Int oldPos = player.currentPos;

        bool validAction = false;

        if (action == 0)
        {
            validAction = player.instantGoNorth();
        }
        else if (action == 1)
        {
            validAction = player.instantGoSouth();
        }
        else if (action == 2)
        {
            validAction = player.instantGoEast();
        }
        else if (action == 3)
        {
            validAction = player.instantGoWest();
        }
        else if (action == 4)
        {
            validAction = player.instantDoAbility();
        }

        
        if (!validAction)
        {
            AddReward(INVALID_MOVE_PENALTY);
        }
        else if (action == 4 && validAction)
        {
            AddReward(ABILITY_REWARD);
        }
        else
        {
            if (visitedCells.Contains(player.currentPos))
            {
                AddReward(REVISIT_PENALTY);
            }
            else
            {
                visitedCells.Add(player.currentPos);
            }
        }


        // Extra penalty for standing still without useful action
        if (player.currentPos == oldPos && action != 4)
        {
            AddReward(-0.02f);
        }

        // Goal reached
        if (player.currentPos == player.correspondingMazeScript.endCell)
        {
            AddReward(GOAL_REWARD);
            Debug.Log("RL reached goal!");
            episodeSucceeded = true;
            EndEpisode();
        }
    }



    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        Keyboard keyboard = Keyboard.current;

        // Default action
        discreteActions[0] = 0;

        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed)
            discreteActions[0] = 0;
        else if (keyboard.sKey.isPressed)
            discreteActions[0] = 1;
        else if (keyboard.dKey.isPressed)
            discreteActions[0] = 2;
        else if (keyboard.aKey.isPressed)
            discreteActions[0] = 3;
        else if (keyboard.spaceKey.isPressed)
            discreteActions[0] = 4;
    }
}