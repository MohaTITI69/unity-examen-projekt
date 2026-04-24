using UnityEngine;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    [SerializeField] private AstarGrid grid;
    [SerializeField] private Transform agent;
    [SerializeField] private Transform goal;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float minDistanceFromPortal = 3f;
    [SerializeField] private PortalPair[] portalPairs;

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
            if (dist >= minDistance && !TooCloseToPortal(n.worldPosition))
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
            if (dist >= minDistance && !TooCloseToPortal(n.worldPosition))
            {
                agent.position = new Vector3(n.worldPosition.x, agent.position.y, n.worldPosition.z);
                return;
            }
        }
    }

    private bool TooCloseToPortal(Vector3 position)
    {
        if (portalPairs == null) return false;
        foreach (PortalPair pair in portalPairs)
        {
            if (Vector3.Distance(position, pair.portalA.position) < minDistanceFromPortal ||
                Vector3.Distance(position, pair.portalB.position) < minDistanceFromPortal)
                return true;
        }
        return false;
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
