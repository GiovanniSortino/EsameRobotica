using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class AstarRobotMove : MonoBehaviour{

    public float moveSpeed = 20f;         // Velocità di movimento
    public float rotationSpeed = 20f;   // Velocità di rotazione

    // Parametri per la griglia
    public Transform plane;             // Il riferimento al Plane nella scena
    public float cellSize = 1.0f;       // Dimensione di una cella nella griglia

    public Transform target; // Obiettivo dinamico: indica il receiving cpr creato da giovanni, dopo dovra essere il risultato della query che permette di trovare la zona in cui si trova il tizio piu giovane


    // Variabili per il percorso e la gestione della griglia
    private List<Nodo> path;            // Percorso calcolato
    private int currentIndex = 0;       // Nodo attuale nel percorso
    private int[,] grid;                // Matrice che rappresenta la griglia (0 = libero, 1 = ostacolo)
    private bool[,] visitato;           // Matrice che tiene traccia delle celle visitate
    private AStar aStar;                // Algoritmo A* per calcolare i percorsi

    void Start(){
        if (target == null){ //creo il target dinamicamente, dopo sarà da prendere dalle query
            GameObject nuovoTarget = GameObject.CreatePrimitive(PrimitiveType.Cube); // Crea un cubo
            nuovoTarget.name = "ReceivingCPR"; // Assegna un nome
            nuovoTarget.transform.position = new Vector3(5, 0, 5); // Posizionalo nella scena
            target = nuovoTarget.transform; // Assegna il transform al target
            Debug.Log($"Target creato dinamicamente: {target.position}");
        }
        
        GenerateGridFromPlane();
        Vector2Int start = PosizioneRobotInGriglia(transform.position);
        aStar = new AStar(grid);

        // Calcolo percorso iniziale
        CalcolaNuovoPercorso(new Vector2Int(0, 0));
    }

    void Update(){
        if (target == null){
            Debug.LogError("Nessun target assegnato!");
            return;
        }

        // Controlla se il target è cambiato posizione
        Vector2Int destinazione = PosizioneTargetInGriglia();
        if (path == null || currentIndex >= path.Count || !destinazione.Equals(PosizioneTargetInGriglia())){
            Vector2Int posizioneCorrente = PosizioneRobotInGriglia(transform.position);
            CalcolaNuovoPercorso(posizioneCorrente); // Ricalcola il percorso
        }

        // Muove verso la prossima posizione
        MuoviVersoProssimaPosizione();
    }


    //crea la griglia 
    public void GenerateGridFromPlane(){
        if (plane == null){
            Debug.Log("Il piano è nullo");
            return;
        }
        Collider planeCollider = plane.GetComponent<Collider>();
        if (planeCollider == null){
            Debug.LogError("Il plane deve avere un Collider!");
            return;
        }
        Vector3 planeScale = plane.localScale;
        float width = 10 * planeScale.x;
        float height = 10 * planeScale.z;

        int gridWidth = Mathf.CeilToInt(width / cellSize);
        int gridHeight = Mathf.CeilToInt(height / cellSize);

        grid = new int[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++){
            for (int y = 0; y < gridHeight; y++){
                grid[x, y] = 0; // Celle libere
            }
        }

        // Stampa la dimensione della griglia
        Debug.Log($"Dimensioni griglia: {gridWidth} x {gridHeight}");
    }

    //identifica la posizione del robot nella griglia 
    private Vector2Int PosizioneRobotInGriglia(Vector3 posizioneFisica){
        // Calcola la scala del Plane
        Vector3 planeScale = plane.localScale;

        // Calcola la posizione nella griglia, tenendo conto della scala
        int x = Mathf.FloorToInt((posizioneFisica.x - plane.position.x) / (cellSize * planeScale.x));
        int y = Mathf.FloorToInt((posizioneFisica.z - plane.position.z) / (cellSize * planeScale.z));

        // Clamping per rimanere nei limiti della griglia
        x = Mathf.Clamp(x, 0, grid.GetLength(0) - 1);
        y = Mathf.Clamp(y, 0, grid.GetLength(1) - 1);

        Debug.Log($"Posizione robot fisica: {posizioneFisica}, Griglia: ({x}, {y})");
        return new Vector2Int(x, y);
    }

    //identifica la posizione del target nella griglia 
    private Vector2Int PosizioneTargetInGriglia(){
        if (target == null){
            Debug.LogError("Target non assegnato!");
            return Vector2Int.zero;
        }

        Vector3 planeScale = plane.localScale;

        int x = Mathf.FloorToInt((target.position.x - plane.position.x) / (cellSize * planeScale.x));
        int y = Mathf.FloorToInt((target.position.z - plane.position.z) / (cellSize * planeScale.z));

        x = Mathf.Clamp(x, 0, grid.GetLength(0) - 1);
        y = Mathf.Clamp(y, 0, grid.GetLength(1) - 1);

        Debug.Log($"Posizione target fisica: {target.position}, Griglia: ({x}, {y})");
        return new Vector2Int(x, y);
    }

    //trova il percorso migliore per raggiungere il target
    private void CalcolaNuovoPercorso(Vector2Int posizioneCorrente){
        Vector2Int destinazione = PosizioneTargetInGriglia();

        // Controlla se la destinazione è valida
        if (grid[destinazione.x, destinazione.y] == 1){ // 1 = ostacolo
            Debug.LogError($"Il target ({destinazione.x}, {destinazione.y}) si trova su un ostacolo!");
            return;
        }

        // Calcola il percorso
        path = aStar.TrovaPercorso(posizioneCorrente, destinazione);
        currentIndex = 0; // Ripristina l'indice

        if (path == null || path.Count == 0){
            Debug.LogError("Nessun percorso disponibile!");
        }else{
            Debug.Log($"Percorso calcolato. Lunghezza: {path.Count}");
        }
    }


    //si sposta verso la nuova posizione
    private void MuoviVersoProssimaPosizione(){
        if (path == null || currentIndex >= path.Count){
            Debug.LogWarning("Percorso vuoto o completato.");
            return;
        }

        Nodo currentNode = path[currentIndex];
        Vector2Int targetGrid = PosizioneTargetInGriglia();
        Vector3 planeScale = plane.localScale;

        // Converte la posizione nella griglia al mondo reale
        Vector3 targetPosition = new Vector3(
            targetGrid.x * cellSize * planeScale.x + plane.position.x,
            transform.position.y, // Mantieni l'altezza originale
            targetGrid.y * cellSize * planeScale.z + plane.position.z
        );

        Debug.Log($"Muovo verso {targetPosition}, Posizione attuale: {transform.position}");

        // Movimento verso la destinazione
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);


        // Rotazione verso la destinazione
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero){
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }

        // Passa al nodo successivo se raggiunge la destinazione
        float soglia = Mathf.Max(0.5f * cellSize, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPosition) < soglia){
            Debug.Log($"Raggiunto nodo: ({currentNode.x}, {currentNode.y})");
            currentIndex++;
        }

    }
}


public class Nodo{
    public int x, y;                  // Coordinate nella griglia
    public float gCost;               // Costo dal nodo di partenza
    public float hCost;               // Costo stimato al nodo di destinazione
    public float fCost => gCost + hCost; // Costo totale (g + h)
    public Nodo parent;               // Nodo precedente (per ricostruire il percorso)

    public Nodo(int x, int y){
        this.x = x;
        this.y = y;
    }
}

public class AStar{
    private int[,] grid;              // Griglia (0 = libero, 1 = ostacolo)
    private int rows, cols;           // Dimensioni della griglia

    // Direzioni per esplorare i vicini (su, giù, sinistra, destra, diagonali)
    private Vector2Int[] directions = {
        new Vector2Int(0, 1), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(-1, 0),
        new Vector2Int(1, 1), new Vector2Int(1, -1),
        new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };
    public AStar(int[,] grid){
        this.grid = grid;
        rows = grid.GetLength(0); // Larghezza
        cols = grid.GetLength(1); // Altezza
        
    }

    private List<Nodo> RicostruisciPercorso(Nodo node){
        List<Nodo> path = new List<Nodo>();
        while (node != null){
            path.Add(node);   // Aggiunge il nodo al percorso
            node = node.parent; // Torna al parent
        }
        path.Reverse(); // Inverte l'ordine per ottenere il percorso giusto
        return path;
    }

    private float CalcolaDistanza(Vector2Int a, Vector2Int b){
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private bool PosizioneValida(Vector2Int pos){
        // Controlla che la posizione sia nei limiti della griglia e non sia un ostacolo
        return pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols && grid[pos.x, pos.y] == 0;
    }


    public List<Nodo> TrovaPercorso(Vector2Int start, Vector2Int goal){
        // Liste di nodi aperti (da esplorare) e chiusi (già esplorati)
        List<Nodo> openList = new List<Nodo>();
        HashSet<Nodo> closedList = new HashSet<Nodo>();

        // Inizializza il nodo di partenza
        Nodo startNode = new Nodo(start.x, start.y) { gCost = 0, hCost = CalcolaDistanza(start, goal) };
        Debug.Log("Inizializzo il nodo di partenza");
        openList.Add(startNode);

        while (openList.Count > 0){
            // Trova il nodo con il costo F più basso
            Nodo currentNode = openList.OrderBy(n => n.fCost).First();
            Debug.Log($"Nodo Esplorato: ({currentNode.x}, {currentNode.y}), fCost: {currentNode.fCost}");

            // Se raggiunge il nodo di destinazione, costruisce il percorso
            if (currentNode.x == goal.x && currentNode.y == goal.y)
                Debug.Log("Sto ricostruendo il percorso");
                return RicostruisciPercorso(currentNode);

            // Sposta il nodo dalla lista aperta a quella chiusa
            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // Esplora tutti i vicini del nodo corrente
            foreach (var direction in directions){
                Vector2Int neighborPos = new Vector2Int(currentNode.x + direction.x, currentNode.y + direction.y);

                // Salta i vicini non validi
                if (!PosizioneValida(neighborPos))
                    continue;

                Nodo neighbor = new Nodo(neighborPos.x, neighborPos.y) { gCost = float.MaxValue };

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
