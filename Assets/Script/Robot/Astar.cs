using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AStar{
    private int rows, cols;

    private Vector2Int[] directions = {
        new Vector2Int(0, 1), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };

    private List<Cell> RicostruisciPercorso(Cell cell){
        List<Cell> path = new List<Cell>();
        while (cell != null){
            path.Add(cell);
            cell = cell.parent;
        }
        path.Reverse();
        return path;
    }

    private float CalcolaDistanza(Vector2Int a, Vector2Int b){
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private bool PosizioneValida(Vector2Int pos, int[,] grid){
        rows = grid.GetLength(0);
        cols = grid.GetLength(1);
        bool result = pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols && grid[pos.x, pos.y] != 1;
        return result;
    }


    public List<Cell> TrovaPercorso(Vector2Int start, Vector2Int goal, int[,] grid){
        List<Cell> openList = new List<Cell>();
        HashSet<Cell> closedList = new HashSet<Cell>();

        Cell startNode = new Cell(start.x, start.y) { gCost = 0, hCost = CalcolaDistanza(start, goal) };
        Debug.Log("Inizializzo il nodo di partenza");
        openList.Add(startNode);

        while (openList.Count > 0){
            Cell currentNode = openList.OrderBy(n => n.fCost).First();

            if (currentNode.x == goal.x && currentNode.y == goal.y){
                Debug.Log("Sto ricostruendo il percorso");
                return RicostruisciPercorso(currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (var direction in directions){
                Vector2Int neighborPos = new Vector2Int(currentNode.x + direction.x, currentNode.y + direction.y);

                if (!PosizioneValida(neighborPos, grid))
                    continue;

                Cell neighbor = new Cell(neighborPos.x, neighborPos.y) { gCost = float.MaxValue };

                if (closedList.Any(n => n.x == neighbor.x && n.y == neighbor.y))
                    continue;

                float tentativeGCost = currentNode.gCost + CalcolaDistanza(new Vector2Int(currentNode.x, currentNode.y), neighborPos);

                if (tentativeGCost < neighbor.gCost || !openList.Any(n => n.x == neighbor.x && n.y == neighbor.y)){
                    neighbor.gCost = tentativeGCost; 
                    neighbor.hCost = CalcolaDistanza(neighborPos, goal); 
                    neighbor.parent = currentNode; 

                    if (!openList.Any(n => n.x == neighbor.x && n.y == neighbor.y))
                        openList.Add(neighbor);
                }
            }
        }

        return null;
    }
}

