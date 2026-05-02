using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents.Sensors;

public class RL_Algorithm : Agent
{
    private PlayerController player;

    public override void Initialize()
    {
        player = GetComponent<PlayerController>();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];

        if (action == 1)
            StartCoroutine(player.goNorth());
        else if (action == 2)
            StartCoroutine(player.goSouth());
        else if (action == 3)
            StartCoroutine(player.goEast());
        else if (action == 4)
            StartCoroutine(player.goWest());
    }

    public override void Heuristic(in Unity.MLAgents.Actuators.ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;

        if (Keyboard.current.upArrowKey.isPressed)
            discreteActions[0] = 1;
        else if (Keyboard.current.downArrowKey.isPressed)
            discreteActions[0] = 2;
        else if (Keyboard.current.rightArrowKey.isPressed)
            discreteActions[0] = 3;
        else if (Keyboard.current.leftArrowKey.isPressed)
            discreteActions[0] = 4;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (player == null)
            player = GetComponent<PlayerController>();

        if (player == null || player.correspondingMazeScript == null || player.maze == null)
        {
            // Add dummy observations so ML-Agents still gets 11 values
            for (int i = 0; i < 11; i++)
                sensor.AddObservation(0f);

            return;
        }

        Vector2Int pos = player.currentPos;
        Vector2Int goal = player.correspondingMazeScript.endCell;
        int[,] maze = player.maze;

        int width = maze.GetLength(0);
        int height = maze.GetLength(1);

        sensor.AddObservation((float)pos.x / width);
        sensor.AddObservation((float)pos.y / height);

        sensor.AddObservation((float)goal.x / width);
        sensor.AddObservation((float)goal.y / height);

        sensor.AddObservation((float)(goal.x - pos.x) / width);
        sensor.AddObservation((float)(goal.y - pos.y) / height);

        sensor.AddObservation(IsWalkable(pos + Vector2Int.up) ? 1f : 0f);
        sensor.AddObservation(IsWalkable(pos + Vector2Int.down) ? 1f : 0f);
        sensor.AddObservation(IsWalkable(pos + Vector2Int.right) ? 1f : 0f);
        sensor.AddObservation(IsWalkable(pos + Vector2Int.left) ? 1f : 0f);

        sensor.AddObservation(player.usedAbility ? 1f : 0f);
    }

    private bool IsWalkable(Vector2Int cell)
    {
        if (player == null || player.maze == null)
            return false;

        int[,] maze = player.maze;

        if (cell.x < 0 || cell.y < 0 ||
            cell.x >= maze.GetLength(0) ||
            cell.y >= maze.GetLength(1))
            return false;

        return maze[cell.x, cell.y] == 0 || maze[cell.x, cell.y] == 3;
    }
}
