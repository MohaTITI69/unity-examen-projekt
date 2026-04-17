using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class TurtleAgent : Agent
{
    [SerializeField] private Transform _goal;
    [SerializeField] private Renderer _groundRenderer;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 180f;

    [SerializeField] private PortalPair[] _portalPairs;

    private Vector3 _originalTurtlePosition;
    private Quaternion _originalTurtleRotation;

    [SerializeField] private GoalSpawner _goalSpawner;

    private Renderer _renderer;

    [HideInInspector] public int CurrentEpisode = 0;
    [HideInInspector] public float CumulativeReward = 0f;

    private Color _defaultGroundColor;
    private Coroutine _flashGroundCoroutine;

    private bool _reachedGoalLastEpisode = false;
    private float _previousDist;
    private bool _canDetectGoal = false;
    private bool _usedOptimalPortal = false;  // ADD THIS

    // Track if agent used a portal this episode
    private bool _usedPortalThisEpisode = false;
    private int _optimalPortalIndex = -1; // which portal pair is optimal at episode start

    public override void Initialize()
    {
        _renderer = GetComponent<Renderer>();
        CurrentEpisode = 0;
        CumulativeReward = 0f;

        _originalTurtlePosition = transform.localPosition;
        _originalTurtleRotation = transform.localRotation;

        if (_groundRenderer != null)
            _defaultGroundColor = _groundRenderer.material.color;
    }

    public override void OnEpisodeBegin()
    {
        CurrentEpisode++;
        _canDetectGoal = false;
        _usedPortalThisEpisode = false;
        _usedOptimalPortal = false;  // ADD THIS

        if (_groundRenderer != null)
        {
            if (_flashGroundCoroutine != null)
                StopCoroutine(_flashGroundCoroutine);

            Color flashColor = _reachedGoalLastEpisode ? Color.green : Color.red;
            _flashGroundCoroutine = StartCoroutine(FlashGround(flashColor, 2.5f));
        }

        _reachedGoalLastEpisode = false;
        CumulativeReward = 0f;
        _renderer.material.color = Color.blue;

        ResetPositions();

        _previousDist = Vector3.Distance(transform.localPosition, _goal.localPosition);

        // Calculate optimal portal at episode start
        _optimalPortalIndex = GetOptimalPortalIndex();
    }

    private void ResetPositions()
    {
        _goalSpawner.RespawnGoal();
        _goalSpawner.RespawnAgent();
        transform.localRotation = _originalTurtleRotation;
        _previousDist = Vector3.Distance(transform.localPosition, _goal.localPosition);
    }

    // Returns index of best portal pair, or -1 if walking direct is better
    private int GetOptimalPortalIndex()
    {
        float directDist = Vector3.Distance(transform.position, _goal.position);
        float bestPortalCost = directDist; // portal must beat direct distance
        int bestIndex = -1;

        for (int i = 0; i < _portalPairs.Length; i++)
        {
            PortalPair pair = _portalPairs[i];

            // Cost of going through A → exit at B
            float costViaA = Vector3.Distance(transform.position, pair.portalA.position)
                           + Vector3.Distance(pair.bPosTarget.position, _goal.position);

            // Cost of going through B → exit at A
            float costViaB = Vector3.Distance(transform.position, pair.portalB.position)
                           + Vector3.Distance(pair.aPosTarget.position, _goal.position);

            float bestCostThisPair = Mathf.Min(costViaA, costViaB);

            if (bestCostThisPair < bestPortalCost)
            {
                bestPortalCost = bestCostThisPair;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

 // Replace OnPortalUsed with this
    public void OnPortalUsed(int portalIndex)
    {
        // Only reward portal use once per episode
        if (_usedPortalThisEpisode) return;
        
        _usedPortalThisEpisode = true;

        if (portalIndex == _optimalPortalIndex)
        {
            // Don't reward immediately — just flag it, reward at goal
            _usedOptimalPortal = true;
        }
        else if (_optimalPortalIndex == -1)
        {
            // Portal wasn't optimal at all
            AddReward(-1f);
        }
        else
        {
            // Wrong portal
            AddReward(-0.5f);
        }
    }

    private IEnumerator FlashGround(Color targetColor, float duration)
    {
        _groundRenderer.material.color = targetColor;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            _groundRenderer.material.color =
                Color.Lerp(targetColor, _defaultGroundColor, elapsedTime / duration);
            yield return null;
        }

        _groundRenderer.material.color = _defaultGroundColor;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Base observations (5)
        Vector3 toGoal = (_goal.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(toGoal.x);
        sensor.AddObservation(toGoal.z);
        float dist = Vector3.Distance(transform.localPosition, _goal.localPosition);
        sensor.AddObservation(dist / 70f);
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);

        // Portal observations (8 per pair x 3 pairs = 24)
        foreach (PortalPair pair in _portalPairs)
        {
            Vector3 toA = (pair.portalA.position - transform.position).normalized;
            sensor.AddObservation(toA.x);
            sensor.AddObservation(toA.z);
            sensor.AddObservation(Vector3.Distance(transform.position, pair.portalA.position) / 70f);

            Vector3 toB = (pair.portalB.position - transform.position).normalized;
            sensor.AddObservation(toB.x);
            sensor.AddObservation(toB.z);
            sensor.AddObservation(Vector3.Distance(transform.position, pair.portalB.position) / 70f);

            // How useful is each exit relative to goal
            sensor.AddObservation(Vector3.Distance(pair.bPosTarget.position, _goal.position) / 70f);
            sensor.AddObservation(Vector3.Distance(pair.aPosTarget.position, _goal.position) / 70f);
        }

        // Is a portal optimal right now? (1 = yes, 0 = no) — explicit hint
        sensor.AddObservation(_optimalPortalIndex >= 0 ? 1f : 0f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;

        if (Input.GetKey(KeyCode.W))
            discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.A))
            discreteActionsOut[0] = 2;
        else if (Input.GetKey(KeyCode.D))
            discreteActionsOut[0] = 3;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        _canDetectGoal = true;

        MoveAgent(actions.DiscreteActions);

        float currentDist = Vector3.Distance(transform.localPosition, _goal.localPosition);
        AddReward((_previousDist - currentDist) * 0.1f);
        _previousDist = currentDist;

        AddReward(-0.001f);

        CumulativeReward = GetCumulativeReward();
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var action = act[0];

        switch (action)
        {
            case 1:
                transform.position += transform.forward * _moveSpeed * Time.fixedDeltaTime;
                break;
            case 2:
                transform.Rotate(0f, -_rotationSpeed * Time.fixedDeltaTime, 0f);
                break;
            case 3:
                transform.Rotate(0f, _rotationSpeed * Time.fixedDeltaTime, 0f);
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_canDetectGoal && other.gameObject.CompareTag("goal"))
            GoalReached();
    }

    private void GoalReached()
    {
        float bonus = 0f;

        if (_usedOptimalPortal)
            bonus = 3f; // extra bonus for using optimal portal AND reaching goal
        else if (_optimalPortalIndex >= 0 && !_usedPortalThisEpisode)
            bonus = -1f; // portal was available but ignored

        AddReward(10f + bonus);
        _reachedGoalLastEpisode = true;
        CumulativeReward = GetCumulativeReward();
        EndEpisode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-1f);
            _renderer.material.color = Color.red;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
            _renderer.material.color = Color.blue;
    }
}