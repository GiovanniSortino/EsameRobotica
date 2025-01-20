using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;

public class DStarLite
{
    private int rows, cols;

    public static int[,] grid_visited;
    public bool first = false;
    private AStar aStar = new AStar();

    static DStarLite(){
        grid_visited = InizializzaGridVisited(70, 70);
    }

    public static int[,] InizializzaGridVisited(int rows, int cols){
        int[,] gridVisited = new int[rows, cols];
        for (int i = 0; i < rows; i++){
            for (int j = 0; j < cols; j++)
            {
                gridVisited[i, j] = -1;
            }
        }
        return gridVisited;
    }

    private Vector2Int[] directions = {
        new Vector2Int(0, 1), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };

    private bool PosizioneValida(Cell pos, int[,] grid, string zone)
    {
        rows = grid.GetLength(0);
        cols = grid.GetLength(1);
        return pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols && grid[pos.x, pos.y] != 1 && (FindZone(pos) == zone);
    }

    private float Heuristic(Cell a, Cell b, int[,] grid, List<Cell> pathTronc, List<Cell> path)
    {
        int inv;
        Vector2Int av = new Vector2Int(a.x, a.y);
        Vector2Int bv = new Vector2Int(b.x, b.y);
        Debug.Log($"Vicino :{a.ToString()}, Robot {b.ToString()}, Av :{av.ToString()}, Bv {bv.ToString()}");
        StampaMatrice(grid);
        List<Cell> eur_path = aStar.TrovaPercorso(av, bv, grid);
        bool flag = false;
        if (eur_path == null)
        {
            Debug.Log("NULLLLL");
            flag = true;
        }
        if (grid[a.x, a.y] == 0 && !pathTronc.Contains(a))
        {
            inv = 1; 

        }
        else
        {
            inv = -1;
        }
        float eur;
        float eur_inv;
        if (flag)
        {
            eur_inv = -1000;
        }
        else
        {
            eur = eur_path.Count;
            eur_inv = eur * inv;
        }
        Debug.Log($"Euristica {a.x}, {a.y} : {eur_inv}");

        return eur_inv;
    }

    public List<Cell> UpdatePath(List<Cell> pathTronc, Cell robotPos, Cell goal, int[,] grid, string zone)
    {

        first = true;

        grid_visited = CreaNuovaGriglia(grid);
        if (pathTronc == null || pathTronc.Count == 0)
        {
            pathTronc.Add(goal);
        }

        Cell cella = pathTronc[pathTronc.Count - 1];
        pathTronc.RemoveAt(pathTronc.Count - 1);

        (int min_x, int min_y, int max_x, int max_y) = GetZoneBounds(zone, 14);

        while (grid[cella.x, cella.y] == 1)
        {
            if (pathTronc.Count == 0)
            {
                if (grid[min_x, max_y] == 0)
                {
                    pathTronc.Add(new Cell(min_x, max_y));
                }
                else if (grid[max_x, min_y] == 0)
                {
                    pathTronc.Add(new Cell(max_x, min_y));
                }
                else
                {
                    pathTronc.Add(robotPos);
                    //Debug.Log("Non ci sono celle non ostacolo nel percorso troncato.");
                    break;
                }
            }

            cella = pathTronc[pathTronc.Count - 1];
            pathTronc.RemoveAt(pathTronc.Count - 1);
        }

        List<Cell> path = new List<Cell>();
        grid_visited[robotPos.x, robotPos.y] = -1;

        Debug.Log($"cella di partenza {cella.ToString()}");
        Debug.Log($"cella robot {robotPos.ToString()}");

        while (cella != null)
        {
            Debug.Log($"cella scelta {cella.ToString()}");

            path.Add(cella);
            grid_visited[cella.x, cella.y] = 0;



            if (cella.x == robotPos.x && cella.y == robotPos.y) 
            {
                

                path.Reverse();
                StampaPercorso(path);
                pathTronc.Reverse();
                path.AddRange(pathTronc);
                Debug.Log("Nodo iniziale");
                Debug.Log(cella.ToString());
                //StampaPercorso(path);
                //int[,] grid_for_samp = CreaNuovaGriglia(grid);
                //List<Cell> path_samp = SemplificaPercorso(path, grid_for_samp);

                return path;
            }

            List<Cell> neighbors = prendi_vicini_non_visitati(cella, grid_visited, zone, pathTronc, grid);

            if (neighbors.Count >= 1)
            {
                Cell maxNeighbor = max_euristic_neighbor(neighbors, robotPos, pathTronc, grid, path);
                maxNeighbor.parent = cella;
                cella = maxNeighbor;

            }
            else
            {
                Debug.Log("RICERCA IN AMPIEZZA");

                cella = cella.parent;
            }
            //Debug.Log($"cella scelta {cella.ToString()}");
        }
        //Debug.Log("Percorso: Null, Non sono riuscito a trovare un percorso che parte dal target fino al robot");

        List<Cell> r = new();
        r.Add(robotPos);
        return r;
    }

    (int min_x, int min_y, int max_x, int max_y) GetZoneBounds(string zona, int zoneSize)
    {
        if (!int.TryParse(zona.Substring(1), out int zoneNumber))
        {
            return (0, 0, 0, 0);
        }

        int zonesPerRow = 70 / zoneSize; 
        int colIndex = (zoneNumber - 1) % zonesPerRow; 
        int rowIndex = (zoneNumber - 1) / zonesPerRow; 

        int min_x = colIndex * zoneSize;    
        int max_x = min_x + zoneSize;        
        int min_y = rowIndex * zoneSize;    
        int max_y = min_y + zoneSize;  

        return (min_x, min_y, max_x, max_y);
    }

    public void StampaMatrice(int[,] matrix)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("MATRICE:");

        for (int j = 13; j >= 0; j--)
        {
            for (int i = 0; i < 14; i++)
            {
                sb.Append(matrix[i, j] + " ");
            }
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }

    public int[,] CreaNuovaGriglia(int[,] grid)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        int[,] nuovaGriglia = new int[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (grid[i, j] != 1)
                {
                    nuovaGriglia[i, j] = -1;
                }
                else
                {
                    nuovaGriglia[i, j] = 0;
                }
            }
        }
        //Debug.Log("AGGIORNAMENTO GRID_VISITED");
        return nuovaGriglia;
    }
    private Cell max_euristic_neighbor(List<Cell> neighbors, Cell robotPos, List<Cell> pathTronc, int[,] grid, List<Cell> path)
    {
        Cell bestNeighbor = null;

        float maxEur = float.MinValue;
        foreach (Cell neigh in neighbors)
        {
            float heuristic = Heuristic(neigh, robotPos, grid, pathTronc, path);
            if (heuristic > maxEur)
            {
                maxEur = heuristic;
                bestNeighbor = neigh;
            }
        }
        return bestNeighbor;
    }


    private List<Cell> prendi_vicini_non_visitati(Cell cella, int[,] grid_visited, string zone, List<Cell> pathTronc, int[,] grid)
    {
        List<Cell> neighbors = new List<Cell>();

        foreach (var direction in directions)
        {
            Cell neighbor = new Cell(cella.x + direction.x, cella.y + direction.y);

            if (PosizioneValida(neighbor, grid, zone) &&
                grid_visited[neighbor.x, neighbor.y] == -1 && grid[neighbor.x, neighbor.y] != 1) 
            {
                neighbors.Add(neighbor);
                Debug.Log($"Vicino PRESO: {neighbor.ToString()}");
            }
            else
            {
                Debug.Log($"Vicino SCARTATO: {neighbor.ToString()}");

            }
        }
        return neighbors;
    }

    private string FindZone(Cell cell)
    {
        int x = cell.x;
        int y = cell.y;

        int zoneX = x / 14;
        int zoneY = y / 14;

        int zoneNumber = zoneY * 5 + zoneX + 1;
        return "Z" + zoneNumber;
    }

    public int[,] Check(int[,] grid, int x_start, int y_start, int min_x, int min_y, int max_x, int max_y)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        bool[,] visited = new bool[rows, cols];

        Queue<Vector2Int> coda = new Queue<Vector2Int>();
        visited[x_start, y_start] = true;
        coda.Enqueue(new Vector2Int(x_start, y_start));

        while (coda.Count > 0)
        {
            Vector2Int current = coda.Dequeue();

            foreach (Vector2Int neighbor in AddNeighbor(current.x, current.y, min_x, min_y, max_x, max_y, grid, visited))
            {
                coda.Enqueue(neighbor);
            }
        }

        int[,] new_grid = (int[,])grid.Clone();
        bool visit = false;
        for (int x = min_x; x <= max_x; x++)
        {
            for (int y = min_y; y <= max_y; y++)
            {
                if (!visited[x, y])
                {
                    new_grid[x, y] = 1;
                    if (new_grid[x, y] != grid[x, y])
                    {
                        visit = true;
                    }
                }
            }
        }
        if (visit)
        {
            DatabaseManager.sapiTextToSpeech.Speak($"Ho individuato zone inacessibili per via di ostacoli attorno di esso");
        }

        return new_grid;
    }

    public IEnumerable<Vector2Int> AddNeighbor(int x, int y, int min_x, int min_y, int max_x, int max_y, int[,] grid, bool[,] visited)
    {
        if (x - 1 >= min_x)
        {
            if (grid[x - 1, y] != 1 && !visited[x - 1, y])
            {
                visited[x - 1, y] = true;
                yield return new Vector2Int(x - 1, y);
            }
        }

        if (x + 1 <= max_x)
        {
            if (grid[x + 1, y] != 1 && !visited[x + 1, y])
            {
                visited[x + 1, y] = true;
                yield return new Vector2Int(x + 1, y);
            }
        }

        if (y - 1 >= min_y)
        {
            if (grid[x, y - 1] != 1 && !visited[x, y - 1])
            {
                visited[x, y - 1] = true;
                yield return new Vector2Int(x, y - 1);
            }
        }

        if (y + 1 <= max_y)
        {
            if (grid[x, y + 1] != 1 && !visited[x, y + 1])
            {
                visited[x, y + 1] = true;
                yield return new Vector2Int(x, y + 1);
            }
        }
    }
    

    public void StampaPercorso(List<Cell> path)
    {
        if (path == null || path.Count == 0)
        {
            Console.WriteLine("Il percorso è vuoto o nullo.");
            return;
        }

        StringBuilder sb = new StringBuilder("Percorso trovato:\n");

        for (int i = 0; i < path.Count; i++)
        {
            sb.Append($"Step {i + 1}: ({path[i].x}, {path[i].y})\n");
        }
        Debug.Log(sb.ToString());
    }

    public List<Cell> SemplificaPercorso(List<Cell> path, int[,] grid_for_samp)
    {
        List<Cell> percorsoSemplificato = new List<Cell>(path);
        List<Cell> percorso_ritorno = new List<Cell>();

        int i = 0;
        while (i < percorsoSemplificato.Count)
        {
            if (grid_for_samp[percorsoSemplificato[i].x, percorsoSemplificato[i].y] == 0)
            {
                int j = i + 1;
                while (j < percorsoSemplificato.Count)
                {
                    if (percorsoSemplificato[i].x == percorsoSemplificato[j].x &&
                        percorsoSemplificato[i].y == percorsoSemplificato[j].y)
                    {
                        percorsoSemplificato.RemoveRange(i + 1, j - i);
                        Debug.Log($"Ho tagliato da {i + 1} a {j - 1}");
                        j = i; 
                    }
                    else if (grid_for_samp[percorsoSemplificato[j].x, percorsoSemplificato[j].y] != 0)
                    {
                        Debug.Log("Salto");
                        break; 
                    }
                    else
                    {
                        j++;
                    }
                }
            }

            if (grid_for_samp[percorsoSemplificato[i].x, percorsoSemplificato[i].y] != 0)
            {
                percorso_ritorno.Add(percorsoSemplificato[i]);
                grid_for_samp[percorsoSemplificato[i].x, percorsoSemplificato[i].y] = 0;
            }

            i++;
        }

        return percorso_ritorno;
    }


}
