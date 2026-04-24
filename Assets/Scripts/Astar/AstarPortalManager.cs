using UnityEngine;
using System.Collections.Generic;

public class AstarPortalManager : MonoBehaviour
{
    [SerializeField] private PortalPair[] _portalPairs;
    [SerializeField] private AstarGrid _grid;

    // Maps ingångsnod → exitnod (båda riktningarna per portalpar)
    private Dictionary<AstarNode, AstarNode> _portalConnections;
    // Maps ingångsnod → portalindex (för loggning)
    private Dictionary<AstarNode, int> _portalIndices;

    private void Awake()
    {
        for (int i = 0; i < _portalPairs.Length; i++)
            _portalPairs[i].portalIndex = i;
    }

    // Anropas av Pathfinding innan varje sökning (grid måste vara klart)
    public void BuildPortalConnections()
    {
        _portalConnections = new Dictionary<AstarNode, AstarNode>();
        _portalIndices     = new Dictionary<AstarNode, int>();

        for (int i = 0; i < _portalPairs.Length; i++)
        {
            PortalPair pair = _portalPairs[i];

            AstarNode entryA = _grid.NodeFromWorldPoint(pair.portalA.position);
            AstarNode exitA  = _grid.NodeFromWorldPoint(pair.aPosTarget.position);
            AstarNode entryB = _grid.NodeFromWorldPoint(pair.portalB.position);
            AstarNode exitB  = _grid.NodeFromWorldPoint(pair.bPosTarget.position);

            // A → B
            _portalConnections[entryA] = exitB;
            _portalIndices[entryA]     = i;

            // B → A
            _portalConnections[entryB] = exitA;
            _portalIndices[entryB]     = i;
        }
    }

    public Dictionary<AstarNode, AstarNode> GetPortalConnections() => _portalConnections;
    public Dictionary<AstarNode, int>       GetPortalIndices()     => _portalIndices;
    public PortalPair[] PortalPairs => _portalPairs;
}
