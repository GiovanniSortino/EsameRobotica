using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AStar{
    private int rows, cols;           // Dimensioni della griglia

    // Direzioni per esplorare i vicini (su, giù, sinistra, destra, diagonali)
    private Vector2Int[] directions = {
        new Vector2Int(0, 1), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(-1, 0)//,
        // new Vector2Int(1, 1), new Vector2Int(1, -1),
        // new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private List<Cell> RicostruisciPercorso(Cell cell){
        List<Cell> path = new List<Cell>();
        while (cell != null){
            path.Add(cell);   // Aggiunge il nodo al percorso
            cell = cell.parent; // Torna al parent
        }
        path.Reverse(); // Inverte l'ordine per ottenere il percorso giusto
        return path;
    }

    private float CalcolaDistanza(Vector2Int a, Vector2Int b){
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private bool PosizioneValida(Vector2Int pos, int[,] grid){
        rows = grid.GetLength(0);
        cols = grid.GetLength(1);
        bool result = pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols && grid[pos.x, pos.y] == 0;
        // Debug.Log($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA {result}, {pos.x}, {pos.y}, {grid[pos.x, pos.y]}");
        // Controlla che la posizione sia nei limiti della griglia e non sia un ostacolo
        return result;
    }


    public List<Cell> TrovaPercorso(Vector2Int start, Vector2Int goal, int[,] grid){
        // Liste di nodi aperti (da esplorare) e chiusi (già esplorati)
        List<Cell> openList = new List<Cell>();
        HashSet<Cell> closedList = new HashSet<Cell>();

        // Inizializza il nodo di partenza
        Cell startNode = new Cell(start.x, start.y) { gCost = 0, hCost = CalcolaDistanza(start, goal) };
        Debug.Log("Inizializzo il nodo di partenza");
        openList.Add(startNode);

        while (openList.Count > 0){
            // Trova il nodo con il costo F più basso
            Cell currentNode = openList.OrderBy(n => n.fCost).First();
            // Debug.Log($"Nodo Esplorato: ({currentNode.x}, {currentNode.y}), fCost: {currentNode.fCost}");

            // Se raggiunge il nodo di destinazione, costruisce il percorso
            if (currentNode.x == goal.x && currentNode.y == goal.y){
                Debug.Log("Sto ricostruendo il percorso");
                return RicostruisciPercorso(currentNode);
            }

            // Sposta il nodo dalla lista aperta a quella chiusa
            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // Esplora tutti i vicini del nodo corrente
            foreach (var direction in directions){
                Vector2Int neighborPos = new Vector2Int(currentNode.x + direction.x, currentNode.y + direction.y);

                // Salta i vicini non validi
                if (!PosizioneValida(neighborPos, grid))
                    continue;

                Cell neighbor = new Cell(neighborPos.x, neighborPos.y) { gCost = float.MaxValue };

                // Se il vicino è già esplorato, lo salta
                if (closedList.Any(n => n.x == neighbor.x && n.y == neighbor.y))
                    continue;

                // Calcola il costo G per il vicino
                float tentativeGCost = currentNode.gCost + CalcolaDistanza(new Vector2Int(currentNode.x, currentNode.y), neighborPos);

                // Se è un percorso migliore o il nodo non è ancora nella lista aperta
                if (tentativeGCost < neighbor.gCost || !openList.Any(n => n.x == neighbor.x && n.y == neighbor.y)){
                    neighbor.gCost = tentativeGCost; // Aggiorna il costo G
                    neighbor.hCost = CalcolaDistanza(neighborPos, goal); // Aggiorna il costo H
                    neighbor.parent = currentNode; // Salva il nodo parent

                    // Aggiunge il vicino alla lista aperta se non è già presente
                    if (!openList.Any(n => n.x == neighbor.x && n.y == neighbor.y))
                        openList.Add(neighbor);
                }
            }
        }


        // Nessun percorso trovato
        return null;
    }
}

