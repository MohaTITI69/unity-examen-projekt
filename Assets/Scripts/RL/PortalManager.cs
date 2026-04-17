using UnityEngine;

public class PortalManager : MonoBehaviour
{
    [SerializeField] private PortalPair[] _portalPairs;
    private TurtleAgent _turtleAgent;

    private void Awake()
    {
        _turtleAgent = GetComponent<TurtleAgent>();
        for (int i = 0; i < _portalPairs.Length; i++)
            _portalPairs[i].portalIndex = i;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Portal")) return;

        for (int i = 0; i < _portalPairs.Length; i++)
        {
            PortalPair pair = _portalPairs[i];

            if (other.transform == pair.portalA || other.transform.IsChildOf(pair.portalA) ||
                other.transform == pair.portalB || other.transform.IsChildOf(pair.portalB))
            {
                pair.TryTeleport(transform, _turtleAgent);
                return;
            }
        }
    }
}