using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;



public class MyGrid : MonoBehaviour
{
    public Vector2 gridWorldSize;
    public float nodeRadius;
    public LayerMask unWalkableMask; 

    public bool showGrid = true;
    Node[,] grid;
    float nodeDiameter; 
    int gridSizeX;
    int gridSizeY;


    void Start()
    {
        nodeDiameter = nodeRadius*2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        createGrid();
         
    }

    void createGrid()
    {
        grid = new Node[gridSizeX,gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x /2 - Vector3.forward * gridWorldSize.y /2;

        for(int x = 0; x < gridSizeX; x++)
        {
            for(int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * ( y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint,nodeRadius,unWalkableMask));
                grid[x,y] = new Node(walkable,worldPoint);
            }
        }
    }


    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float precentX = ((worldPosition.x - transform.position.x) + gridWorldSize.x/2) / gridWorldSize.x;
        float precentY = ((worldPosition.z - transform.position.z) + gridWorldSize.y/2) / gridWorldSize.y;
        precentX = Mathf.Clamp01(precentX);
        precentY = Mathf.Clamp01(precentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * precentX); 
        int y = Mathf.RoundToInt((gridSizeY - 1) * precentY);
        return grid[x,y];
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid != null && showGrid)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = (n.walkable) ? Color.white : Color.red;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }
    }


        public List<Node> GetWalkableNodes()
    {
        List<Node> walkableNodes = new List<Node>();
        foreach (Node n in grid)
        {
            if (n.walkable)
                walkableNodes.Add(n);
        }
        return walkableNodes;
    }

}