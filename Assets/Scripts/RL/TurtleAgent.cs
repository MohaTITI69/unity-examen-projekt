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
    }

    private void ResetPositions()
    {
        _goalSpawner.RespawnGoal();
        _goalSpawner.RespawnAgent(); // agent now spawns randomly too
        transform.localRotation = _originalTurtleRotation;
        _previousDist = Vector3.Distance(transform.localPosition, _goal.localPosition);
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
        Vector3 toGoal = (_goal.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(toGoal.x);
        sensor.AddObservation(toGoal.z);

        float dist = Vector3.Distance(transform.localPosition, _goal.localPosition);
        sensor.AddObservation(dist / 70f);

        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);
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
        AddReward(10f);
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