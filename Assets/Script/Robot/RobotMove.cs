using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.Experimental.GraphView;
using UnityEngine.PlayerLoop;

public class RobotMovement : MonoBehaviour{

    // public Transform target;
    public Transform plane;
    private float cellSize = 1.0f;
    public DistanceSensor distanceSensorCenter;
    public DistanceSensor distanceSensorLeft;
    public DistanceSensor distanceSensorRight;
    public float speed;
    public float rotationSpeed;
    public float obstacleDistance;


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
    private bool isProcessing = false;
    private Cell nextCell;

    async void Start(){
        offsetX = plane.localScale.x * 10f / 2f;
        offsetZ = plane.localScale.z * 10f / 2f;
        transform.position = GridToReal(new Cell(35, 35));
        GenerateGridFromPlane();
        aStar = new AStar();
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
    }

    void OnApplicationQuit(){
        databaseManager.DestroyConnectionAsync();
    }

    async void Update(){

        if (isProcessing) return;
        Vector3 direzioneTarget = nextPosition - transform.position;
        float angle = Vector3.SignedAngle(transform.forward, direzioneTarget, Vector3.up);
        Debug.Log($"angle {angle}");

        if(target == null){
            PathInitialization();
        }else{
            isProcessing = true;
            // Debug.Log($"Nodo corrente: {currentIndex}/{path.Count}, Target Posizione: {nextPosition}");
            // if (Vector3.Distance(transform.position, nextPosition) <= 0.1f){
            nextCell = path[currentIndex];
            nextPosition = GridToReal(nextCell);

            autoIncrementCell++;
            await databaseManager.CreateCellNodeAsync($"C{autoIncrementCell}", nextCell.x, nextCell.y, FindZone(nextCell));

            float centerDistance = distanceSensorCenter.currentDistance;
            float leftDistance = distanceSensorLeft.currentDistance;
            float rightDistance = distanceSensorRight.currentDistance;

            if(Mathf.Min(Mathf.Min(centerDistance, leftDistance), rightDistance) < obstacleDistance){                    
                autoIncrementObstacle++;
                grid[nextCell.x, nextCell.y] = 1;
                await databaseManager.CreateObstacleNodeAsync($"O{autoIncrementObstacle}", $"C{autoIncrementCell}");
                target = null;
                isProcessing = false;
                return;
            }
            
            if(currentIndex < path.Count-1){
                currentIndex++;
            }
            // }

            if((angle >= -90 && angle <= -1) || (angle >= 1 && angle <= 90)){
                distanceSensorCenter.isSensorActive = false;
                distanceSensorLeft.isSensorActive = false;
                distanceSensorRight.isSensorActive = false;
                Debug.Log("MOVIENTO ROTAZIONE");
                RotateRobot(direzioneTarget);
            }else{
                Debug.Log("MOVIENTO MOVE");
                Move(angle);
            }  
        }        
    }

    public async void PathInitialization(){
        string zone = await databaseManager.GetYoungestMissingPersonAsync();
        currentIndex = 0;
        if(zone != null){
            target = FindCell(zone);
            if(target != null){
                Debug.Log($"posizione robot fisica {transform.position}, posizione cella: {RealToGrid(transform.position)}, posizione target {target}");
                path = aStar.TrovaPercorso(RealToGrid(transform.position), (Vector2Int) target, grid);
                if(path == null){
                    target = null; //DA MODIFICAREEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE
                    return;
                }
                nextCell = path[currentIndex];
                nextPosition = GridToReal(nextCell);
                currentIndex++;
                Debug.Log($"Percorso calcolato. Lunghezza: {path.Count}");
            }
        }else{
            return;
        }
    }

    public void RotateRobot(Vector3 direction){
        // transform.position = GridToReal(path[currentIndex-2]);
        Quaternion rotazioneTarget = Quaternion.LookRotation(direction);
        transform.rotation = rotazioneTarget;// Quaternion.Slerp(transform.rotation, rotazioneTarget, rotationSpeed);
        isProcessing = false;
        distanceSensorCenter.isSensorActive = true;
        distanceSensorLeft.isSensorActive = true;
        distanceSensorRight.isSensorActive = true;
    }

    public void Move(float angle){
        float direction = (angle > -1 && angle < 1) ? 1 : -1;
        while(Vector3.Distance(transform.position, nextPosition) > 0.1f){
            transform.position += transform.forward * direction * speed * Time.deltaTime;
        }
        isProcessing = false;
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

    private Vector2Int RealToGrid(Vector3 position){
        return new Vector2Int(Mathf.FloorToInt(position.x + offsetX), Mathf.FloorToInt(position.z + offsetZ));
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