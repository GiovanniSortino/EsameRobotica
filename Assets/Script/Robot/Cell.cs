using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;

public class Cell{
    public int x, y;
    public float gCost;
    public float hCost;
    public float fCost => gCost + hCost;
    public Cell parent;

    public Cell(int x, int y){
        this.x = x;
        this.y = y;
    }

    public Cell(Vector2Int cell){
        x = cell.x;
        y = cell.y;
    }
}