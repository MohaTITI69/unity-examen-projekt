using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 
public class Pathfinding : MonoBehaviour {
 
    public Transform seeker, target;
    public int stepsPerFrame = 5;
    public float seekerSpeed = 5;
 
    AstarGrid grid;
    LineRenderer lineRenderer;
    PortalManagerAstar portalManager;
 
    // ── Bug fix: track which portal the seeker just exited ──────────
    private int lastUsedPortalIndex = -1;
    private Vector3 lastExitPosition = Vector3.zero;
    private const float reentryBlockDistance = 3f;
    // ────────────────────────────────────────────────────────────────
 
    void Awake() {
        grid = FindFirstObjectByType<AstarGrid>();
        portalManager = FindFirstObjectByType<PortalManagerAstar>();
 
        if (grid == null)
            Debug.LogError("[Pathfinding] AstarGrid NOT FOUND in scene!");
        else
            Debug.Log("[Pathfinding] AstarGrid found OK.");
 
        if (portalManager == null)
            Debug.LogWarning("[Pathfinding] PortalManagerAstar NOT FOUND — portals will be ignored.");
        else {
            PortalPairAstar[] pairs = portalManager.GetPortalPairs();
            Debug.Log($"[Pathfinding] PortalManager found with {pairs.Length} portal pair(s).");
            for (int i = 0; i < pairs.Length; i++) {
                PortalPairAstar p = pairs[i];
                Debug.Log($"[Pathfinding] Portal pair {i}: " +
                          $"A={p.portalA.position}  exitB={p.exitB.position} | " +
                          $"B={p.portalB.position}  exitA={p.exitA.position}");
            }
 
            // ── Bug fix: subscribe to teleport event to track exits ──
            foreach (PortalPairAstar pair in pairs)
                pair.OnTeleport += OnSeekerTeleported;
            // ────────────────────────────────────────────────────────
        }
    }
 
    // ── Bug fix: called when seeker physically teleports ────────────
    // Restarts pathfinding from the new exit position so the seeker
    // doesn't walk through walls trying to follow the old path.
    void OnSeekerTeleported(int portalIndex, Transform obj) {
        if (obj != seeker) return;
 
        lastUsedPortalIndex = portalIndex;
        lastExitPosition = obj.position;
 
        Debug.Log($"[Pathfinding] Seeker teleported via portal {portalIndex}. Restarting path from exit {obj.position}");
 
        StopAllCoroutines();
        lineRenderer.positionCount = 0;
        grid.ResetTileColors();
        StartCoroutine(FindPathVisual(seeker.position, target.position));
    }
    // ────────────────────────────────────────────────────────────────
 
    void Start() {
        GameObject lineObj = new GameObject("PathLine");
        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth  = 0.3f;
        lineRenderer.endWidth    = 0.3f;
        lineRenderer.material    = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor  = Color.yellow;
        lineRenderer.endColor    = Color.yellow;
        lineRenderer.useWorldSpace  = true;
        lineRenderer.positionCount  = 0;
 
        StartCoroutine(DelayedStart());
    }
 
    IEnumerator DelayedStart() {
        yield return null;
        RunNewPath();
    }
 
    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("[Pathfinding] Space pressed — rerunning pathfind.");
            StopAllCoroutines();
            lineRenderer.positionCount = 0;
            grid.ResetTileColors();
            RunNewPath();
        }
    }
 
    void RunNewPath() {
        grid.ResetTileColors();
        lineRenderer.positionCount = 0;
        StartCoroutine(FindPathVisual(seeker.position, target.position));
    }
 
    IEnumerator FindPathVisual(Vector3 startPos, Vector3 targetPos) {
 
        AstarNode startNode  = grid.NodeFromWorldPoint(startPos);
        AstarNode targetNode = grid.NodeFromWorldPoint(targetPos);
 
        Debug.Log($"[Pathfinding] === NEW PATHFIND ===");
        Debug.Log($"[Pathfinding] Seeker world pos: {startPos}  → grid node ({startNode.gridX},{startNode.gridY})  walkable={startNode.walkable}");
        Debug.Log($"[Pathfinding] Target world pos: {targetPos} → grid node ({targetNode.gridX},{targetNode.gridY})  walkable={targetNode.walkable}");
 
        if (portalManager != null) {
            foreach (PortalPairAstar pair in portalManager.GetPortalPairs()) {
                AstarNode entryA = grid.NodeFromWorldPoint(pair.portalA.position);
                AstarNode exitB  = grid.NodeFromWorldPoint(pair.exitB.position);
                AstarNode entryB = grid.NodeFromWorldPoint(pair.portalB.position);
                AstarNode exitA  = grid.NodeFromWorldPoint(pair.exitA.position);
 
                Debug.Log($"[Pathfinding] Portal A entrance grid node: ({entryA.gridX},{entryA.gridY})  exitB grid node: ({exitB.gridX},{exitB.gridY})");
                Debug.Log($"[Pathfinding] Portal B entrance grid node: ({entryB.gridX},{entryB.gridY})  exitA grid node: ({exitA.gridX},{exitA.gridY})");
 
                int directCost = GetDistance(startNode, targetNode);
                int viaA = GetDistance(startNode, entryA) + 1 + GetDistance(exitB, targetNode);
                int viaB = GetDistance(startNode, entryB) + 1 + GetDistance(exitA, targetNode);
                Debug.Log($"[Pathfinding] Heuristic from START: direct={directCost}  viaPortalA={viaA}  viaPortalB={viaB}");
 
                if (Mathf.Min(viaA, viaB) < directCost)
                    Debug.Log("[Pathfinding] ✔ Portal route looks CHEAPER than direct — A* should prefer it.");
                else
                    Debug.LogWarning("[Pathfinding] ✘ Direct route is cheaper than portal from start — A* will likely ignore the portal.");
            }
        }
 
        Heap<AstarNode> openSet      = new Heap<AstarNode>(grid.MaxSize);
        HashSet<AstarNode> closedSet = new HashSet<AstarNode>();
        openSet.Add(startNode);
 
        grid.ColorTile(startNode.gridX,  startNode.gridY,  Color.green);
        grid.ColorTile(targetNode.gridX, targetNode.gridY, Color.magenta);
 
        int steps      = 0;
        int iterations = 0;
 
        while (openSet.Count > 0) {
            AstarNode currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);
            iterations++;
 
            if (currentNode != startNode && currentNode != targetNode)
                grid.ColorTile(currentNode.gridX, currentNode.gridY, Color.red);
 
            if (currentNode == targetNode) {
                Debug.Log($"[Pathfinding] ✔ Target reached after {iterations} iterations (nodes expanded).");
                RetracePath(startNode, targetNode, iterations);
                yield break;
            }
 
            List<AstarNode> neighbours = grid.GetNeighbours(currentNode);
 
            foreach (AstarNode neighbour in neighbours) {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;
 
                int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);
 
                int gridDist = Mathf.Max(
                    Mathf.Abs(currentNode.gridX - neighbour.gridX),
                    Mathf.Abs(currentNode.gridY - neighbour.gridY));
                if (gridDist > 2)
                    Debug.Log($"[Pathfinding] *** PORTAL EDGE: ({currentNode.gridX},{currentNode.gridY}) → ({neighbour.gridX},{neighbour.gridY})  gridDist={gridDist}  newGCost={newCost}");
 
                if (newCost < neighbour.gCost || !openSet.Contains(neighbour)) {
                    neighbour.gCost  = newCost;
                    neighbour.hCost  = GetHeuristic(neighbour, targetNode);
                    neighbour.parent = currentNode;
 
                    if (gridDist > 2)
                        Debug.Log($"[Pathfinding] Portal exit node ({neighbour.gridX},{neighbour.gridY})  hCost={neighbour.hCost}  fCost={neighbour.fCost}");
 
                    if (!openSet.Contains(neighbour)) {
                        openSet.Add(neighbour);
                        if (neighbour != startNode && neighbour != targetNode)
                            grid.ColorTile(neighbour.gridX, neighbour.gridY, Color.cyan);
                    } else {
                        openSet.UpdateItem(neighbour);
                    }
                }
            }
 
            steps++;
            if (steps >= stepsPerFrame) {
                steps = 0;
                yield return null;
            }
        }
 
        Debug.LogError($"[Pathfinding] ✘ No path found after {iterations} iterations!");
    }
 
    void RetracePath(AstarNode startNode, AstarNode endNode, int iterations) {
        List<AstarNode> path = new List<AstarNode>();
        AstarNode currentNode = endNode;
 
        while (currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Add(startNode);
        path.Reverse();
 
        bool portalUsed = false;
        int portalStep = -1;
        for (int i = 1; i < path.Count; i++) {
            int dx = Mathf.Abs(path[i].gridX - path[i-1].gridX);
            int dy = Mathf.Abs(path[i].gridY - path[i-1].gridY);
            if (Mathf.Max(dx, dy) > 2) {
                Debug.Log($"[Pathfinding] *** PATH USES PORTAL at step {i}: ({path[i-1].gridX},{path[i-1].gridY}) → ({path[i].gridX},{path[i].gridY})");
                portalUsed = true;
                portalStep = i;
            }
        }
 
        // ── THESIS SUMMARY ───────────────────────────────────────────
        Debug.Log("╔══════════════════════════════════════════════════╗");
        Debug.Log("║           THESIS COMPARISON SUMMARY              ║");
        Debug.Log("╠══════════════════════════════════════════════════╣");
        Debug.Log($"║  Nodes explored (iterations):     {iterations,-18}║");
        Debug.Log($"║  Path length (nodes):             {path.Count,-18}║");
        Debug.Log($"║  Total path cost (gCost):         {endNode.gCost,-18}║");
        Debug.Log($"║  Portal used:                     {(portalUsed ? "YES (step " + portalStep + ")" : "NO"),-18}║");
        Debug.Log("╠══════════════════════════════════════════════════╣");
        Debug.Log("║  Compare these values with/without portal:       ║");
        Debug.Log("║  → fewer iterations = more efficient search      ║");
        Debug.Log("║  → lower gCost = shorter actual path             ║");
        Debug.Log("║  → fewer path nodes = more direct route          ║");
        Debug.Log("╚══════════════════════════════════════════════════╝");
        // ────────────────────────────────────────────────────────────
 
        foreach (AstarNode node in path)
            grid.ColorTile(node.gridX, node.gridY, Color.yellow);
 
        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++) {
            lineRenderer.SetPosition(i, new Vector3(
                path[i].worldPosition.x, 0.5f, path[i].worldPosition.z));
        }
 
        StartCoroutine(MoveSeeker(path));
    }
 
    IEnumerator MoveSeeker(List<AstarNode> path) {
        Debug.Log($"[Pathfinding] Seeker starting movement — {path.Count} nodes.");
 
        for (int i = 0; i < path.Count; i++) {
            AstarNode node = path[i];
            Vector3 targetPos = new Vector3(node.worldPosition.x, seeker.position.y, node.worldPosition.z);
 
            if (i > 0) {
                int dx = Mathf.Abs(path[i].gridX - path[i-1].gridX);
                int dy = Mathf.Abs(path[i].gridY - path[i-1].gridY);
                if (Mathf.Max(dx, dy) > 2)
                    Debug.Log($"[Pathfinding] Seeker PORTAL JUMP at step {i}: ({path[i-1].gridX},{path[i-1].gridY}) → ({path[i].gridX},{path[i].gridY})");
            }
 
            // ── Bug fix: skip MoveTowards for portal jump steps ──────
            // Instead of walking through walls to the exit node,
            // just snap the seeker there — the physical teleport is
            // handled by PortalPairAstar's OnTriggerEnter separately.
            int gdx = i > 0 ? Mathf.Abs(path[i].gridX - path[i-1].gridX) : 0;
            int gdy = i > 0 ? Mathf.Abs(path[i].gridY - path[i-1].gridY) : 0;
            if (Mathf.Max(gdx, gdy) > 2) {
                // This is a portal jump — snap directly, don't walk through walls
                seeker.position = targetPos;
                yield return null;
                continue;
            }
            // ────────────────────────────────────────────────────────
 
            while (Vector3.Distance(seeker.position, targetPos) > 0.05f) {
                seeker.position = Vector3.MoveTowards(seeker.position, targetPos, seekerSpeed * Time.deltaTime);
                yield return null;
            }
            seeker.position = targetPos;
        }
 
        Debug.Log("[Pathfinding] ✔ Seeker reached goal!");
    }
 
    int GetDistance(AstarNode nodeA, AstarNode nodeB) {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
 
    int GetHeuristic(AstarNode from, AstarNode target) {
        int best = GetDistance(from, target);
 
        if (portalManager != null) {
            foreach (PortalPairAstar pair in portalManager.GetPortalPairs()) {
                AstarNode entryA = grid.NodeFromWorldPoint(pair.portalA.position);
                AstarNode exitB  = grid.NodeFromWorldPoint(pair.exitB.position);
                int viaA = GetDistance(from, entryA) + 1 + GetDistance(exitB, target);
 
                AstarNode entryB = grid.NodeFromWorldPoint(pair.portalB.position);
                AstarNode exitA  = grid.NodeFromWorldPoint(pair.exitA.position);
                int viaB = GetDistance(from, entryB) + 1 + GetDistance(exitA, target);
 
                best = Mathf.Min(best, viaA, viaB);
            }
        }
 
        return best;
    }
}