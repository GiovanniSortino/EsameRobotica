using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class DStarLite
{
    private int rows, cols;

    public static int[,] grid_visited;
    public bool first = false;
    public int CountStop = 0;

    // Costruttore statico per inizializzare grid_visited
    static DStarLite()
    {
        grid_visited = InizializzaGridVisited(70, 70);
    }

    public static int[,] InizializzaGridVisited(int rows, int cols)
    {
        int[,] gridVisited = new int[rows, cols];
        for (int i = 0; i < rows; i++)
        {
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

    private List<Cell> RicostruisciPercorso(Cell cell)
    {
        List<Cell> path = new List<Cell>();
        while (cell != null)
        {
            path.Add(cell);
            cell = cell.parent; // Torna al parent
        }
        path.Reverse(); // Inverte l'ordine per ottenere il percorso giusto
        return path;
    }


    //GIUSTAAAAAAAAAAAA
    private bool PosizioneValida(Cell pos, int[,] grid, string zone)
    {
        rows = grid.GetLength(0);
        cols = grid.GetLength(1);
        return pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols && grid[pos.x, pos.y] != 1 && (FindZone(pos) == zone);
    }

    private float Manhattan(Cell a, Cell b, int[,] grid, List<Cell> pathTronc)
    {
        int inv;
        // Se la cella non � stata visitata (presumibilmente indicata da -1), inv = 1, altrimenti inv = -1
        if (grid[a.x, a.y] == 0)//&& !pathTronc.Contains(a)
        {
            inv = 1;
            Debug.Log($"Euristica {a.x}, {a.y} : {inv}");

        }
        else
        {
            inv = -1;
            Debug.Log($"Euristica {a.x}, {a.y} : {inv}");
        }
        float eur = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        float eur_inv = eur * inv;
        return eur_inv;
    }

    public List<Cell> UpdatePath(List<Cell> pathTronc, Cell robotPos, Cell goal, int[,] grid, string zone)
    {
        /*int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        Debug.Log(rows);*/
        first = true;

        grid_visited = CreaNuovaGriglia(grid);
        if (pathTronc == null || pathTronc.Count == 0)
        {
            throw new ArgumentException("Il percorso troncato non pu� essere nullo o vuoto.");
        }

        Cell cella = pathTronc[pathTronc.Count - 1];
        //pathTronc.RemoveAt(pathTronc.Count - 1);

        //Utilizzare una open list, come quella di Mc per tenere conto dei vicini
        //Inoltre, per evitare che prenda percorsi ancora non esplorati per
        //raggiungere quella zona usa un flag (forse non serve)
        //while (isChild(grid[cella.x, cella.y]))


        while (grid[cella.x, cella.y] == 1)
        {
            // Se la lista � vuota, esci dal ciclo (o gestisci l'errore)
            if (pathTronc.Count == 0)
            {
                pathTronc.Add(robotPos);
                Debug.Log("Non ci sono celle non ostacolo nel percorso troncato.");
                break; // oppure return null, a seconda della logica
            }

            cella = pathTronc[pathTronc.Count - 1];
            pathTronc.RemoveAt(pathTronc.Count - 1);
        }

        List<Cell> path = new List<Cell>();
        grid_visited[robotPos.x, robotPos.y] = -1;
        string pathString = "Percorso: ";

        Debug.Log($"cella di partenza {cella.ToString()}");
        Debug.Log($"cella robot {robotPos.ToString()}");

        while (cella != null)
        {
            Debug.Log($"cella scelta {cella.ToString()}");

            path.Add(cella);
            grid_visited[cella.x, cella.y] = 0;

            if (Stop(grid, zone))
            {
                List<Cell> r =  new();
                r.Add(robotPos);
                return r;
            }

            if (cella.x == robotPos.x && cella.y == robotPos.y)
            {
                //path.Add(robotPos);
                Debug.Log("Nodo iniziale");
                Debug.Log(cella.ToString());
                path.Reverse();
                pathTronc.Reverse();
                path.AddRange(pathTronc);
                //pathTronc.AddRange(path);
                return path;
            }

            List<Cell> neighbors = prendi_vicini_non_visitati(cella, grid, zone, pathTronc);

            if (neighbors.Count >= 1)
            {
                Cell maxNeighbor = max_euristic_neighbor(neighbors, robotPos, pathTronc, grid);
                maxNeighbor.parent = cella;
                cella = maxNeighbor;
            }
            else
            {
                Debug.Log("Forse � qui il problema");
                cella = cella.parent;
            }
            //Debug.Log($"cella scelta {cella.ToString()}");
        }
        Debug.Log("Percorso: Null");
        return null;
    }

    public bool Stop(int[,] grid, string zona)
    {
        // Dimensione di ogni zona
        int zoneSize = 14;

        // Verifica che la griglia abbia dimensioni 70x70 (oppure che zoneSize sia coerente con grid)
        int totalRows = grid.GetLength(0);
        int totalCols = grid.GetLength(1);

        // Se la griglia non rispetta le dimensioni attese, si pu� loggare un errore
        if (totalRows != 70 || totalCols != 70)
        {
            Debug.LogWarning("La griglia non ha le dimensioni 70x70. Verifica le dimensioni.");
        }

        int CountStop = 0;

        // Estrae il numero della zona dalla stringa, ad esempio da "Z1", "Z2", ...
        int zoneNumber;
        if (!int.TryParse(zona.Substring(1), out zoneNumber))
        {
            Debug.LogError("Formato della zona non valido: " + zona);
            return false;
        }

        // In una griglia 70x70 divisa in blocchi di 14x14 ci sono 5 blocchi per riga e 5 per colonna (totale 25 zone).
        // Calcola l'indice di colonna (0-4) e l'indice di riga (0-4) in base al numero di zona:
        int colIndex = (zoneNumber - 1) % 5;  // es. per Z1 -> 0, Z2 -> 1, ..., Z5 -> 4, Z6 -> 0, ecc.
        int rowIndex = (zoneNumber - 1) / 5;    // es. per Z1-Z5 -> 0, per Z6-Z10 -> 1, ecc.

        int min_x = colIndex * zoneSize;      // colonna minima
        int max_x = min_x + zoneSize;           // colonna massima (non inclusa nell�iterazione)
        int min_y = rowIndex * zoneSize;        // riga minima
        int max_y = min_y + zoneSize;

        for (int i = min_x; i < max_x; i++)
        {
            for (int j = min_y; j < max_y; j++)
            {
                if (grid[i, j] != 0)//|| grid_visited[i,j] == 0
                {
                    CountStop++;
                }
            }
        }

        if (CountStop >= 196)
        {
            Debug.Log($"1 Ho visitato {CountStop} celle");
            return true;
        }
        Debug.Log($"2 Ho visitato {CountStop} celle");

        return false;
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
                if (grid[i, j] != 1)//|| grid_visited[i,j] == 0
                {
                    nuovaGriglia[i, j] = -1; // -1 per i nodi in cui il robot pu� camminare
                }
                else
                {
                    nuovaGriglia[i, j] = 0; // 0 in cui non pu� andare (osatacoli o percorsi gi� programmati)
                }
            }
        }
        Debug.Log("AGGIORNAMENTO GRID_VISITED");
        return nuovaGriglia;
    }
    private Cell max_euristic_neighbor(List<Cell> neighbors, Cell robotPos, List<Cell> pathTronc, int[,] grid)
    {
        Cell bestNeighbor = null;

        if (first)
        {
            // Trova il vicino con il valore di euristica MINIMO
            float minEur = float.MaxValue;
            foreach (Cell neigh in neighbors)
            {
                float heuristic = Manhattan(neigh, robotPos, grid, pathTronc);
                if (heuristic < minEur)
                {
                    minEur = heuristic;
                    bestNeighbor = neigh;
                }
            }
            first = false;
        }
        else
        {
            // Trova il vicino con il valore di euristica MASSIMO
            float maxEur = float.MinValue;
            foreach (Cell neigh in neighbors)
            {
                float heuristic = Manhattan(neigh, robotPos, grid, pathTronc);
                if (heuristic > maxEur)
                {
                    maxEur = heuristic;
                    bestNeighbor = neigh;
                }
            }
        }


        return bestNeighbor;
    }


    private List<Cell> prendi_vicini_non_visitati(Cell cella, int[,] grid, string zone, List<Cell> pathTronc)
    {
        List<Cell> neighbors = new List<Cell>();

        foreach (var direction in directions)
        {
            Cell neighbor = new Cell(cella.x + direction.x, cella.y + direction.y);

            if (PosizioneValida(neighbor, grid, zone) &&
                grid_visited[neighbor.x, neighbor.y] == -1) // && !pathTronc.Contains(neighbor)
            {
                neighbors.Add(neighbor);
                Debug.Log($"Vicino: {neighbor.ToString()}");
            }
        }
        return neighbors;
    }

    private bool isChild(Cell nodo, int[,] grid, string zone)
    {
        foreach (var direction in directions)
        {
            Cell neighbor = new Cell(nodo.x + direction.x, nodo.y + direction.y);

            if (PosizioneValida(neighbor, grid, zone) &&
                grid[neighbor.x, neighbor.y] == 2)
            {
                return true;
            }
        }
        return false;
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

    private int CountVisitedCell(Cell cell, HashSet<Cell> exploredNode)
    {
        int count = 0;
        foreach (var direction in directions)
        {
            Cell neighbor = new Cell(cell.x + direction.x, cell.y + direction.y);
            if (exploredNode.Any(n => n.x == neighbor.x && n.y == neighbor.y))
            {
                count++;
            }
        }
        return count;
    }

    private bool AreZonesAdjacent(string zone1, string zone2)
    {
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

    // Segna zone no visitate
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

        for (int x = min_x; x <= max_x; x++)
        {
            for (int y = min_y; y <= max_y; y++)
            {
                if (!visited[x, y])
                {
                    new_grid[x, y] = 1;
                }
            }
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


}