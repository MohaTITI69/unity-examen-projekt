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
    Spawner spawner;
 
    // ── Stats matching TurtleAgent ────────────────────────────
    private int _currentRun = 0;
    private int _totalRuns = 0;
    private int _totalGoalReached = 0;
    private int _totalRunsWithPortalOpportunity = 0;
    private int _totalPortalUsed = 0;
    private int _totalGoalReachedViaOptimalPortal = 0;
 
    private bool _usedPortal = false;
    private bool _usedOptimalPortal = false;
    private int _optimalPortalIndex = -1;
    private float _directDistAtStart;
    private float _optimalPortalCostAtStart;
    // ─────────────────────────────────────────────────────────
 
    void Awake() {
        grid = FindFirstObjectByType<AstarGrid>();
        portalManager = FindFirstObjectByType<PortalManagerAstar>();
        spawner = FindFirstObjectByType<Spawner>();
    }
 
    void Start() {
        GameObject lineObj = new GameObject("PathLine");
        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth     = 0.3f;
        lineRenderer.endWidth       = 0.3f;
        lineRenderer.material       = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor     = Color.yellow;
        lineRenderer.endColor       = Color.yellow;
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
            StopAllCoroutines();
            lineRenderer.positionCount = 0;
            grid.ResetTileColors();
            RunNewPath();
        }
    }
 
    void RunNewPath() {
        _currentRun++;
        _usedPortal = false;
        _usedOptimalPortal = false;
 
        // Spawn first, then log and pathfind with correct positions
        spawner?.RespawnBoth();
 
        _directDistAtStart        = Vector3.Distance(seeker.position, target.position);
        _optimalPortalIndex       = GetOptimalPortalIndex();
        _optimalPortalCostAtStart = GetOptimalPortalCost();
 
        if (_optimalPortalIndex >= 0)
            _totalRunsWithPortalOpportunity++;
 
        Debug.Log($"[A* Run {_currentRun}] START — " +
                  $"agent={seeker.position:F1}  goal={target.position:F1}  " +
                  $"directDist={_directDistAtStart:F1}  " +
                  $"portalOptimal={(_optimalPortalIndex >= 0 ? "YES cost=" + _optimalPortalCostAtStart.ToString("F1") : "NO")}");
 
        grid.ResetTileColors();
        lineRenderer.positionCount = 0;
        StartCoroutine(FindPathVisual(seeker.position, target.position));
    }
 
    IEnumerator FindPathVisual(Vector3 startPos, Vector3 targetPos) {
        AstarNode startNode  = grid.NodeFromWorldPoint(startPos);
        AstarNode targetNode = grid.NodeFromWorldPoint(targetPos);
 
        Heap<AstarNode>    openSet   = new Heap<AstarNode>(grid.MaxSize);
        HashSet<AstarNode> closedSet = new HashSet<AstarNode>();
        openSet.Add(startNode);
 
        grid.ColorTile(startNode.gridX,  startNode.gridY,  Color.green);
        grid.ColorTile(targetNode.gridX, targetNode.gridY, Color.magenta);
 
        int steps = 0, iterations = 0;
 
        while (openSet.Count > 0) {
            AstarNode currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);
            iterations++;
 
            if (currentNode != startNode && currentNode != targetNode)
                grid.ColorTile(currentNode.gridX, currentNode.gridY, Color.red);
 
            if (currentNode == targetNode) {
                RetracePath(startNode, targetNode, iterations);
                yield break;
            }
 
            foreach (AstarNode neighbour in grid.GetNeighbours(currentNode)) {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;
 
                int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newCost < neighbour.gCost || !openSet.Contains(neighbour)) {
                    neighbour.gCost  = newCost;
                    neighbour.hCost  = GetHeuristic(neighbour, targetNode);
                    neighbour.parent = currentNode;
 
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
            if (steps >= stepsPerFrame) { steps = 0; yield return null; }
        }
 
        Debug.LogError($"[A* Run {_currentRun}] No path found after {iterations} iterations!");
    }
 
    void RetracePath(AstarNode startNode, AstarNode endNode, int iterations) {
        List<AstarNode> path = new List<AstarNode>();
        AstarNode current = endNode;
        while (current != startNode) { path.Add(current); current = current.parent; }
        path.Add(startNode);
        path.Reverse();
 
        int portalUsedIndex = -1;
        for (int i = 1; i < path.Count; i++) {
            int dx = Mathf.Abs(path[i].gridX - path[i-1].gridX);
            int dy = Mathf.Abs(path[i].gridY - path[i-1].gridY);
            if (Mathf.Max(dx, dy) > 2) {
                _usedPortal = true;
                portalUsedIndex = GetPortalIndexForJump(path[i-1], path[i]);
                break;
            }
        }
 
        if (_usedPortal) {
            _usedOptimalPortal = (portalUsedIndex == _optimalPortalIndex);
            _totalPortalUsed++;
            Debug.Log($"[A* Run {_currentRun}] PORTAL USED — " +
                      $"portalIndex={portalUsedIndex}  optimalIndex={_optimalPortalIndex}  " +
                      $"wasOptimal={_usedOptimalPortal}");
        }
 
        foreach (AstarNode node in path)
            grid.ColorTile(node.gridX, node.gridY, Color.yellow);
 
        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            lineRenderer.SetPosition(i, new Vector3(path[i].worldPosition.x, 0.5f, path[i].worldPosition.z));
 
        StartCoroutine(MoveSeeker(path, iterations));
    }
 
    IEnumerator MoveSeeker(List<AstarNode> path, int iterations) {
        // Disable portal colliders so seeker movement never triggers OnTriggerEnter
        portalManager?.SetPortalsActive(false);
 
        for (int i = 0; i < path.Count; i++) {
            AstarNode node = path[i];
            bool isPortalJump = false;
            Vector3 portalExitWorld = Vector3.zero;
 
            if (i > 0) {
                int dx = Mathf.Abs(path[i].gridX - path[i-1].gridX);
                int dy = Mathf.Abs(path[i].gridY - path[i-1].gridY);
                if (Mathf.Max(dx, dy) > 2) {
                    isPortalJump    = true;
                    portalExitWorld = GetPortalExitWorld(path[i-1], path[i]);
                }
            }
 
            if (isPortalJump) {
                // Walk to portal entry
                Vector3 entryPos = new Vector3(path[i-1].worldPosition.x, seeker.position.y, path[i-1].worldPosition.z);
                while (Vector3.Distance(seeker.position, entryPos) > 0.05f) {
                    seeker.position = Vector3.MoveTowards(seeker.position, entryPos, seekerSpeed * Time.deltaTime);
                    yield return null;
                }
                // Teleport to exact exit
                seeker.position = new Vector3(portalExitWorld.x, seeker.position.y, portalExitWorld.z);
                yield return null;
                continue;
            }
 
            Vector3 targetPos = new Vector3(node.worldPosition.x, seeker.position.y, node.worldPosition.z);
            while (Vector3.Distance(seeker.position, targetPos) > 0.05f) {
                seeker.position = Vector3.MoveTowards(seeker.position, targetPos, seekerSpeed * Time.deltaTime);
                yield return null;
            }
            seeker.position = targetPos;
        }
 
        // Re-enable portal colliders
        portalManager?.SetPortalsActive(true);
 
        _totalRuns++;
        _totalGoalReached++;
        if (_usedOptimalPortal) _totalGoalReachedViaOptimalPortal++;
 
        float portalUsageRate = _totalRunsWithPortalOpportunity > 0
            ? (float)_totalPortalUsed / _totalRunsWithPortalOpportunity * 100f : 0f;
        float optimalPortalRate = _totalRunsWithPortalOpportunity > 0
            ? (float)_totalGoalReachedViaOptimalPortal / _totalRunsWithPortalOpportunity * 100f : 0f;
 
        Debug.Log($"[A* Run {_currentRun}] GOAL REACHED — " +
                  $"iterations={iterations}  portalUsed={_usedPortal}  " +
                  $"portalWasOptimal={(_optimalPortalIndex >= 0 ? "YES" : "NO")}  " +
                  $"usedOptimalPortal={_usedOptimalPortal}");
 
        if (_currentRun % 10 == 0) {
            Debug.Log("╔══════════════════════════════════════════════════════╗");
            Debug.Log("║           A* THESIS STATS (running average)          ║");
            Debug.Log("╠══════════════════════════════════════════════════════╣");
            Debug.Log($"║  Runs completed:                  {_currentRun,-22}║");
            Debug.Log($"║  Goals reached:                   {_totalGoalReached,-22}║");
            Debug.Log($"║  Runs w/ portal opportunity:      {_totalRunsWithPortalOpportunity,-22}║");
            Debug.Log($"║  Portal used (of opportunities):  {portalUsageRate:F1}%{"",-18}║");
            Debug.Log($"║  Optimal portal + goal reached:   {optimalPortalRate:F1}%{"",-18}║");
            Debug.Log("╚══════════════════════════════════════════════════════╝");
        }
 
        yield return new WaitForSeconds(0.5f);
        RunNewPath();
    }
 
    // ── Helpers ───────────────────────────────────────────────────────
 
    int GetPortalIndexForJump(AstarNode from, AstarNode to) {
        if (portalManager == null) return -1;
        PortalPairAstar[] pairs = portalManager.GetPortalPairs();
        for (int i = 0; i < pairs.Length; i++) {
            AstarNode entryA = grid.NodeFromWorldPoint(pairs[i].portalA.position);
            AstarNode entryB = grid.NodeFromWorldPoint(pairs[i].portalB.position);
            if ((from.gridX == entryA.gridX && from.gridY == entryA.gridY) ||
                (from.gridX == entryB.gridX && from.gridY == entryB.gridY))
                return i;
        }
        return -1;
    }
 
    Vector3 GetPortalExitWorld(AstarNode from, AstarNode to) {
        if (portalManager == null) return to.worldPosition;
        PortalPairAstar[] pairs = portalManager.GetPortalPairs();
        for (int i = 0; i < pairs.Length; i++) {
            AstarNode entryA = grid.NodeFromWorldPoint(pairs[i].portalA.position);
            AstarNode entryB = grid.NodeFromWorldPoint(pairs[i].portalB.position);
            if (from.gridX == entryA.gridX && from.gridY == entryA.gridY) return pairs[i].exitB.position;
            if (from.gridX == entryB.gridX && from.gridY == entryB.gridY) return pairs[i].exitA.position;
        }
        return to.worldPosition;
    }
 
    int GetOptimalPortalIndex() {
        if (portalManager == null) return -1;
        float directDist = Vector3.Distance(seeker.position, target.position);
        float best = directDist;
        int bestIndex = -1;
        PortalPairAstar[] pairs = portalManager.GetPortalPairs();
        for (int i = 0; i < pairs.Length; i++) {
            float viaA = Vector3.Distance(seeker.position, pairs[i].portalA.position)
                       + Vector3.Distance(pairs[i].exitB.position, target.position);
            float viaB = Vector3.Distance(seeker.position, pairs[i].portalB.position)
                       + Vector3.Distance(pairs[i].exitA.position, target.position);
            float bestPair = Mathf.Min(viaA, viaB);
            if (bestPair < best) { best = bestPair; bestIndex = i; }
        }
        return bestIndex;
    }
 
    float GetOptimalPortalCost() {
        if (_optimalPortalIndex < 0 || portalManager == null) return _directDistAtStart;
        PortalPairAstar pair = portalManager.GetPortalPairs()[_optimalPortalIndex];
        float viaA = Vector3.Distance(seeker.position, pair.portalA.position)
                   + Vector3.Distance(pair.exitB.position, target.position);
        float viaB = Vector3.Distance(seeker.position, pair.portalB.position)
                   + Vector3.Distance(pair.exitA.position, target.position);
        return Mathf.Min(viaA, viaB);
    }
 
    int GetDistance(AstarNode a, AstarNode b) {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);
        if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
 
    int GetHeuristic(AstarNode from, AstarNode targetNode) {
        int best = GetDistance(from, targetNode);
        if (portalManager != null) {
            foreach (PortalPairAstar pair in portalManager.GetPortalPairs()) {
                AstarNode entryA = grid.NodeFromWorldPoint(pair.portalA.position);
                AstarNode exitB  = grid.NodeFromWorldPoint(pair.exitB.position);
                int viaA = GetDistance(from, entryA) + GetDistance(exitB, targetNode);

                AstarNode entryB = grid.NodeFromWorldPoint(pair.portalB.position);
                AstarNode exitA  = grid.NodeFromWorldPoint(pair.exitA.position);
                int viaB = GetDistance(from, entryB) + GetDistance(exitA, targetNode);

                best = Mathf.Min(best, viaA, viaB);
            }
        }
        return best;
    }
}