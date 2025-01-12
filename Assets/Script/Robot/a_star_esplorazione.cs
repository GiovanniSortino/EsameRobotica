using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting.ReorderableList.Element_Adder_Menu; // Usato per Vector2Int compatibile con unity

public class AStarExploration{
    private int rows, cols;
    private bool flag = false;

    // Direzioni di movimento (su, giù, sinistra, destra)
    private Vector2Int[] directions = {
        new Vector2Int(0, 1), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };

    private bool PosizioneValida(Vector2Int pos, int[,] grid, string zone){
        rows = grid.GetLength(0);
        cols = grid.GetLength(1);
        Debug.Log("Verifica");
        return pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols && grid[pos.x, pos.y] != 1 && (FindZone(pos) == zone);// || AreZonesAdjacent(FindZone(pos), zone));
    }

    public List<Cell> EsploraZona(Vector2Int start, int[,] grid, string zone){

        List<Cell> openList = new List<Cell>();
        HashSet<Cell> exploredNodes = new HashSet<Cell>();

        Cell startNode = new Cell(start.x, start.y){
            gCost = 0,
            hCost = 0
        };
        openList.Add(startNode);

        while (openList.Count > 0){
            Cell currentNode = openList.OrderBy(n => n.fCost).Last();
            openList.Remove(currentNode);
            exploredNodes.Add(currentNode);

            foreach (var direction in directions){
                Vector2Int neighborPos = new Vector2Int(currentNode.x + direction.x, currentNode.y + direction.y);

                if (!PosizioneValida(neighborPos, grid, zone)) 
                    continue; // Salta se la posizione non è valida

                if (exploredNodes.Any(n => n.x == neighborPos.x && n.y == neighborPos.y))
                    continue; // Salta se è già stato esplorato

                Cell neighbor = new Cell(neighborPos.x, neighborPos.y);
                float tentativeGCost = currentNode.gCost + 1;
                float tentativeHCost = 0;
                if (grid[neighborPos.x, neighborPos.y] == 2){
                    tentativeHCost = currentNode.hCost - 10;
                }

                // if(AreZonesAdjacent(FindZone(neighborPos), zone)){
                //     tentativeHCost = currentNode.hCost - 100;
                //     Debug.Log("COSTO");
                // }

                flag = false;
                foreach (Cell n in openList){
                    if (n.x == neighbor.x && n.y == neighbor.y && neighbor.gCost > n.gCost){
                        n.hCost = tentativeHCost;
                        n.gCost = tentativeGCost;
                        n.parent = currentNode;
                        flag = true;
                    }
                }

                if (!flag){
                    neighbor.gCost = tentativeGCost;
                    neighbor.hCost = tentativeHCost;
                    openList.Add(neighbor);
                }
            }
        }
        return exploredNodes.ToList();
    }

    private string FindZone(Vector2Int cell){
        int x = cell.x;
        int y = cell.y;

        int zoneX = x / 14;
        int zoneY = y / 14;

        int zoneNumber = zoneY * 5 + zoneX + 1;
        return "Z" + zoneNumber;
    }

    private bool AreZonesAdjacent(string zone1, string zone2){
        int zoneNumber1 = int.Parse(zone1.Substring(1)) - 1;
        int zoneNumber2 = int.Parse(zone2.Substring(1)) - 1;

        int row1 = zoneNumber1 / 5;
        int col1 = zoneNumber1 % 5;

        int row2 = zoneNumber2 / 5;
        int col2 = zoneNumber2 % 5;

        int rowDiff = Mathf.Abs(row1 - row2);
        int colDiff = Mathf.Abs(col1 - col2);

        return rowDiff <= 1 && colDiff <= 1 && (rowDiff + colDiff > 0);
    }

    public int[,] Check(int[,] grid, int x_start, int y_start){
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        bool[,] visited = new bool[rows, cols];

        Queue<Vector2Int> coda = new Queue<Vector2Int>();
        visited[x_start, y_start] = true;
        coda.Enqueue(new Vector2Int(x_start, y_start));

        while (coda.Count > 0){
            Vector2Int current = coda.Dequeue();
            
            foreach (Vector2Int neighbor in AddNeighbor(current.x, current.y, 0, 70,grid,visited)){
                coda.Enqueue(neighbor);  
            }
        }

        int[,] new_grid = (int[,])grid.Clone();

        for (int x = 0; x < rows; x++){
            for (int y = 0; y < cols; y++){
                if (!visited[x,y]){
                    new_grid[x, y] = 1;
                }
            }
        }
        return new_grid;
    }   

    public IEnumerable<Vector2Int> AddNeighbor(int x, int y, int min, int max, int[,] grid, bool[,] visited){
        if (x - 1 >= min){
            if (grid[x - 1, y] != 1 && !visited[x - 1, y]){
                visited[x - 1, y] = true;
                yield return new Vector2Int(x - 1, y);
            }
        }

        if (x + 1 <= max){
            if (grid[x + 1, y] != 1 && !visited[x + 1, y]){
                visited[x + 1, y] = true;
                yield return new Vector2Int(x + 1, y);
            }
        }

        if (y - 1 >= min){
            if (grid[x, y - 1] != 1 && !visited[x, y - 1]){
                visited[x, y - 1] = true;
                yield return new Vector2Int(x, y - 1);
            }
        }

        if (y + 1 <= max){
            if (grid[x, y + 1] != 1 && !visited[x, y + 1]){
                visited[x, y + 1] = true;
                yield return new Vector2Int(x, y + 1);
            }
        }
    }


}