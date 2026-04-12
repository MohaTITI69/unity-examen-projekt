using UnityEngine;

public class GoalSpawner : MonoBehaviour
{
    [SerializeField] private MyGrid _grid;
    [SerializeField] private Transform _goal;
    [SerializeField] private float _minDistanceFromAgent = 5f;
    [SerializeField] private Transform _agent;

    public void RespawnGoal()
    {
        var walkable = _grid.GetWalkableNodes();

        // Shuffle to pick randomly
        for (int i = 0; i < walkable.Count; i++)
        {
            int rand = Random.Range(i, walkable.Count);
            (walkable[i], walkable[rand]) = (walkable[rand], walkable[i]);
        }

        foreach (Node n in walkable)
        {
            float dist = Vector3.Distance(n.worldPosition, _agent.position);
            if (dist >= _minDistanceFromAgent)
            {
                // Convert world position to local position relative to goal's parent
                _goal.position = n.worldPosition;
                return;
            }
        }
    }


        public void RespawnAgent()
    {
        var walkable = _grid.GetWalkableNodes();

        for (int i = 0; i < walkable.Count; i++)
        {
            int rand = Random.Range(i, walkable.Count);
            (walkable[i], walkable[rand]) = (walkable[rand], walkable[i]);
        }

        foreach (Node n in walkable)
        {
            float dist = Vector3.Distance(n.worldPosition, _goal.position);
            if (dist >= _minDistanceFromAgent)
            {
                _agent.position = n.worldPosition;
                return;
            }
        }
    }
}