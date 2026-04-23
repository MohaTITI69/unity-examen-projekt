using UnityEngine;
 
public class PortalManagerAstar : MonoBehaviour
{
    [SerializeField] private PortalPairAstar[] portalPairs;
 
    private void Awake()
    {
        for (int i = 0; i < portalPairs.Length; i++)
            portalPairs[i].portalIndex = i;
    }
 
    public PortalPairAstar[] GetPortalPairs() => portalPairs;
 
    // Enable or disable all portal colliders
    // Call SetPortalsActive(false) before moving seeker
    // Call SetPortalsActive(true) after seeker reaches goal
    public void SetPortalsActive(bool active)
    {
        foreach (PortalPairAstar pair in portalPairs)
        {
            Collider colA = pair.portalA.GetComponent<Collider>();
            Collider colB = pair.portalB.GetComponent<Collider>();
            if (colA != null) colA.enabled = active;
            if (colB != null) colB.enabled = active;
        }
    }
}
 