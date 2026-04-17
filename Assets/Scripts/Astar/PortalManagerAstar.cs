using UnityEngine;

public class PortalManagerAstar : MonoBehaviour
{
    [SerializeField] private PortalPairAstar[] portalPairs;

    private void Awake()
    {
        for (int i = 0; i < portalPairs.Length; i++)
            portalPairs[i].portalIndex = i;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Portal")) return;

        for (int i = 0; i < portalPairs.Length; i++)
        {
            PortalPairAstar pair = portalPairs[i];

            if (other.transform == pair.portalA || other.transform.IsChildOf(pair.portalA) ||
                other.transform == pair.portalB || other.transform.IsChildOf(pair.portalB))
            {
                pair.TryTeleport(transform);
                return;
            }
        }
    }

    public PortalPairAstar[] GetPortalPairs() => portalPairs;
}