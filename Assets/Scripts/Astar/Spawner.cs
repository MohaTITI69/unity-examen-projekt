using UnityEngine;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    [SerializeField] private AstarGrid grid;
    [SerializeField] private Transform agent;
    [SerializeField] private Transform goal;
    [SerializeField] private float minDistance = 5f;

    public void RespawnBoth()
    {
        RespawnAgent();
        RespawnGoal();
    }

    public void RespawnGoal()
    {
        var walkable = grid.GetWalkableNodes();
        Shuffle(walkable);

        foreach (AstarNode n in walkable)
        {
            float dist = Vector3.Distance(n.worldPosition, agent.position);
            if (dist >= minDistance)
            {
                goal.position = new Vector3(n.worldPosition.x, goal.position.y, n.worldPosition.z);
                return;
            }
        }
    }

    public void RespawnAgent()
    {
        var walkable = grid.GetWalkableNodes();
        Shuffle(walkable);

        foreach (AstarNode n in walkable)
        {
            float dist = Vector3.Distance(n.worldPosition, goal.position);
            if (dist >= minDistance)
            {
                agent.position = new Vector3(n.worldPosition.x, agent.position.y, n.worldPosition.z);
                return;
            }
        }
    }

    void Shuffle(List<AstarNode> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}