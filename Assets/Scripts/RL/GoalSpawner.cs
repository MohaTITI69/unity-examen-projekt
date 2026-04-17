using UnityEngine;
using System.Collections.Generic;

public class GoalSpawner : MonoBehaviour
{
    [SerializeField] private MyGrid _grid;
    [SerializeField] private Transform _goal;
    [SerializeField] private float _minDistanceFromAgent = 5f;
    [SerializeField] private Transform _agent;

    [Header("Spawn Mode")]
    [SerializeField] private bool _fullyRandom = false;

    [Header("Portal Spawning")]
    [SerializeField] private PortalPair[] _portalPairs;
    [SerializeField] private float _portalSpawnRadius = 6f;
    [SerializeField] private float _minDistanceFromPortal = 3f;

    private PortalPair _selectedPair;

    public void RespawnGoal()
    {
        if (_fullyRandom)
        {
            SpawnFullyRandom(_goal, _agent.position);
            return;
        }

        _selectedPair = _portalPairs[Random.Range(0, _portalPairs.Length)];

        var walkable = GetNodesNearPortal(_selectedPair.portalB);

        for (int i = 0; i < walkable.Count; i++)
        {
            int rand = Random.Range(i, walkable.Count);
            (walkable[i], walkable[rand]) = (walkable[rand], walkable[i]);
        }

        foreach (Node n in walkable)
        {
            float distToAgent = Vector3.Distance(n.worldPosition, _agent.position);
            if (distToAgent >= _minDistanceFromAgent && !TooCloseToPortal(n.worldPosition))
            {
                _goal.position = n.worldPosition;
                return;
            }
        }

        SpawnFullyRandom(_goal, _agent.position);
    }

    public void RespawnAgent()
    {
        if (_fullyRandom)
        {
            SpawnFullyRandom(_agent, _goal.position);
            return;
        }

        if (_selectedPair == null)
            _selectedPair = _portalPairs[Random.Range(0, _portalPairs.Length)];

        var walkable = GetNodesNearPortal(_selectedPair.portalA);

        for (int i = 0; i < walkable.Count; i++)
        {
            int rand = Random.Range(i, walkable.Count);
            (walkable[i], walkable[rand]) = (walkable[rand], walkable[i]);
        }

        foreach (Node n in walkable)
        {
            float distToGoal = Vector3.Distance(n.worldPosition, _goal.position);
            if (distToGoal >= _minDistanceFromAgent && !TooCloseToPortal(n.worldPosition))
            {
                _agent.position = n.worldPosition;
                return;
            }
        }

        SpawnFullyRandom(_agent, _goal.position);
    }

    private List<Node> GetNodesNearPortal(Transform portal)
    {
        var allWalkable = _grid.GetWalkableNodes();
        List<Node> nearby = new List<Node>();

        foreach (Node n in allWalkable)
        {
            float dist = Vector3.Distance(n.worldPosition, portal.position);
            if (dist <= _portalSpawnRadius && dist >= _minDistanceFromPortal)
                nearby.Add(n);
        }

        return nearby.Count > 5 ? nearby : allWalkable;
    }

    private void SpawnFullyRandom(Transform target, Vector3 otherPosition)
    {
        var walkable = _grid.GetWalkableNodes();

        for (int i = 0; i < walkable.Count; i++)
        {
            int rand = Random.Range(i, walkable.Count);
            (walkable[i], walkable[rand]) = (walkable[rand], walkable[i]);
        }

        foreach (Node n in walkable)
        {
            if (Vector3.Distance(n.worldPosition, otherPosition) >= _minDistanceFromAgent
                && !TooCloseToPortal(n.worldPosition))
            {
                target.position = n.worldPosition;
                return;
            }
        }
    }

    private bool TooCloseToPortal(Vector3 position)
    {
        foreach (PortalPair pair in _portalPairs)
        {
            if (Vector3.Distance(position, pair.portalA.position) < _minDistanceFromPortal ||
                Vector3.Distance(position, pair.portalB.position) < _minDistanceFromPortal)
                return true;
        }
        return false;
    }
}