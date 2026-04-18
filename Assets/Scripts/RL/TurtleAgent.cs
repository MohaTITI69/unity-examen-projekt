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
    [SerializeField] private GoalSpawner _goalSpawner;
 
    private Vector3 _originalTurtlePosition;
    private Quaternion _originalTurtleRotation;
    private Renderer _renderer;
 
    [HideInInspector] public int CurrentEpisode = 0;
    [HideInInspector] public float CumulativeReward = 0f;
 
    private Color _defaultGroundColor;
    private Coroutine _flashGroundCoroutine;
 
    private bool _reachedGoalLastEpisode = false;
    private float _previousDist;
    private bool _canDetectGoal = false;
    private bool _usedOptimalPortal = false;
    private bool _usedPortalThisEpisode = false;
    private int _optimalPortalIndex = -1;
 
    // ──  debug tracking ────────────────────────────────────────
    private Vector3 _episodeStartPos;
    private Vector3 _episodeGoalPos;
    private float _directDistAtStart;
    private float _optimalPortalCostAtStart;
    private int _stepCount = 0;
 
    // Running totals for portal usage rate
    private int _totalEpisodesWithPortalOpportunity = 0;
    private int _totalEpisodesPortalUsed = 0;
    private int _totalEpisodesGoalReached = 0;
    private int _totalEpisodesGoalReachedViaPortal = 0;
    // ────────────────────────────────────────────────────────────────
 
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
        _usedOptimalPortal = false;
        _stepCount = 0;
 
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
 
        _optimalPortalIndex = GetOptimalPortalIndex();
 
        // ── Thesis debug: log episode start info ─────────────────────
        _episodeStartPos = transform.position;
        _episodeGoalPos = _goal.position;
        _directDistAtStart = Vector3.Distance(_episodeStartPos, _episodeGoalPos);
        _optimalPortalCostAtStart = GetOptimalPortalCost();
 
        if (_optimalPortalIndex >= 0)
            _totalEpisodesWithPortalOpportunity++;
 
        Debug.Log($"[RL Episode {CurrentEpisode}] START — " +
                  $"agent={_episodeStartPos:F1}  goal={_episodeGoalPos:F1}  " +
                  $"directDist={_directDistAtStart:F1}  " +
                  $"portalOptimal={(_optimalPortalIndex >= 0 ? "YES cost=" + _optimalPortalCostAtStart.ToString("F1") : "NO")}");
        // ────────────────────────────────────────────────────────────
    }
 
    private void ResetPositions()
    {
        _goalSpawner.RespawnGoal();
        _goalSpawner.RespawnAgent();
        transform.localRotation = _originalTurtleRotation;
        _previousDist = Vector3.Distance(transform.localPosition, _goal.localPosition);
    }
 
    private int GetOptimalPortalIndex()
    {
        float directDist = Vector3.Distance(transform.position, _goal.position);
        float bestPortalCost = directDist;
        int bestIndex = -1;
 
        for (int i = 0; i < _portalPairs.Length; i++)
        {
            PortalPair pair = _portalPairs[i];
            float costViaA = Vector3.Distance(transform.position, pair.portalA.position)
                           + Vector3.Distance(pair.bPosTarget.position, _goal.position);
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
 
    // ── Returns the best portal cost (or direct dist if no portal optimal) ──
    private float GetOptimalPortalCost()
    {
        if (_optimalPortalIndex < 0)
            return _directDistAtStart;
 
        PortalPair pair = _portalPairs[_optimalPortalIndex];
        float costViaA = Vector3.Distance(transform.position, pair.portalA.position)
                       + Vector3.Distance(pair.bPosTarget.position, _goal.position);
        float costViaB = Vector3.Distance(transform.position, pair.portalB.position)
                       + Vector3.Distance(pair.aPosTarget.position, _goal.position);
        return Mathf.Min(costViaA, costViaB);
    }
    // ────────────────────────────────────────────────────────────────
 
    public void OnPortalUsed(int portalIndex)
    {
        if (_usedPortalThisEpisode) return;
        _usedPortalThisEpisode = true;
 
        // ──  debug: log portal use ─────────────────────────────
        Debug.Log($"[RL Episode {CurrentEpisode}] PORTAL USED — " +
                  $"portalIndex={portalIndex}  " +
                  $"optimalIndex={_optimalPortalIndex}  " +
                  $"wasOptimal={portalIndex == _optimalPortalIndex}  " +
                  $"stepsSoFar={_stepCount}");
        _totalEpisodesPortalUsed++;
        // ────────────────────────────────────────────────────────────
 
        if (portalIndex == _optimalPortalIndex)
            _usedOptimalPortal = true;
        else if (_optimalPortalIndex == -1)
            AddReward(-1f);
        else
            AddReward(-0.5f);
    }
 
    private IEnumerator FlashGround(Color targetColor, float duration)
    {
        _groundRenderer.material.color = targetColor;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            _groundRenderer.material.color = Color.Lerp(targetColor, _defaultGroundColor, elapsedTime / duration);
            yield return null;
        }
        _groundRenderer.material.color = _defaultGroundColor;
    }
 
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 toGoal = (_goal.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(toGoal.x);
        sensor.AddObservation(toGoal.z);
        float dist = Vector3.Distance(transform.localPosition, _goal.localPosition);
        sensor.AddObservation(dist / 70f);
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);
 
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
 
            sensor.AddObservation(Vector3.Distance(pair.bPosTarget.position, _goal.position) / 70f);
            sensor.AddObservation(Vector3.Distance(pair.aPosTarget.position, _goal.position) / 70f);
        }
 
        sensor.AddObservation(_optimalPortalIndex >= 0 ? 1f : 0f);
    }
 
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.A)) discreteActionsOut[0] = 2;
        else if (Input.GetKey(KeyCode.D)) discreteActionsOut[0] = 3;
    }
 
    public override void OnActionReceived(ActionBuffers actions)
    {
        _canDetectGoal = true;
        _stepCount++;
 
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
            case 1: transform.position += transform.forward * _moveSpeed * Time.fixedDeltaTime; break;
            case 2: transform.Rotate(0f, -_rotationSpeed * Time.fixedDeltaTime, 0f); break;
            case 3: transform.Rotate(0f, _rotationSpeed * Time.fixedDeltaTime, 0f); break;
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
            bonus = 3f;
        else if (_optimalPortalIndex >= 0 && !_usedPortalThisEpisode)
            bonus = -1f;
 
        AddReward(10f + bonus);
        _reachedGoalLastEpisode = true;
        CumulativeReward = GetCumulativeReward();
 
        _totalEpisodesGoalReached++;
        if (_usedOptimalPortal) _totalEpisodesGoalReachedViaPortal++;
 
        // ──  debug: episode summary ────────────────────────────
        float portalUsageRate = _totalEpisodesWithPortalOpportunity > 0
            ? (float)_totalEpisodesPortalUsed / _totalEpisodesWithPortalOpportunity * 100f
            : 0f;
        float optimalPortalRate = _totalEpisodesWithPortalOpportunity > 0
            ? (float)_totalEpisodesGoalReachedViaPortal / _totalEpisodesWithPortalOpportunity * 100f
            : 0f;
 
        Debug.Log($"[RL Episode {CurrentEpisode}] GOAL REACHED — " +
                  $"steps={_stepCount}  " +
                  $"totalReward={CumulativeReward:F2}  " +
                  $"portalUsed={_usedPortalThisEpisode}  " +
                  $"portalWasOptimal={(_optimalPortalIndex >= 0 ? "YES" : "NO")}  " +
                  $"usedOptimalPortal={_usedOptimalPortal}");
 
        // Every 10 episodes print running stats
        if (CurrentEpisode % 10 == 0)
        {
            Debug.Log("╔══════════════════════════════════════════════════════╗");
            Debug.Log("║           RL THESIS STATS (running average)          ║");
            Debug.Log("╠══════════════════════════════════════════════════════╣");
            Debug.Log($"║  Episodes completed:              {CurrentEpisode,-22}║");
            Debug.Log($"║  Goals reached:                   {_totalEpisodesGoalReached,-22}║");
            Debug.Log($"║  Episodes w/ portal opportunity:  {_totalEpisodesWithPortalOpportunity,-22}║");
            Debug.Log($"║  Portal used (of opportunities):  {portalUsageRate:F1}%{"",-18}║");
            Debug.Log($"║  Optimal portal + goal reached:   {optimalPortalRate:F1}%{"",-18}║");
            Debug.Log("╠══════════════════════════════════════════════════════╣");
            Debug.Log("║  Compare portal usage % over time — rising = agent   ║");
            Debug.Log("║  is learning to exploit the shortcut                 ║");
            Debug.Log("╚══════════════════════════════════════════════════════╝");
        }
        // ────────────────────────────────────────────────────────────
 
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