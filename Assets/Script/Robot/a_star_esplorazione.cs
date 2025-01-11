using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting.ReorderableList.Element_Adder_Menu; // Usato per Vector2Int compatibile con unity

public class AStarExploration
{
    private int rows, cols;
    private bool flag = false;

    // Direzioni di movimento (su, giù, sinistra, destra)
    private Vector2Int[] directions = {
        new Vector2Int(0, 1), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };

    private bool PosizioneValida(Vector2Int pos, int[,] grid, string zone)
    {
        rows = grid.GetLength(0);
        cols = grid.GetLength(1);
        return pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols && grid[pos.x, pos.y] != 1 && FindZone(pos) == zone;
    }

    public List<Cell> EsploraZona(Vector2Int start, int[,] grid, string zone)
    {

        List<Cell> openList = new List<Cell>();
        HashSet<Cell> exploredNodes = new HashSet<Cell>();

        Cell startNode = new Cell(start.x, start.y)
        {
            gCost = 0,
            hCost = 0
        };
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Cell currentNode = openList.OrderBy(n => n.fCost).Last();
            openList.Remove(currentNode);
            exploredNodes.Add(currentNode);

            foreach (var direction in directions)
            {
                Vector2Int neighborPos = new Vector2Int(currentNode.x + direction.x, currentNode.y + direction.y);

                if (!PosizioneValida(neighborPos, grid, zone))
                    continue; // Salta se la posizione non è valida

                if (exploredNodes.Any(n => n.x == neighborPos.x && n.y == neighborPos.y))
                    continue; // Salta se è già stato esplorato

                Cell neighbor = new Cell(neighborPos.x, neighborPos.y);
                float tentativeGCost = currentNode.gCost + 1;
                float tentativeHCost = 0;
                if (grid[neighborPos.x, neighborPos.y] == 2)
                {
                    tentativeHCost = currentNode.hCost - 1000;
                }

                flag = false;
                foreach (Cell n in openList)
                {
                    if (n.x == neighbor.x && n.y == neighbor.y && neighbor.gCost > n.gCost)
                    {
                        n.hCost = tentativeHCost;
                        n.gCost = tentativeGCost;
                        n.parent = currentNode;
                        flag = true;
                    }
                }
                if (!flag)
                {
                    neighbor.gCost = tentativeGCost;
                    neighbor.hCost = tentativeHCost;
                    openList.Add(neighbor);
                }
            }
        }
        return exploredNodes.ToList();
    }

    private string FindZone(Vector2Int cell)
    {
        int x = cell.x;
        int y = cell.y;

        return (x < 35) ? (y < 35 ? "Z1" : "Z3") : (y < 35 ? "Z2" : "Z4");
    }
}