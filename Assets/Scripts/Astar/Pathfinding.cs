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

    // ── Step-count tracking (for median over 100 episodes) ────────────────────
    // Stores total steps (cardinal + diagonal + portal jumps) per episode.
    private List<int> _episodeStepCounts = new List<int>();

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

        // Diagnostic — remove once portal walkability is confirmed.
        foreach (var kvp in portalManager.GetPortalConnections())
            Debug.Log($"[Portal] entry ({kvp.Key.gridX},{kvp.Key.gridY}) walkable={kvp.Key.walkable}  →  exit ({kvp.Value.gridX},{kvp.Value.gridY}) walkable={kvp.Value.walkable}");

        _episode++;
        StartCoroutine(FindBestPath(seeker.position, target.position));
    }

    // ── Neighbours seen by the portal-aware A* search ────────────────────────
    // For a portal ENTRY node, return ONLY the exit — no spatial neighbours.
    // If we returned 8 spatial + exit, A* could route through the entry tile as a
    // normal waypoint (e.g. [..., (9,3), (10,3), (11,3), ...]). That path has no
    // non-adjacent step, so MoveSeeker's planned-teleport block never fires, the
    // unplanned fallback snaps the seeker to the exit, re-plans, gets the same
    // spatial pass-through again → infinite loop. Returning only the exit forces
    // A* to either route AROUND the entry or COMMIT to the portal jump, so every
    // portal hop in the returned path is always a non-adjacent step that the
    // planned-teleport block in MoveSeeker correctly handles.
    List<AstarNode> GetNeighboursWithPortals(AstarNode node, Dictionary<AstarNode, AstarNode> portalConnections)
    {
        if (portalConnections.TryGetValue(node, out AstarNode exitNode))
            return new List<AstarNode> { exitNode };

        return grid.GetNeighbours(node);
    }

    // ── Fully admissible heuristic for portal-aware search ───────────────────
    // For every node n, the true cost to goal is at most:
    //   min( GetDistance(n, goal),
    //        min over all portals: GetDistance(n, entry) + GetDistance(exit, goal) )
    // The old version only corrected the heuristic AT the entry node itself.
    // Nodes on the approach path to an entry still had h = GetDistance(n, goal),
    // which overestimates when the portal shortcut is cheaper — causing A* to
    // deprioritise the portal route and potentially return a suboptimal direct path.
    // Iterating all portals is O(P) per node; with a small fixed portal count this
    // is negligible.
    int GetHeuristic(AstarNode node, AstarNode targetNode, Dictionary<AstarNode, AstarNode> portalConnections)
    {
        int h = GetDistance(node, targetNode);
        foreach (var kvp in portalConnections)
        {
            int viaPortal = GetDistance(node, kvp.Key) + GetDistance(kvp.Value, targetNode);
            if (viaPortal < h) h = viaPortal;
        }
        return h;
    }

    // ── Unified portal-aware A* — ONE call finds the globally optimal path ───
    // Portal edges (entry → exit) cost 0; all other edges use standard octile cost.
    // Because the search graph includes portal edges, chained portals are handled
    // naturally: A* simply keeps expanding through them if that minimises total cost.
    List<AstarNode> PortalAwareStar(AstarNode startNode, AstarNode targetNode,
                                     Dictionary<AstarNode, AstarNode> portalConnections)
    {
        if (startNode == targetNode) return new List<AstarNode> { startNode };

        grid.ResetNodes();

        Heap<AstarNode>    openSet   = new Heap<AstarNode>(grid.MaxSize);
        HashSet<AstarNode> closedSet = new HashSet<AstarNode>();

        startNode.gCost = 0;
        startNode.hCost = GetHeuristic(startNode, targetNode, portalConnections);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            AstarNode current = openSet.RemoveFirst();
            closedSet.Add(current);

            if (current == targetNode)
                return RetraceNodes(startNode, targetNode);

            // Look up once per expanded node to avoid repeated dict lookups in the inner loop.
            portalConnections.TryGetValue(current, out AstarNode portalExitFromCurrent);

            foreach (AstarNode neighbour in GetNeighboursWithPortals(current, portalConnections))
            {
                // Portal exits are teleport destinations, not traversed tiles — skip the
                // walkability check for them so an unwalkably-placed exit doesn't silently
                // block the portal edge. Spatial neighbours still require walkable=true.
                bool isPortalExit = portalExitFromCurrent != null && portalExitFromCurrent == neighbour;
                if ((!neighbour.walkable && !isPortalExit) || closedSet.Contains(neighbour)) continue;

                // Portal edge costs 0; all spatial edges use octile distance.
                int edgeCost = (portalExitFromCurrent != null && portalExitFromCurrent == neighbour)
                    ? 0
                    : GetDistance(current, neighbour);

                int newCost = current.gCost + edgeCost;
                if (!openSet.Contains(neighbour) || newCost < neighbour.gCost)
                {
                    neighbour.gCost  = newCost;
                    neighbour.hCost  = GetHeuristic(neighbour, targetNode, portalConnections);
                    neighbour.parent = current;

                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                    else                               openSet.UpdateItem(neighbour);
                }
            }
        }
        return null;
    }

    // ── Plain A* with no portal knowledge — used only to compute directCost for stats ──
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

    // ── Scan a path for the first non-adjacent step (= portal jump) and return its index ──
    // Returns -1 if the path contains no portal hops.
    int DetectPortalIndex(List<AstarNode> path, Dictionary<AstarNode, int> portalIndices)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            int dx = Mathf.Abs(path[i].gridX - path[i + 1].gridX);
            int dy = Mathf.Abs(path[i].gridY - path[i + 1].gridY);
            if (dx > 1 || dy > 1)
            {
                if (portalIndices.TryGetValue(path[i], out int idx))
                    return idx;
            }
        }
        return -1;
    }

    // ── Single unified search (2 A* calls total vs. old 1 + 2·P) ────────────
    // Call 1 — PortalAwareStar: finds the globally optimal path (including portal hops).
    // Call 2 — SilentAstar: portal-unaware direct path, used only for thesis stats.
    //
    // bestPortalCost equals bestCost when the portal route wins; it is int.MaxValue
    // when the direct route wins (the cost of a losing portal alternative is not
    // computed in this design — document as a known stat limitation).
    (List<AstarNode> bestPath, int bestCost, bool usedPortal, int usedPortalIdx,
     List<AstarNode> directPath, int directCost, int bestPortalCost)
    ComputeBestPath(Vector3 fromPos, Vector3 toPos)
    {
        var portalConnections = portalManager.GetPortalConnections();
        var portalIndices     = portalManager.GetPortalIndices();

        AstarNode startNode  = grid.NodeFromWorldPoint(fromPos);
        AstarNode targetNode = grid.NodeFromWorldPoint(toPos);

        // Primary search — gCost on returned nodes is valid until SilentAstar resets them.
        List<AstarNode> bestPath = PortalAwareStar(startNode, targetNode, portalConnections);
        int             bestCost = bestPath != null ? bestPath[bestPath.Count - 1].gCost : int.MaxValue;

        // Stats search — resets all node costs, so bestCost must be captured above first.
        List<AstarNode> directPath = SilentAstar(startNode, targetNode);
        int             directCost = directPath != null ? directPath[directPath.Count - 1].gCost : int.MaxValue;

        int  usedPortalIdx  = DetectPortalIndex(bestPath ?? new List<AstarNode>(), portalIndices);
        bool usedPortal     = usedPortalIdx >= 0;
        int  bestPortalCost = usedPortal ? bestCost : int.MaxValue;

        return (bestPath, bestCost, usedPortal, usedPortalIdx, directPath, directCost, bestPortalCost);
    }

    // ── Huvudsökning ─────────────────────────────────────────────────────────
    IEnumerator FindBestPath(Vector3 startPos, Vector3 targetPos)
    {
        var (bestPath, bestCost, usedPortal, usedPortalIdx,
             directPath, directCost, bestPortalCost)
            = ComputeBestPath(startPos, targetPos);

        // When portal route wins, show direct as the alternative (brown tiles).
        // When direct route wins, no portal path was computed separately, so no alt overlay.
        List<AstarNode> altPath = usedPortal ? directPath : null;

        bool portalOpportunity = bestPortalCost < directCost;
        if (portalOpportunity) _totalEpisodesWithPortal++;
        if (usedPortal)        _totalEpisodesPortalChosen++;

        // Count steps in the chosen path and store for median.
        var (cardinalSteps, diagonalSteps, portalSteps) =
            bestPath != null ? CountPathSteps(bestPath) : (0, 0, 0);
        int totalSteps = cardinalSteps + diagonalSteps + portalSteps;
        _episodeStepCounts.Add(totalSteps);

        string chosenRoute = usedPortal ? $"PORTAL (index {usedPortalIdx})" : "DIREKT";

        Debug.Log($"[A* Episode {_episode}] " +
                  $"direktKostnad={directCost}  " +
                  $"bästaPortalKostnad={(usedPortal ? bestPortalCost + $" (portal {usedPortalIdx})" : "N/A")}  " +
                  $"portalSnabbare={portalOpportunity}  " +
                  $"valdVäg={chosenRoute}  " +
                  $"valdKostnad={bestCost}  " +
                  $"steg={totalSteps} (raka={cardinalSteps} diag={diagonalSteps} portal={portalSteps})");

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

        if (_episode % 100 == 0)
        {
            // Take only the most recent 100 episodes for the median window.
            int windowStart = _episodeStepCounts.Count - 100;
            List<int> window = _episodeStepCounts.GetRange(windowStart, 100);
            float median = ComputeMedian(window);

            Debug.Log("╔══════════════════════════════════════════════════════╗");
            Debug.Log("║           STEGSTATISTIK (senaste 100 episodes)       ║");
            Debug.Log("╠══════════════════════════════════════════════════════╣");
            Debug.Log($"║  Episodes i fönster:              {100,-22}║");
            Debug.Log($"║  Median antal steg:               {median,-22:F1}║");
            Debug.Log($"║  (raka + diagonala + portalhop)                     ║");
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

            // Portal-entry tile: the next step is non-adjacent (grid jump), colour it purple.
            // This check works unchanged because PortalAwareStar still produces entry→exit
            // as consecutive non-adjacent nodes in the path, just like the old design.
            bool isPortalEntry = i < path.Count - 1 &&
                                 (Mathf.Abs(path[i].gridX - path[i + 1].gridX) > 1 ||
                                  Mathf.Abs(path[i].gridY - path[i + 1].gridY) > 1);

            grid.ColorTile(node.gridX, node.gridY, isPortalEntry ? new Color(0.6f, 0f, 1f) : Color.yellow);
        }

        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            lineRenderer.SetPosition(i, new Vector3(path[i].worldPosition.x, 0.5f, path[i].worldPosition.z));
    }

    // ── MoveSeeker med automatisk teleportering ───────────────────────────────
    IEnumerator MoveSeeker(List<AstarNode> path, Vector3 targetPos)
    {
        var portalConnections = portalManager.GetPortalConnections();
        var portalIndices     = portalManager.GetPortalIndices();
        int unplannedThisEpisode = 0;

        for (int i = 0; i < path.Count; i++)
        {
            AstarNode currentNode = path[i];

            // ── Planned portal jump (non-adjacent step in path) ────────────────
            // PortalAwareStar always places entry and exit as consecutive nodes,
            // so this block handles every intentional portal hop.
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

            // ── Rörelse mot nästa nod ──────────────────────────────────────────
            Vector3 nodePos = new Vector3(currentNode.worldPosition.x, seeker.position.y, currentNode.worldPosition.z);
            while (Vector3.Distance(seeker.position, nodePos) > 0.05f)
            {
                seeker.position = Vector3.MoveTowards(seeker.position, nodePos, seekerSpeed * Time.deltaTime);
                yield return null;
            }
            seeker.position = nodePos;

            // ── Unplanned-teleport safety fallback ────────────────────────────
            // With portal-aware A*, every portal hop is a planned jump handled above,
            // so this block is DEAD CODE in normal operation. It is kept as a safety
            // net for edge cases (e.g. floating-point snap placing the seeker on a
            // portal tile outside of a planned hop). Remove only after extended testing.
            if (portalConnections.ContainsKey(currentNode))
            {
                AstarNode exitNode = portalConnections[currentNode];
                bool plannedTeleport = i < path.Count - 1 && path[i + 1] == exitNode;

                if (!plannedTeleport)
                {
                    unplannedThisEpisode++;
                    _totalUnplannedTeleports++;
                    Vector3 exitPos = new Vector3(exitNode.worldPosition.x, seeker.position.y, exitNode.worldPosition.z);
                    seeker.position = exitPos;

                    Debug.Log($"[A* Episode {_episode}] OPLANERAD TELEPORT #{unplannedThisEpisode} " +
                              $"från ({currentNode.gridX},{currentNode.gridY}) " +
                              $"till ({exitNode.gridX},{exitNode.gridY}) — räknar om väg");

                    var (newPath, newCost, newUsedPortal, newUsedPortalIdx,
                         newDirectPath, newDirectCost, newBestPortalCost)
                        = ComputeBestPath(seeker.position, targetPos);

                    if (newPath != null)
                    {
                        grid.ResetTileColors();
                        List<AstarNode> newAlt      = newUsedPortal ? newDirectPath : null;
                        AstarNode       newStartNode = grid.NodeFromWorldPoint(seeker.position);
                        AstarNode       newTargetNode = grid.NodeFromWorldPoint(targetPos);

                        if (newAlt != null)
                            VisualizeAltPath(newAlt, newStartNode, newTargetNode);
                        VisualizePath(newPath, newStartNode, newTargetNode);

                        path = newPath;
                        i = -1;
                    }
                    continue;
                }
            }
        }

        if (unplannedThisEpisode > 0)
            Debug.Log($"[A* Episode {_episode}] Episoden avslutad med {unplannedThisEpisode} oplanerade teleporter");
    }

    // ── Count cardinal, diagonal, and portal steps in a path ─────────────────
    (int cardinal, int diagonal, int portalJumps) CountPathSteps(List<AstarNode> path)
    {
        int cardinal = 0, diagonal = 0, portals = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            int dx = Mathf.Abs(path[i].gridX - path[i + 1].gridX);
            int dy = Mathf.Abs(path[i].gridY - path[i + 1].gridY);
            if      (dx > 1 || dy > 1)  portals++;
            else if (dx + dy == 1)       cardinal++;
            else                          diagonal++;
        }
        return (cardinal, diagonal, portals);
    }

    // ── Median of a list of ints ──────────────────────────────────────────────
    float ComputeMedian(List<int> values)
    {
        List<int> sorted = new List<int>(values);
        sorted.Sort();
        int n = sorted.Count;
        if (n % 2 == 1) return sorted[n / 2];
        return (sorted[n / 2 - 1] + sorted[n / 2]) / 2f;
    }

    int GetDistance(AstarNode nodeA, AstarNode nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
}
