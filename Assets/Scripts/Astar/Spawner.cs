using UnityEngine;
using System.Collections.Generic;
 
public class Spawner : MonoBehaviour
{
    [SerializeField] private AstarGrid _grid;
    [SerializeField] private Transform _agent;
    [SerializeField] private Transform _goal;
    [SerializeField] private float _minDistanceFromAgent = 5f;
 
    [Header("Spawn Mode")]
    [SerializeField] private bool _fullyRandom = false;
 
    [Header("Portal Spawning")]
    [SerializeField] private PortalPairAstar[] _portalPairs;
    [SerializeField] private float _portalSpawnRadius = 6f;
    [SerializeField] private float _minDistanceFromPortal = 3f;
 
    private PortalPairAstar _selectedPair;
 
    public void RespawnBoth()
    {
        RespawnGoal();
        RespawnAgent();
    }
 
    public void RespawnGoal()
    {
        if (_fullyRandom)
        {
            SpawnFullyRandom(_goal, _agent.position);
            return;
        }
 
        _selectedPair = _portalPairs[Random.Range(0, _portalPairs.Length)];
 
        var walkable = GetNodesNearPortal(_selectedPair.portalB);
        Shuffle(walkable);
 
        foreach (AstarNode n in walkable)
        {
            float distToAgent = Vector3.Distance(n.worldPosition, _agent.position);
            if (distToAgent >= _minDistanceFromAgent && !TooCloseToPortal(n.worldPosition))
            {
                _goal.position = new Vector3(n.worldPosition.x, _goal.position.y, n.worldPosition.z);
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
        Shuffle(walkable);
 
        foreach (AstarNode n in walkable)
        {
            float distToGoal = Vector3.Distance(n.worldPosition, _goal.position);
            if (distToGoal >= _minDistanceFromAgent && !TooCloseToPortal(n.worldPosition))
            {
                _agent.position = new Vector3(n.worldPosition.x, _agent.position.y, n.worldPosition.z);
                return;
            }
        }
 
        SpawnFullyRandom(_agent, _goal.position);
    }
 
    private List<AstarNode> GetNodesNearPortal(Transform portal)
    {
        var allWalkable = _grid.GetWalkableNodes();
        List<AstarNode> nearby = new List<AstarNode>();
 
        foreach (AstarNode n in allWalkable)
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
        Shuffle(walkable);
 
        foreach (AstarNode n in walkable)
        {
            if (Vector3.Distance(n.worldPosition, otherPosition) >= _minDistanceFromAgent
                && !TooCloseToPortal(n.worldPosition))
            {
                target.position = new Vector3(n.worldPosition.x, target.position.y, n.worldPosition.z);
                return;
            }
        }
    }
 
    private bool TooCloseToPortal(Vector3 position)
    {
        foreach (PortalPairAstar pair in _portalPairs)
        {
            if (Vector3.Distance(position, pair.portalA.position) < _minDistanceFromPortal ||
                Vector3.Distance(position, pair.portalB.position) < _minDistanceFromPortal)
                return true;
        }
        return false;
    }
 
    private void Shuffle(List<AstarNode> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}