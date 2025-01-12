using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AStarExploration{
    private int rows, cols;

    private Vector2Int[] directions = {
        new Vector2Int(0, 1), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };

    private List<Cell> RicostruisciPercorso(Cell cell){
        List<Cell> path = new List<Cell>();
        while (cell != null){
            path.Add(cell);
            cell = cell.parent; // Torna al parent
        }
        path.Reverse(); // Inverte l'ordine per ottenere il percorso giusto
        return path;
    }

    private float CalcolaDistanza(Cell a, Cell b){
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private bool PosizioneValida(Cell pos, int[,] grid, string zone){
        rows = grid.GetLength(0);
        cols = grid.GetLength(1);
        return pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols && grid[pos.x, pos.y] != 1 && (FindZone(pos) == zone);// || AreZonesAdjacent(FindZone(pos), zone));
    }

    public List<Cell> EsploraZona(Vector2Int start, Vector2Int goal, int[,] grid, string zone){
        List<Cell> openList = new List<Cell>();
        HashSet<Cell> closedList = new HashSet<Cell>();

        Cell startNode = new Cell(start.x, start.y) { gCost = 0};
        startNode.hCost = CountVisitedCell(startNode, closedList);
        openList.Add(startNode);

        while (openList.Count > 0){
            Cell currentNode = openList.OrderBy(n => n.fCost).Last();

            if (currentNode.x == goal.x && currentNode.y == goal.y){
                return RicostruisciPercorso(currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (var direction in directions){
                Cell neighbor = new Cell(currentNode.x + direction.x, currentNode.y + direction.y) { gCost = float.MinValue };

                if (!PosizioneValida(neighbor, grid, zone) || closedList.Any(n => n.x == neighbor.x && n.y == neighbor.y))
                    continue;

                float tentativeGCost = currentNode.gCost + 1;

                bool isPresent = false;
                foreach(Cell oldCell in openList){
                    if(oldCell.x == neighbor.x && oldCell.y == neighbor.y && oldCell.fCost < tentativeGCost + CountVisitedCell(neighbor, closedList)){
                        oldCell.gCost = tentativeGCost;
                        oldCell.hCost = CountVisitedCell(oldCell, closedList);
                        oldCell.parent = currentNode;
                        isPresent = true;
                        break;
                    }else if(oldCell.x == neighbor.x && oldCell.y == neighbor.y && oldCell.fCost >= tentativeGCost + CountVisitedCell(neighbor, closedList)){
                        isPresent = true;
                        break;
                    }
                }

                if(!isPresent){
                    neighbor.gCost = tentativeGCost;
                    neighbor.hCost = CountVisitedCell(neighbor, closedList);
                    neighbor.parent = currentNode;
                    openList.Add(neighbor);
                }
            }
        }

        return null;
    }

    private string FindZone(Cell cell){
        int x = cell.x;
        int y = cell.y;

        int zoneX = x / 14;
        int zoneY = y / 14;

        int zoneNumber = zoneY * 5 + zoneX + 1;
        return "Z" + zoneNumber;
    }

    private int CountVisitedCell(Cell cell, HashSet<Cell> exploredNode){
        int count = 0;
        foreach (var direction in directions){
            Cell neighbor = new Cell(cell.x + direction.x, cell.y + direction.y);
            if(exploredNode.Any(n => n.x == neighbor.x && n.y == neighbor.y)){
                count++;
            }
        }
        return count;
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

    public int[,] Check(int[,] grid, int x_start, int y_start, int min_x, int min_y, int max_x, int max_y){
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        bool[,] visited = new bool[rows, cols];

        Queue<Vector2Int> coda = new Queue<Vector2Int>();
        visited[x_start, y_start] = true;
        coda.Enqueue(new Vector2Int(x_start, y_start));

        while (coda.Count > 0){
            Vector2Int current = coda.Dequeue();
            
            foreach (Vector2Int neighbor in AddNeighbor(current.x, current.y, min_x, min_y,max_x, max_y,grid,visited)){
                coda.Enqueue(neighbor);  
            }
        }

        int[,] new_grid = (int[,])grid.Clone();

        for (int x = 0; x <= max_x; x++){
            for (int y = 0; y <= max_y; y++){
                if (!visited[x,y]){
                    new_grid[x, y] = 1;
                }
            }
        }
        return new_grid;
    }   

    public IEnumerable<Vector2Int> AddNeighbor(int x, int y, int min_x, int min_y, int max_x, int max_y, int[,] grid, bool[,] visited){
        if (x - 1 >= min_x){
            if (grid[x - 1, y] != 1 && !visited[x - 1, y]){
                visited[x - 1, y] = true;
                yield return new Vector2Int(x - 1, y);
            }
        }

        if (x + 1 <= max_x){
            if (grid[x + 1, y] != 1 && !visited[x + 1, y]){
                visited[x + 1, y] = true;
                yield return new Vector2Int(x + 1, y);
            }
        }

        if (y - 1 >= min_y){
            if (grid[x, y - 1] != 1 && !visited[x, y - 1]){
                visited[x, y - 1] = true;
                yield return new Vector2Int(x, y - 1);
            }
        }

        if (y + 1 <= max_y){
            if (grid[x, y + 1] != 1 && !visited[x, y + 1]){
                visited[x, y + 1] = true;
                yield return new Vector2Int(x, y + 1);
            }
        }
    }


}