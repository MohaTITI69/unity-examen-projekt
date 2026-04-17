using UnityEngine;
using System;

public class PortalPairAstar : MonoBehaviour
{
    [Header("Portal Entrances")]
    public Transform portalA;
    public Transform portalB;

    [Header("Exit Points (where object appears)")]
    public Transform exitA; 
    public Transform exitB; 

    [Header("Settings")]
    public float cooldown = 0.5f;

    private float _lastTeleportTime = -999f;

    [HideInInspector] public int portalIndex;

    public event Action<int, Transform> OnTeleport;

    public bool TryTeleport(Transform obj)
    {
        if (Time.time - _lastTeleportTime < cooldown)
            return false;

        float distToA = Vector3.Distance(obj.position, portalA.position);
        float distToB = Vector3.Distance(obj.position, portalB.position);

        if (distToA < distToB)
        {
            obj.position = exitB.position;
            obj.rotation = Quaternion.LookRotation(-exitB.forward);
        }
        else
        {
            obj.position = exitA.position;
            obj.rotation = Quaternion.LookRotation(-exitA.forward);
        }

        _lastTeleportTime = Time.time;

        OnTeleport?.Invoke(portalIndex, obj);

        return true;
    }
}