using UnityEngine;

public class PortalPair : MonoBehaviour
{
    public Transform portalA;      
    public Transform portalB;      
    public Transform aPosTarget;   
    public Transform bPosTarget;   

    [HideInInspector] public int portalIndex;

    private float _cooldown = 0.5f;
    private float _lastTeleportTime = -999f;

    public void TryTeleport(Transform agent, TurtleAgent turtleAgent)
    {
        if (Time.time - _lastTeleportTime < _cooldown)
            return;

        float distToA = Vector3.Distance(agent.position, portalA.position);
        float distToB = Vector3.Distance(agent.position, portalB.position);

        if (distToA < distToB)
        {
            agent.position = bPosTarget.position;
            agent.rotation = Quaternion.LookRotation(-bPosTarget.forward);
        }
        else
        {
            agent.position = aPosTarget.position;
            agent.rotation = Quaternion.LookRotation(-aPosTarget.forward);
        }

        _lastTeleportTime = Time.time;
        turtleAgent.OnPortalUsed(portalIndex);
    }
}