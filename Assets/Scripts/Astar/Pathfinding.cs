
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 
public class Pathfinding : MonoBehaviour {
 
    public Transform seeker, target;
    public int stepsPerFrame = 5;
    public float seekerSpeed = 5;
 
    AstarGrid grid;
    LineRenderer lineRenderer;
 
    void Awake() {
        grid = FindFirstObjectByType<AstarGrid>();
    }
 
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
 
        Heap<AstarNode> openSet      = new Heap<AstarNode>(grid.MaxSize);
        HashSet<AstarNode> closedSet = new HashSet<AstarNode>();
        openSet.Add(startNode);
 
        grid.ColorTile(startNode.gridX,  startNode.gridY,  Color.green);
        grid.ColorTile(targetNode.gridX, targetNode.gridY, Color.magenta);
 
        int steps = 0;
 
        while (openSet.Count > 0) {
            AstarNode currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);
 
            if (currentNode != startNode && currentNode != targetNode)
                grid.ColorTile(currentNode.gridX, currentNode.gridY, Color.red);
 
            if (currentNode == targetNode) {
                RetracePath(startNode, targetNode);
                yield break;
            }
 
            foreach (AstarNode neighbour in grid.GetNeighbours(currentNode)) {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;
 
                int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newCost < neighbour.gCost || !openSet.Contains(neighbour)) {
                    neighbour.gCost  = newCost;
                    neighbour.hCost  = GetDistance(neighbour, targetNode);
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
            if (steps >= stepsPerFrame) {
                steps = 0;
                yield return null;
            }
        }
    }
 
    void RetracePath(AstarNode startNode, AstarNode endNode) {
        List<AstarNode> path = new List<AstarNode>();
        AstarNode currentNode = endNode;
 
        while (currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Add(startNode);
        path.Reverse();
 
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
        foreach (AstarNode node in path) {
            Vector3 targetPos = new Vector3(node.worldPosition.x, seeker.position.y, node.worldPosition.z);
 
            while (Vector3.Distance(seeker.position, targetPos) > 0.05f) {
                seeker.position = Vector3.MoveTowards(seeker.position, targetPos, seekerSpeed * Time.deltaTime);
                yield return null;
            }
            seeker.position = targetPos;
        }
        // Seeker has arrived. Press Space to run again.
    }
 
    int GetDistance(AstarNode nodeA, AstarNode nodeB) {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }


}
