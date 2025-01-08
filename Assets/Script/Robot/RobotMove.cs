using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;

public class RobotMovement : MonoBehaviour{

    // public Transform target;
    public Transform plane;
    private float cellSize = 1.0f;
    public DistanceSensor distanceSensor;

    public GripperController gripper;

    private List<Cell> path;
    private int currentIndex = 0;
    private AStar aStar;
    private int[,] grid;
    private DatabaseManager databaseManager;
    private int autoIncrementCell = 0, autoIncrementObstacle = 0;
    private Vector3 nextPosition;
    private float offsetX;
    private float offsetZ;
    private Vector2Int? target = null;

    async void Start(){
        GenerateGridFromPlane();
        aStar = new AStar(grid);
        nextPosition = transform.position;

        databaseManager = DatabaseManager.GetDatabaseManager();
 
        await databaseManager.ResetDatabaseAsync();

        // Carica i dati nella base di conoscenza
        await databaseManager.CreateZoneNodeAsync("Z1", 0, 0, 35, 35, false);
        await databaseManager.CreateZoneNodeAsync("Z2", 35, 0, 70, 35, false);
        await databaseManager.CreateZoneNodeAsync("Z3", 0, 35, 35, 70, false);
        await databaseManager.CreateZoneNodeAsync("Z4", 35, 35, 70, 70, false);
        await databaseManager.CreateRescueTeamAsync("P1","Vigile");

        await databaseManager.CreateRobotNodeAsync("R1", "Agente1", "C1", 20, 80);
        await databaseManager.CreateChargerBaseAsync("B1", "C1");
        await databaseManager.LoadDataFromCSV();

        offsetX = plane.localScale.x * 10f / 2f; // Metà larghezza fisica del piano
        offsetZ = plane.localScale.z * 10f / 2f; // Metà altezza fisica del piano
    }

    void OnApplicationQuit(){
        databaseManager.DestroyConnectionAsync();
    }

    async void Update(){
        if(target == null){
            string zone = await databaseManager.GetYoungestMissingPersonAsync();
            if(zone != null){
                target = FindCell(zone);
                if(target != null){
                    path = aStar.TrovaPercorso(RealToGrid(transform.position), (Vector2Int) target);
                    currentIndex = 0; 
                    Debug.Log($"Percorso calcolato. Lunghezza: {path.Count}");
                }
            }else{
                return;
            }
        }else{
            Debug.Log($"Nodo corrente: {currentIndex}/{path.Count}, Target Posizione: {nextPosition}");

            if (Vector3.Distance(transform.position, nextPosition) <= 0.2f){
                Cell nextCell = path[currentIndex];
                nextPosition = GridToReal(nextCell);

                autoIncrementCell++;
                await databaseManager.CreateCellNodeAsync($"C{autoIncrementCell}", nextCell.x, nextCell.y, FindZone(nextCell));
                Debug.Log($"OSTACOLOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO, {distanceSensor.currentDistance}");

                if(distanceSensor.currentDistance < 10f){                    
                    autoIncrementObstacle++;
                    grid[nextCell.x, nextCell.y] = 1;
                    Debug.Log("OSTACOLOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO");
                    await databaseManager.CreateObstacleNodeAsync($"O{autoIncrementObstacle}", $"C{autoIncrementCell}");
                    // path = null;
                }
                currentIndex++;
                if(currentIndex >= path.Count){
                    Debug.Log("Percorso completato!");
                    path = null;
                }
            }

            // Calcola la direzione verso il nodo
            Vector3 direzione = (nextPosition - transform.position).normalized;
            Quaternion rotazioneTarget = Quaternion.LookRotation(direzione);

            // Ruota verso il nodo
            transform.rotation = Quaternion.Slerp(transform.rotation, rotazioneTarget, 2f);
            transform.position += transform.forward * 2f * Time.deltaTime;
        }
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

    //identifica la posizione del target nella griglia 
    private Vector2Int RealToGrid(Vector3 position){

        // Calcola l'offset considerando la scala
        float offsetX = plane.localScale.x * 5f; // 7 * 5 = 35
        float offsetZ = plane.localScale.z * 5f; // 7 * 5 = 35

        int x = Mathf.FloorToInt((position.x + offsetX) / cellSize);
        int y = Mathf.FloorToInt((position.z + offsetZ) / cellSize);

        Debug.Log($"Posizione target fisica: {position}, Griglia: ({x}, {y})");
        return new Vector2Int(x, y);
    }

    private Vector3 GridToReal(Cell nextCell){
        return new Vector3((nextCell.x * cellSize) - offsetX, transform.position.y, (nextCell.y * cellSize) - offsetZ);
    }

    private string FindZone(Cell cell){
        int x = cell.x;
        int y = cell.y;

        return (x < 35) ? (y < 35 ? "Z1" : "Z3") : (y < 35 ? "Z2" : "Z4");
    }

    private Vector2Int FindCell(String zone){
        if(zone == "Z1"){
            return new Vector2Int(0, 0);
        }else if(zone == "Z2"){
            return new Vector2Int(35, 0);
        }else if(zone == "Z3"){
            return new Vector2Int(0, 35);
        }
        return new Vector2Int(35, 35);
    }
}