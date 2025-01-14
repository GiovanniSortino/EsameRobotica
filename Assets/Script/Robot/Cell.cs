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
    public override string ToString(){
        return $"Cell({x}, {y})";
    }
    
    public override bool Equals(object obj){
        if (obj == null || GetType() != obj.GetType()) return false;
        Cell other = (Cell)obj;
        return x == other.x && y == other.y;
    }

    public override int GetHashCode(){
        unchecked{
            int hash = 17;
            hash = hash * 23 + x.GetHashCode();
            hash = hash * 23 + y.GetHashCode();
            return hash;
        }
    }
}