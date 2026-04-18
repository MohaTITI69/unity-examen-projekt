using UnityEngine;
using System.Collections.Generic;
 
public class AstarGrid : MonoBehaviour {
 
    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    public Material tileMaterial;
 
    AstarNode[,] grid;
    GameObject[,] floorTiles;
 
    float nodeDiameter;
    int gridSizeX, gridSizeY;
 
    // ── Portal support ──────────────────────────────────────────────
    private PortalManagerAstar portalManager;
    // ────────────────────────────────────────────────────────────────
 
    public int MaxSize => gridSizeX * gridSizeY;
 
    void Start() {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
        SpawnFloorTiles();
 
        // ── Find portal manager after grid is built ──────────────────
        portalManager = FindFirstObjectByType<PortalManagerAstar>();
        // ────────────────────────────────────────────────────────────
    }
 
    void CreateGrid() {
        grid = new AstarNode[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position
            - Vector3.right   * gridWorldSize.x / 2
            - Vector3.forward * gridWorldSize.y / 2;
 
        for (int x = 0; x < gridSizeX; x++) {
            for (int y = 0; y < gridSizeY; y++) {
                Vector3 worldPoint = worldBottomLeft
                    + Vector3.right   * (x * nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);
                grid[x, y] = new AstarNode(walkable, worldPoint, x, y);
            }
        }
    }
 
    void SpawnFloorTiles() {
        floorTiles = new GameObject[gridSizeX, gridSizeY];
 
        for (int x = 0; x < gridSizeX; x++) {
            for (int y = 0; y < gridSizeY; y++) {
                AstarNode node = grid[x, y];
                if (!node.walkable) continue;
 
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name = $"Tile_{x}_{y}";
                tile.transform.position = new Vector3(node.worldPosition.x, 0.01f, node.worldPosition.z);
                tile.transform.rotation = Quaternion.Euler(90, 0, 0);
                tile.transform.localScale = Vector3.one * (nodeDiameter - 0.1f);
                Destroy(tile.GetComponent<MeshCollider>());
 
                Renderer rend = tile.GetComponent<Renderer>();
                if (tileMaterial != null)
                    rend.material = new Material(tileMaterial);
                else
                    rend.material = new Material(Shader.Find("Sprites/Default"));
 
                rend.material.color = Color.white;
                floorTiles[x, y] = tile;
            }
        }
    }
 
    public void ColorTile(int x, int y, Color color) {
        if (floorTiles == null) return;
        GameObject tile = floorTiles[x, y];
        if (tile != null)
            tile.GetComponent<Renderer>().material.color = color;
    }
 
    public void ResetTileColors() {
        if (floorTiles == null) return;
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                ColorTile(x, y, Color.white);
    }
 
    public List<AstarNode> GetWalkableNodes() {
        List<AstarNode> walkable = new List<AstarNode>();
        foreach (AstarNode n in grid)
            if (n.walkable)
                walkable.Add(n);
        return walkable;
    }
 
    public List<AstarNode> GetNeighbours(AstarNode node) {
        List<AstarNode> neighbours = new List<AstarNode>();
 
        // ── Standard 8-directional neighbours ───────────────────────
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0) continue;
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    neighbours.Add(grid[checkX, checkY]);
            }
        }
 
        // ── Portal edges: if this node is on a portal entrance, ──────
        // ── add the corresponding exit node as a neighbour.     ──────
        if (portalManager != null) {
            foreach (PortalPairAstar pair in portalManager.GetPortalPairs()) {
                // Node near portal A entrance → exit comes out at exitB
                if (Vector3.Distance(node.worldPosition, pair.portalA.position) < nodeDiameter * 2) {
                    AstarNode exitNode = NodeFromWorldPoint(pair.exitB.position);
                    if (exitNode.walkable)
                        neighbours.Add(exitNode);
                }
                // Node near portal B entrance → exit comes out at exitA
                else if (Vector3.Distance(node.worldPosition, pair.portalB.position) < nodeDiameter * 2) {
                    AstarNode exitNode = NodeFromWorldPoint(pair.exitA.position);
                    if (exitNode.walkable)
                        neighbours.Add(exitNode);
                }
            }
        }
        // ────────────────────────────────────────────────────────────
 
        return neighbours;
    }
 
    public AstarNode NodeFromWorldPoint(Vector3 worldPosition) {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }
 
    void OnDrawGizmos() {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
    }
}
 