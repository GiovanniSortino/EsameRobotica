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
    public int CountStop = 0;
    private AStar aStar = new AStar(); 

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

    private float Manhattan(Cell a, Cell b, int[,] grid, List<Cell> pathTronc, List<Cell> path)
    {
        int inv;
        Vector2Int av = new Vector2Int(a.x, a.y);
        Vector2Int bv = new Vector2Int(b.x, b.y);
        List<Cell> eur_path = aStar.TrovaPercorso(av, bv, grid);
        // Se la cella non   stata visitata (presumibilmente indicata da -1), inv = 1, altrimenti inv = -1
        if (grid[a.x, a.y] == 0 && !pathTronc.Contains(a))// && !path.Contains(a) //Se è già stata visitata dal robot
        {
            inv = 1; //nel caso di celle già visitate devo allontanarmi dal target
            // Debug.Log($"Euristica {a.x}, {a.y} : {inv}");

        }
        else
        {
            inv = -1; //nel caso di celle già visitate devo avvicinarmi al target
            // Debug.Log($"Euristica {a.x}, {a.y} : {inv}");
        }
        float eur = eur_path.Count;//Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        float eur_inv = eur * inv;
        Debug.Log($"Euristica {a.x}, {a.y} : {eur_inv}");

        return eur_inv;
    }

    public List<Cell> UpdatePath(List<Cell> pathTronc, Cell robotPos, Cell goal, int[,] grid, string zone)
    {
        /*int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        Debug.Log(rows);*/
        //StampaMatrice(grid);

        first = true;

        grid_visited = CreaNuovaGriglia(grid);
        if (pathTronc == null || pathTronc.Count == 0){
            pathTronc.Add(goal);
        }

        Cell cella = pathTronc[pathTronc.Count - 1];
        pathTronc.RemoveAt(pathTronc.Count - 1);

        (int min_x, int min_y, int max_x, int max_y) = GetZoneBounds(zone, 14);

        while (grid[cella.x, cella.y] == 1)
        {
            // Se la lista   vuota, esci dal ciclo (o gestisci l'errore)
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
                    break; // oppure return null, a seconda della logica
                }
            }

            cella = pathTronc[pathTronc.Count - 1];
            pathTronc.RemoveAt(pathTronc.Count - 1);
        }

        List<Cell> path = new List<Cell>();
        grid_visited[robotPos.x, robotPos.y] = -1;
        // string pathString = "Percorso: ";

        Debug.Log($"cella di partenza {cella.ToString()}");
        Debug.Log($"cella robot {robotPos.ToString()}");

        while (cella != null)
        {
            Debug.Log($"cella scelta {cella.ToString()}");

            path.Add(cella);
            grid_visited[cella.x, cella.y] = 0;

 

            if (cella.x == robotPos.x && cella.y == robotPos.y) //Far aggiungere a dario, se la cella è quella di target ma non ha completato la lista continua
            {
                //path.Add(robotPos);
                //Debug.Log("Nodo iniziale");
                //Debug.Log(cella.ToString());

                //StampaMatrice(grid_visited);

                path.Reverse();
                StampaPercorso(path);
                pathTronc.Reverse();
                path.AddRange(pathTronc);
                Debug.Log("Nodo iniziale");
                Debug.Log(cella.ToString());
                //TrovaCelleRipetute(path);
                StampaPercorso(path);
                //int[,] grid_for_samp = CreaNuovaGriglia(grid);
                //List<Cell> path_samp = SemplificaPercorso(path, grid_for_samp);
                //StampaPercorso(path_samp);

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
                /*
                // Trova la cella più vicina con -1
                (min_x, min_y, max_x, max_y) = GetZoneBounds(zone, 14);
                cella = FindNearestMinusOne(path, cella.x, cella.y, min_x, min_y, max_x, max_y, grid);

                List<Cell> li = new List<Cell> { goal }; // Lista corretta
                List<Cell> p = UpdatePath(li, robotPos, goal, grid, zone); // Lista corretta

                path.AddRange(p); // Usa AddRange invece di Add per aggiungere più elementi

                cella = p[p.Count - 1]; // Assegna l'ultima cella trovata   */

                cella = cella.parent;
            }
            //Debug.Log($"cella scelta {cella.ToString()}");
        }
        //Debug.Log("Percorso: Null, Non sono riuscito a trovare un percorso che parte dal target fino al robot");
        
        List<Cell> r =  new();
        r.Add(robotPos);
        return r;
        //return null;
    }

    (int min_x, int min_y, int max_x, int max_y) GetZoneBounds(string zona, int zoneSize)
    {
        // Estrai il numero della zona dalla stringa, ad esempio da "Z1", "Z2", ...
        if (!int.TryParse(zona.Substring(1), out int zoneNumber))
        {
            //Debug.LogError($"Formato della zona non valido: {zona}");
            return (0,0, 0,0);
        }

        // Calcola l'indice di colonna (x) e riga (y) in base al numero della zona
        int zonesPerRow = 70 / zoneSize; // Numero di zone per riga
        int colIndex = (zoneNumber - 1) % zonesPerRow; // Colonna (0-4 per zoneSize=14)
        int rowIndex = (zoneNumber - 1) / zonesPerRow; // Riga (0-4 per zoneSize=14)

        // Calcola i limiti della zona
        int min_x = colIndex * zoneSize;      // Colonna minima
        int max_x = min_x + zoneSize;         // Colonna massima (non inclusiva)
        int min_y = rowIndex * zoneSize;      // Riga minima
        int max_y = min_y + zoneSize;         // Riga massima (non inclusiva)

        // Ritorna i limiti come tuple
        return (min_x, min_y, max_x, max_y);
    }

    public void StampaMatrice(int[,] matrix)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("MATRICE:");

        for (int j = 13; j >= 0; j--) // Invertiamo le colonne per eliminare la specchiatura
        {
            for (int i = 0; i < 14; i++) // Righe normali
            {
                sb.Append(matrix[i, j] + " ");
            }
            sb.AppendLine();
        }

        Debug.Log(sb.ToString()); // Mostra la matrice nella Console di Unity
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
                    nuovaGriglia[i, j] = -1; // -1 per i nodi in cui il robot pu  camminare
                }
                else
                {
                    nuovaGriglia[i, j] = 0; // 0 in cui non pu  andare (osatacoli o percorsi gi  programmati)
                }
            }
        }
        //Debug.Log("AGGIORNAMENTO GRID_VISITED");
        return nuovaGriglia;
    }
    private Cell max_euristic_neighbor(List<Cell> neighbors, Cell robotPos, List<Cell> pathTronc, int[,] grid, List<Cell> path)
    {
        Cell bestNeighbor = null;

        /*if (first)
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
            if (pathTronc.Count > 1)
            {
                first = true;
            }
            else{
                first = false;
            }
        }
        else
        {*/
            // Trova il vicino con il valore di euristica MASSIMO
            float maxEur = float.MinValue;
            foreach (Cell neigh in neighbors)
            {
                float heuristic = Manhattan(neigh, robotPos, grid, pathTronc, path);
                if (heuristic > maxEur)
                {
                    maxEur = heuristic;
                    bestNeighbor = neigh;
                }
            }
        //}
        return bestNeighbor;
    }


    private List<Cell> prendi_vicini_non_visitati(Cell cella, int[,] grid_visited, string zone, List<Cell> pathTronc, int[,] grid)
    {
        List<Cell> neighbors = new List<Cell>();

        foreach (var direction in directions)
        {
            Cell neighbor = new Cell(cella.x + direction.x, cella.y + direction.y);

            if (PosizioneValida(neighbor, grid, zone) &&
                grid_visited[neighbor.x, neighbor.y] == -1 && grid[neighbor.x, neighbor.y] != 1) // && !pathTronc.Contains(neighbor)
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
    public Cell? FindNearestMinusOne(List<Cell> path, int startX, int startY, int min_x, int min_y, int max_x, int max_y, int[,] grid)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        int[,] grid_path = CreaNuovaGrigliaDaPath(path, grid);

        Queue<Cell> coda = new Queue<Cell>();
        grid_path[startX, startY] = 1; // Segna come visitato
        coda.Enqueue(new Cell(startX, startY));

        while (coda.Count > 0)
        {
            Cell current = coda.Dequeue();

            foreach (Cell neighbor in Neighbor(current, grid_path, min_x, min_y, max_x, max_y, grid))
            {
                if (grid[neighbor.x, neighbor.y] == -1)
                {
                    return neighbor; // Trova la prima cella con -1 e la restituisce
                }

                if (grid_path[neighbor.x, neighbor.y] == 0) // Se non è ancora stata visitata
                {
                    grid_path[neighbor.x, neighbor.y] = 1; // Segna come visitata
                    coda.Enqueue(neighbor);
                }
            }
        }
        return null; // Nessuna cella con -1 trovata
    }
    public int[,] CreaNuovaGrigliaDaPath(List<Cell> path, int[,] grid)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        int[,] nuovaGriglia = new int[rows, cols]; // Inizializzato automaticamente a 0

        foreach (Cell cella in path)
        {
            if (cella.x >= 0 && cella.x < rows && cella.y >= 0 && cella.y < cols)
            {
                nuovaGriglia[cella.x, cella.y] = -1; // Imposta -1 per le celle nel path
            }
        }

        return nuovaGriglia;
    }
    private List<Cell> Neighbor(Cell cella, int[,] grid_path, int min_x, int min_y, int max_x, int max_y, int[,] grid)
    {
        List<Cell> neighbors = new List<Cell>();

        int[][] directions = new int[][]
        {
        new int[] { -1,  0 }, // Su
        new int[] {  1,  0 }, // Giù
        new int[] {  0, -1 }, // Sinistra
        new int[] {  0,  1 }  // Destra
        };

        foreach (var dir in directions)
        {
            int newX = cella.x + dir[0];
            int newY = cella.y + dir[1];

            // Controlla che il vicino sia dentro i limiti della zona e della griglia
            if (newX >= min_x && newX <= max_x && newY >= min_y && newY <= max_y)
            {
                if (grid[newX, newY] == 0 && grid_path[newX, newY] == 0) // Solo celle valide e non visitate
                {
                    neighbors.Add(new Cell(newX, newY));
                }
            }
        }

        return neighbors;
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


    public void TrovaCelleRipetute(List<Cell> path)
    {
        Debug.Log("Celle attraversate almeno 3 volte:");
        bool trovato = false;

        // Lista per tenere traccia delle celle già controllate
        List<Cell> celleControllate = new List<Cell>();

        for (int i = 0; i < path.Count; i++)
        {
            Cell current = path[i];
            int count = 0;

            // Conta quante volte questa cella appare nel percorso
            for (int j = 0; j < path.Count; j++)
            {
                if (path[j].x == current.x && path[j].y == current.y)
                {
                    count++;
                }
            }

            // Se appare almeno 3 volte e non è già stata stampata, stampala
            if (count >= 3 && !celleControllate.Any(c => c.x == current.x && c.y == current.y))
            {
                string output = $"Cella ({current.x}, {current.y}) attraversata {count} volte.";
                Debug.Log(output); 
                celleControllate.Add(current);
                trovato = true;
            }
        }

        if (!trovato)
        {
            Debug.Log("Nessuna cella attraversata almeno 3 volte.");
        }
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
                        j = i; // Ripristina j per continuare il controllo
                    }
                    else if (grid_for_samp[percorsoSemplificato[j].x, percorsoSemplificato[j].y] != 0)
                    {
                        Debug.Log("Salto");
                        break; // Se troviamo una cella già visitata, interrompi la ricerca
                    }
                    else
                    {
                        j++;
                    }
                }
            }

            // Aggiungi al percorso di ritorno solo celle effettivamente valide
            if (grid_for_samp[percorsoSemplificato[i].x, percorsoSemplificato[i].y] != 0)
            {
                percorso_ritorno.Add(percorsoSemplificato[i]);
                grid_for_samp[percorsoSemplificato[i].x, percorsoSemplificato[i].y] = 0; // Segna come visitata
            }

            i++;
        }

        return percorso_ritorno;
    }


}
