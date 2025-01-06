// using UnityEngine;

// public class RobotMovement : MonoBehaviour
// {
//     public float moveSpeed = 5f;
//     public float rotationSpeed = 100f;

//     private Rigidbody rb;

//     void Start()
//     {
//         // Ottieni il componente Rigidbody
//         rb = GetComponent<Rigidbody>();
//     }

//     void FixedUpdate()
//     {
//         // Ricevi input dell'utente
//         float moveInput = Input.GetAxis("Vertical"); 
//         float turnInput = Input.GetAxis("Horizontal");
//         // Movimento avanti/indietro
//         Vector3 moveDirection = transform.forward * moveInput * moveSpeed;
//         rb.MovePosition(rb.position + moveDirection * Time.fixedDeltaTime);

//         // Rotazione del robot
//         float rotation = turnInput * rotationSpeed * Time.fixedDeltaTime;
//         Quaternion turnRotation = Quaternion.Euler(0, rotation, 0);
//         rb.MoveRotation(rb.rotation * turnRotation);
//     }
// }


// using UnityEngine;

// public class RobotMovement : MonoBehaviour
// {
//     public float moveSpeed = 5f;
//     public float rotationSpeed = 100f;

//     private Rigidbody rb;

//     void Start()
//     {
//         // Ottieni il componente Rigidbody
//         rb = GetComponent<Rigidbody>();
//     }

//     void FixedUpdate()
//     {
//         // Ricevi input dell'utente
//         float moveInput = Input.GetAxis("Vertical"); 
//         float turnInput = Input.GetAxis("Horizontal");
//         // Movimento avanti/indietro
//         Vector3 moveDirection = transform.forward * moveInput * moveSpeed;
//         rb.MovePosition(rb.position + moveDirection * Time.fixedDeltaTime);

//         // Rotazione del robot
//         float rotation = turnInput * rotationSpeed * Time.fixedDeltaTime;
//         Quaternion turnRotation = Quaternion.Euler(0, rotation, 0);
//         rb.MoveRotation(rb.rotation * turnRotation);
//     }
// }

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class RobotMovement : MonoBehaviour{

     public Transform target;           // Obiettivo del movimento
    public Transform plane;            // Piano per la griglia
    public float cellSize = 1.0f;      // Dimensione delle celle della griglia

    private UnityEngine.AI.NavMeshAgent agent;        // NavMeshAgent per il movimento
    private List<Nodo> path;           // Percorso calcolato con A*
    private int currentIndex = 0;      // Nodo attuale
    private AStar aStar;               // Algoritmo A*
    private int[,] grid;               // Griglia

    void Start()
    {
        // // Inizializza l'agente
        // agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        // if (agent == null)
        // {
        //     Debug.LogError("NavMeshAgent non trovato!");
        //     return;
        // }

        // agent.enabled = true; // Attiva l'agente

        // Ottieni la dimensione del robot (Collider o Renderer)
        Collider robotCollider = GetComponent<Collider>();
        if (robotCollider != null)
        {
            // Usa la dimensione massima tra larghezza (x) e profondità (z) e imposta la dimensione della cella pari alla dimensione del robot
            cellSize = Mathf.Max(robotCollider.bounds.size.x, robotCollider.bounds.size.z);
            Debug.Log($"Dimensione cella calcolata: {cellSize}");
        }
        else
        {
            Debug.LogError("Il robot non ha un Collider per calcolare la dimensione della cella!");
        }

        
        GenerateGridFromPlane();
        aStar = new AStar(grid);

        // Calcola il percorso iniziale
        Vector2Int posizionerobotGriglia = PosizioneRobotInGriglia();

        CalcolaNuovoPercorso(posizionerobotGriglia);
    }




    void Update(){
        if (target == null){
            Debug.LogError("Nessun target assegnato!");
            return;
        }

        if (path == null || path.Count == 0 || currentIndex >= path.Count)
        {
            Debug.Log("Percorso completato!");
            return;
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
    private Vector2Int PosizioneRobotInGriglia(){
        //per calcolare la posizione del robot devo calcolare l'offset della griglia che dipende dalla sua scala e poi sottarlo alla posizione fisica del robot e dividere per la grandezza della cella impostato sulla gradnezza del robot 
        //al momento la griglia ha dimensione 70x70

        // Controlla e riallinea l'agente sulla NavMesh
        // UnityEngine.AI.NavMeshHit hit;
        // if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
        // {
        //     agent.Warp(hit.position);
        //     Debug.Log($"Agente riallineato sulla NavMesh {transform.position}.");
        // }
        // else
        // {
        //     Debug.LogError("Il robot non può essere posizionato sulla NavMesh!");
        // }


        // Calcola l'offset considerando la scala
        float offsetX = plane.localScale.x * 5f; // 7 * 5 = 35
        float offsetZ = plane.localScale.z * 5f; // 7 * 5 = 35

        int x = Mathf.FloorToInt((transform.position.x + offsetX) / cellSize);
        int y = Mathf.FloorToInt((transform.position.z + offsetZ) / cellSize);


        Debug.Log($"Posizione robot fisica: {transform.position}, Griglia: ({x}, {y})");
        return new Vector2Int(x, y);

    }


    //identifica la posizione del target nella griglia 
    private Vector2Int PosizioneTargetInGriglia(){
        if (target == null)
        {
            Debug.LogError("Target non assegnato!");
            return Vector2Int.zero;
        }

        // UnityEngine.AI.NavMeshHit hit;
        // if (UnityEngine.AI.NavMesh.SamplePosition(target.position, out hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
        // {
        //     target.position = hit.position;
        //     Debug.Log($"Target riallineato sulla NavMesh: {target.position}");
        // }
        // else
        // {
        //     Debug.LogError("Il target non può essere posizionato sulla NavMesh!");
        // }


        // Calcola l'offset considerando la scala
        float offsetX = plane.localScale.x * 5f; // 7 * 5 = 35
        float offsetZ = plane.localScale.z * 5f; // 7 * 5 = 35

        int x = Mathf.FloorToInt((target.position.x + offsetX) / cellSize);
        int y = Mathf.FloorToInt((target.position.z + offsetZ) / cellSize);


        Debug.Log($"Posizione target fisica: {target.position}, Griglia: ({x}, {y})");
        return new Vector2Int(x, y);
    }



    //trova il percorso migliore per raggiungere il target
    private void CalcolaNuovoPercorso(Vector2Int posizioneCorrente){
        Vector2Int destinazione = PosizioneTargetInGriglia();

        // Debug informazioni di partenza e destinazione
        Debug.Log($"Partenza: {posizioneCorrente}, Destinazione: {destinazione}");

        // //Controlla se la destinazione è valida
        // if (destinazione.x < 0 || destinazione.y < 0 || destinazione.x >= grid.GetLength(0) || destinazione.y >= grid.GetLength(1)){
        //     Debug.LogError($"Il target è fuori dai limiti della griglia! ({destinazione.x}, {destinazione.y})");
        //     return;
        // }

        // Controlla se il percorso è calcolabile
        path = aStar.TrovaPercorso(posizioneCorrente, destinazione);
        currentIndex = 0; // Ripristina l'indice

        if (path == null || path.Count == 0)
        {
            Debug.LogError("Nessun percorso disponibile!");
        }
        else
        {
            Debug.Log($"Percorso calcolato. Lunghezza: {path.Count}");
        }
    }



    private void MuoviVersoProssimaPosizione()
{
    // Controlla se il percorso è valido
    if (path == null || path.Count == 0)
    {
        Debug.LogError("Percorso non valido o vuoto!");
        //return;
    }

    // Se ha raggiunto la fine del percorso
    if (currentIndex >= path.Count)
    {
        Debug.Log("Percorso completato!");
        //return;
    }

    float offsetX = (plane.localScale.x * 10f) / 2f; // Metà larghezza fisica del piano
    float offsetZ = (plane.localScale.z * 10f) / 2f; // Metà altezza fisica del piano


    // Ottieni il nodo corrente
    Nodo currentNode = path[currentIndex];
    Vector3 targetPosition = new Vector3(
        (currentNode.x * cellSize) - offsetX,  // Converti X
        transform.position.y,                  // Mantieni altezza attuale
        (currentNode.y * cellSize) - offsetZ   // Converti Z
    );


    // Debug della posizione calcolata
    Debug.Log($"Nodo corrente: {currentIndex}/{path.Count}, Target Posizione: {targetPosition}");

    // Calcola la direzione verso il nodo
    Vector3 direzione = (targetPosition - transform.position).normalized;
    Quaternion rotazioneTarget = Quaternion.LookRotation(direzione);

    // Ruota verso il nodo
    transform.rotation = Quaternion.Slerp(
        transform.rotation,
        rotazioneTarget,
        2f // Valore per rotazione fluida
    );
    transform.position += transform.forward * 2f * Time.deltaTime;

    Debug.Log($"Nodo raggiunto: ({currentNode.x}, {currentNode.y})");
    if (Vector3.Distance(transform.position, targetPosition) < 1f){
            currentIndex++; // Passa al prossimo nodo
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

