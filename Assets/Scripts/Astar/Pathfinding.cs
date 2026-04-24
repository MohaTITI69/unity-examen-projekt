using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    public Transform seeker, target;
    public int   stepsPerFrame = 5;
    public float seekerSpeed   = 5f;

    AstarGrid          grid;
    Spawner            spawner;
    AstarPortalManager portalManager;
    LineRenderer       lineRenderer;

    // ── Episode stats ─────────────────────────────────────
    private int _episode                   = 0;
    private int _totalEpisodesWithPortal   = 0;
    private int _totalEpisodesPortalChosen = 0;
    private int _totalUnplannedTeleports   = 0;

    void Awake()
    {
        grid          = FindFirstObjectByType<AstarGrid>();
        spawner       = FindFirstObjectByType<Spawner>();
        portalManager = FindFirstObjectByType<AstarPortalManager>();
    }

    void Start()
    {
        GameObject lineObj = new GameObject("PathLine");
        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth    = 0.3f;
        lineRenderer.endWidth      = 0.3f;
        lineRenderer.material      = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor    = Color.yellow;
        lineRenderer.endColor      = Color.yellow;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;

        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return null;
        RunNewPath();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StopAllCoroutines();
            lineRenderer.positionCount = 0;
            grid.ResetTileColors();
            RunNewPath();
        }
    }

    void RunNewPath()
    {
        spawner.RespawnBoth();
        grid.ResetTileColors();
        lineRenderer.positionCount = 0;
        portalManager.BuildPortalConnections();
        _episode++;
        StartCoroutine(FindBestPath(seeker.position, target.position));
    }

    // ── Tyst A* utan farglaggering, returnerar vag eller null ────────────────
    List<AstarNode> SilentAstar(AstarNode startNode, AstarNode targetNode)
    {
        if (startNode == targetNode) return new List<AstarNode> { startNode };

        grid.ResetNodes();

        Heap<AstarNode>    openSet   = new Heap<AstarNode>(grid.MaxSize);
        HashSet<AstarNode> closedSet = new HashSet<AstarNode>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            AstarNode current = openSet.RemoveFirst();
            closedSet.Add(current);

            if (current == targetNode)
                return RetraceNodes(startNode, targetNode);

            foreach (AstarNode neighbour in grid.GetNeighbours(current))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;

                int newCost = current.gCost + GetDistance(current, neighbour);
                if (!openSet.Contains(neighbour) || newCost < neighbour.gCost)
                {
                    neighbour.gCost  = newCost;
                    neighbour.hCost  = GetDistance(neighbour, targetNode);
                    neighbour.parent = current;

                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                    else                               openSet.UpdateItem(neighbour);
                }
            }
        }
        return null;
    }

    List<AstarNode> RetraceNodes(AstarNode startNode, AstarNode endNode)
    {
        List<AstarNode> path = new List<AstarNode>();
        AstarNode current = endNode;
        while (current != startNode)
        {
            path.Add(current);
            current = current.parent;
        }
        path.Add(startNode);
        path.Reverse();
        return path;
    }

    // ── Beräkna bästa väg från en given position ─────────────────────────────
    // Returnerar (bestPath, bestCost, usedPortal, usedPortalIdx, directPath, bestPortalPath)
    (List<AstarNode> bestPath, int bestCost, bool usedPortal, int usedPortalIdx,
     List<AstarNode> directPath, List<AstarNode> bestPortalPath, int directCost, int bestPortalCost, int bestPortalIndex)
    ComputeBestPath(Vector3 fromPos, Vector3 toPos)
    {
        AstarNode startNode  = grid.NodeFromWorldPoint(fromPos);
        AstarNode targetNode = grid.NodeFromWorldPoint(toPos);

        var portalConnections = portalManager.GetPortalConnections();
        var portalIndices     = portalManager.GetPortalIndices();

        List<AstarNode> directPath    = SilentAstar(startNode, targetNode);
        int             directCost    = directPath != null ? directPath[directPath.Count - 1].gCost : int.MaxValue;
        List<AstarNode> bestPath      = directPath;
        int             bestCost      = directCost;
        int             bestPortalCost  = int.MaxValue;
        int             bestPortalIndex = -1;
        List<AstarNode> bestPortalPath  = null;
        bool            usedPortal    = false;
        int             usedPortalIdx = -1;

        foreach (var kvp in portalIndices)
        {
            AstarNode entryNode = kvp.Key;
            AstarNode exitNode  = portalConnections[entryNode];
            int       pIndex    = kvp.Value;

            List<AstarNode> pathToEntry = SilentAstar(startNode, entryNode);
            if (pathToEntry == null) continue;
            int costToEntry = pathToEntry[pathToEntry.Count - 1].gCost;

            List<AstarNode> pathFromExit = SilentAstar(exitNode, targetNode);
            if (pathFromExit == null) continue;
            int costFromExit = pathFromExit[pathFromExit.Count - 1].gCost;

            int totalCost = costToEntry + costFromExit;

            if (totalCost < bestPortalCost)
            {
                bestPortalCost  = totalCost;
                bestPortalIndex = pIndex;
                List<AstarNode> combined = new List<AstarNode>(pathToEntry);
                combined.AddRange(pathFromExit);
                bestPortalPath = combined;
            }

            if (totalCost < bestCost)
            {
                bestCost      = totalCost;
                usedPortal    = true;
                usedPortalIdx = pIndex;
                bestPath      = bestPortalPath;
            }
        }

        return (bestPath, bestCost, usedPortal, usedPortalIdx, directPath, bestPortalPath, directCost, bestPortalCost, bestPortalIndex);
    }

    // ── Huvudsökning ─────────────────────────────────────────────────────────
    IEnumerator FindBestPath(Vector3 startPos, Vector3 targetPos)
    {
        var (bestPath, bestCost, usedPortal, usedPortalIdx,
             directPath, bestPortalPath, directCost, bestPortalCost, bestPortalIndex)
            = ComputeBestPath(startPos, targetPos);

        List<AstarNode> altPath = usedPortal ? directPath : bestPortalPath;

        bool portalOpportunity = bestPortalCost < directCost;
        if (portalOpportunity) _totalEpisodesWithPortal++;
        if (usedPortal)        _totalEpisodesPortalChosen++;

        string chosenRoute = usedPortal ? $"PORTAL (index {usedPortalIdx})" : "DIREKT";

        Debug.Log($"[A* Episode {_episode}] " +
                  $"direktKostnad={directCost}  " +
                  $"bästaPortalKostnad={(bestPortalIndex >= 0 ? bestPortalCost + $" (portal {bestPortalIndex})" : "N/A")}  " +
                  $"portalSnabbare={portalOpportunity}  " +
                  $"valdVäg={chosenRoute}  " +
                  $"valdKostnad={bestCost}");

        if (_episode % 10 == 0)
        {
            float portalRate = _totalEpisodesWithPortal > 0
                ? (float)_totalEpisodesPortalChosen / _totalEpisodesWithPortal * 100f
                : 0f;

            Debug.Log("╔══════════════════════════════════════════════════════╗");
            Debug.Log("║              A* PORTAL STATS (sammanfattning)        ║");
            Debug.Log("╠══════════════════════════════════════════════════════╣");
            Debug.Log($"║  Episodes körda:                  {_episode,-22}║");
            Debug.Log($"║  Episodes med portalmöjlighet:    {_totalEpisodesWithPortal,-22}║");
            Debug.Log($"║  Portal vald (av möjligheter):    {portalRate:F1}%{"",-18}║");
            Debug.Log($"║  Totalt oplanerade teleporter:    {_totalUnplannedTeleports,-22}║");
            Debug.Log("╚══════════════════════════════════════════════════════╝");
        }

        if (bestPath != null)
        {
            if (altPath != null)
            {
                VisualizeAltPath(altPath, grid.NodeFromWorldPoint(startPos), grid.NodeFromWorldPoint(targetPos));
                yield return new WaitForSeconds(1.0f);
            }

            VisualizePath(bestPath, grid.NodeFromWorldPoint(startPos), grid.NodeFromWorldPoint(targetPos));
            yield return new WaitForSeconds(0.5f);

            yield return StartCoroutine(MoveSeeker(bestPath, targetPos));
        }

        yield return new WaitForSeconds(0.5f);
        RunNewPath();
    }

    void VisualizeAltPath(List<AstarNode> path, AstarNode startNode, AstarNode targetNode)
    {
        Color brown = new Color(0.6f, 0.3f, 0f);
        for (int i = 0; i < path.Count; i++)
        {
            AstarNode node = path[i];
            if (node == startNode || node == targetNode) continue;
            grid.ColorTile(node.gridX, node.gridY, brown);
        }
    }

    void VisualizePath(List<AstarNode> path, AstarNode startNode, AstarNode targetNode)
    {
        grid.ColorTile(startNode.gridX,  startNode.gridY,  Color.green);
        grid.ColorTile(targetNode.gridX, targetNode.gridY, Color.magenta);

        for (int i = 0; i < path.Count; i++)
        {
            AstarNode node = path[i];
            if (node == startNode || node == targetNode) continue;

            bool isPortalEntry = i < path.Count - 1 &&
                                 (Mathf.Abs(path[i].gridX - path[i + 1].gridX) > 1 ||
                                  Mathf.Abs(path[i].gridY - path[i + 1].gridY) > 1);

            grid.ColorTile(node.gridX, node.gridY, isPortalEntry ? new Color(0.6f, 0f, 1f) : Color.yellow);
        }

        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            lineRenderer.SetPosition(i, new Vector3(path[i].worldPosition.x, 0.5f, path[i].worldPosition.z));
    }

    // ── MoveSeeker med automatisk teleportering och omplanering ──────────────
    IEnumerator MoveSeeker(List<AstarNode> path, Vector3 targetPos)
    {
        var portalConnections = portalManager.GetPortalConnections();
        var portalIndices     = portalManager.GetPortalIndices();
        int unplannedThisEpisode = 0;

        for (int i = 0; i < path.Count; i++)
        {
            AstarNode currentNode = path[i];

            // ── Planerad teleportering (hopp i vägen) ──────────────────────
            if (i < path.Count - 1)
            {
                int dx = Mathf.Abs(path[i].gridX - path[i + 1].gridX);
                int dy = Mathf.Abs(path[i].gridY - path[i + 1].gridY);
                if (dx > 1 || dy > 1)
                {
                    Vector3 entryPos = new Vector3(path[i].worldPosition.x, seeker.position.y, path[i].worldPosition.z);
                    while (Vector3.Distance(seeker.position, entryPos) > 0.05f)
                    {
                        seeker.position = Vector3.MoveTowards(seeker.position, entryPos, seekerSpeed * Time.deltaTime);
                        yield return null;
                    }
                    seeker.position = new Vector3(path[i + 1].worldPosition.x, seeker.position.y, path[i + 1].worldPosition.z);
                    i++;
                    continue;
                }
            }

            // ── Rörelse mot nästa nod ──────────────────────────────────────
            Vector3 nodePos = new Vector3(currentNode.worldPosition.x, seeker.position.y, currentNode.worldPosition.z);
            while (Vector3.Distance(seeker.position, nodePos) > 0.05f)
            {
                seeker.position = Vector3.MoveTowards(seeker.position, nodePos, seekerSpeed * Time.deltaTime);
                yield return null;
            }
            seeker.position = nodePos;

            // ── Kolla om vi råkat stå på en portalingång (oplanerat) ────────
            // Oplanerat = noden är en portalingång men nästa nod i vägen är INTE exitnoden
            if (portalConnections.ContainsKey(currentNode))
            {
                AstarNode exitNode = portalConnections[currentNode];
                bool plannedTeleport = i < path.Count - 1 &&
                                       path[i + 1] == exitNode;

                if (!plannedTeleport)
                {
                    // Oplanerad teleportering!
                    unplannedThisEpisode++;
                    _totalUnplannedTeleports++;
                    Vector3 exitPos = new Vector3(exitNode.worldPosition.x, seeker.position.y, exitNode.worldPosition.z);
                    seeker.position = exitPos;

                    Debug.Log($"[A* Episode {_episode}] OPLANERAD TELEPORT #{unplannedThisEpisode} " +
                              $"från ({currentNode.gridX},{currentNode.gridY}) " +
                              $"till ({exitNode.gridX},{exitNode.gridY}) — räknar om väg");

                    // Räkna om bästa väg från ny position
                    var (newPath, newCost, newUsedPortal, newUsedPortalIdx,
                         newDirectPath, newBestPortalPath, newDirectCost, newBestPortalCost, newBestPortalIndex)
                        = ComputeBestPath(seeker.position, targetPos);

                    if (newPath != null)
                    {
                        // Uppdatera visualisering
                        grid.ResetTileColors();
                        List<AstarNode> newAlt = newUsedPortal ? newDirectPath : newBestPortalPath;
                        AstarNode newStartNode = grid.NodeFromWorldPoint(seeker.position);
                        AstarNode newTargetNode = grid.NodeFromWorldPoint(targetPos);

                        if (newAlt != null)
                            VisualizeAltPath(newAlt, newStartNode, newTargetNode);
                        VisualizePath(newPath, newStartNode, newTargetNode);

                        // Fortsätt med den nya vägen
                        path = newPath;
                        i = -1; // reset loop, börjar om från index 0
                    }
                    continue;
                }
            }
        }

        if (unplannedThisEpisode > 0)
            Debug.Log($"[A* Episode {_episode}] Episoden avslutad med {unplannedThisEpisode} oplanerade teleporter");
    }

    int GetDistance(AstarNode nodeA, AstarNode nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
}
