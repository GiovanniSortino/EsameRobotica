using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.Experimental.GraphView;
using UnityEngine.PlayerLoop;
using Palmmedia.ReportGenerator.Core;
using Unity.Mathematics;

//DEVO ESSERCI 1

public class RobotMovement : MonoBehaviour{

    public Transform plane;
    private float cellSize = 1.0f;
    public MixDistance distanceSensorLeft;
    public MixDistance distanceSensorForward;
    public MixDistance distanceSensorRight;
    public float speed;
    public float rotationSpeed;
    public float obstacleDistance;
    public GripperController gripper;
    private List<Cell> path;
    private int currentIndex;
    private AStar aStar;
    private int[,] grid;
    private DatabaseManager databaseManager;
    private int autoIncrementCell = 0, autoIncrementObstacle = 0;
    private Vector3 nextPosition;
    private float offsetX;
    private float offsetZ;
    private Vector2Int? target = null;
    private Cell nextCell;
    private string nextMove = null;
    private string zone;
    private float distance = Mathf.Infinity;
    private Vector2Int obstacleCell = new Vector2Int(0, 0);
    public Text actionText;

    private ProgramState currentState = ProgramState.FindZone;

    public enum ProgramState{
        FindZone,
        WaitResponse,
        StartCalculation,
        Calculating,
        DistanceCalculation,
        Wait,
        DetectObstacle,
        FoundNextMove,
        CalculationgNextMove,
        Move
    }


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
        if(currentState == ProgramState.FindZone){
            currentState = ProgramState.WaitResponse;
            zone = null;
            zone = await databaseManager.GetYoungestMissingPersonAsync();
        }else if(currentState == ProgramState.WaitResponse){
            if(zone != null) currentState = ProgramState.StartCalculation;
        }else if(currentState == ProgramState.StartCalculation){
            nextMove = null;
            CalculatePath();
        }else if(currentState == ProgramState.Calculating){
            nextMove = null;
            if(path != null) currentState = ProgramState.DistanceCalculation;
        }else if(currentState == ProgramState.DistanceCalculation){
            currentState = ProgramState.Wait;
            DistanceCalculation();
        }else if(currentState == ProgramState.DetectObstacle){
            currentState = DetectObstacle() ? ProgramState.FindZone : ProgramState.FoundNextMove;
        }else if(currentState == ProgramState.FoundNextMove){
            currentState = ProgramState.CalculationgNextMove;
            nextMove = null;
            NextMovement();
            if(nextMove == "NON ADIACENTI"){
                currentState = ProgramState.FindZone;
            }
            Debug.Log(nextMove);
        }else if(currentState == ProgramState.CalculationgNextMove){
            if(nextMove != null) currentState = ProgramState.Move;
        }else if(currentState == ProgramState.Move){
            Move();
        }
    }

    public void CalculatePath(){
        currentIndex = 1;
        if(zone != null){
            target = FindCell(zone);
            if(target != null){
                currentState = ProgramState.Calculating;
                path = aStar.TrovaPercorso(RealToGrid(transform.position), (Vector2Int) target, grid);
            }
        }else{
            return;
        }
    }

    public void DistanceCalculation(){
        string activeSensor = OrientationToDirection();

        Vector2Int robotPosition = RealToGrid(transform.position);
        int robotOrientation = RobotOrientation();

        Debug.Log($"Active Sensor: {activeSensor}");

        if(activeSensor == "left"){
            distance = distanceSensorLeft.distance();
            obstacleCell =  robotOrientation switch{
                0 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Nord
                90 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Est
                180 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Sud
                270 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Ovest
                _ => robotPosition // Orientamento non valido
            };
        }else if(activeSensor == "forward"){
            distance = distanceSensorForward.distance();
            obstacleCell =  robotOrientation switch{
                0 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Nord
                90 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Est
                180 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Sud
                270 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Ovest
                _ => robotPosition // Orientamento non valido
            };
        }else if(activeSensor == "right"){
            distance = distanceSensorRight.distance();
            obstacleCell =  robotOrientation switch{
                0 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Nord
                90 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Est
                180 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Sud
                270 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Ovest
                _ => robotPosition // Orientamento non valido
            };
        }
        currentState = ProgramState.DetectObstacle;
    }

    public bool DetectObstacle(){
        currentState = ProgramState.Wait;
        if(distance < obstacleDistance){
            Debug.Log($"Ostacolo, distanza {distance}");
            distance = Mathf.Infinity;

            Debug.Log($"OSTACOLO IN {obstacleCell.x}, {obstacleCell.y}");
            autoIncrementObstacle++;
            grid[obstacleCell.x, obstacleCell.y] = 1;

            _ = databaseManager.CreateObstacleNodeAsync($"O{autoIncrementObstacle}", $"C{autoIncrementCell}");
            return true;
        }
        return false;
    }

    string RelativeOrientation(Vector2Int primaCella, Vector2Int secondaCella){
        int deltaX = secondaCella.x - primaCella.x;
        int deltaY = secondaCella.y - primaCella.y;

        if (deltaX == 0 && deltaY >= 0) return "down";
        if (deltaX == 0 && deltaY < 0) return "up";
        if (deltaX >= 0 && deltaY == 0) return "right";
        if (deltaX < 0 && deltaY == 0) return "left";

        return "Non adiacente";
    }

    public void NextMovement(){

        Debug.Log($"NodoGriglia corrente: {currentIndex}/{path.Count}, Posizione corrente {transform.position}, Target Posizione: {nextPosition}");
        nextCell = path[currentIndex];
        nextPosition = GridToReal(nextCell);
        autoIncrementCell++;

        _ = databaseManager.CreateCellNodeAsync($"C{autoIncrementCell}", nextCell.x, nextCell.y, FindZone(nextCell));
        nextMove = OrientationToDirection();
        actionText.text = "Azione: " + nextMove;

        Debug.Log($"next move {nextMove}");
    }

    public int RobotOrientation(){
        int[] targetAngles = { 0, 90, 180, 270 };

        int robotOrientation = 0;
        int minDifference = int.MaxValue;

        foreach (int angle in targetAngles){
            int difference = Mathf.Abs(angle - (int)transform.eulerAngles.y);
            if (difference < minDifference){
                minDifference = difference;
                robotOrientation = angle;
            }
        }
        return robotOrientation;
    }

    public void Move(){
        float x_rotation = transform.eulerAngles.x;
        float y_rotation = transform.eulerAngles.y;
        float z_rotation = transform.eulerAngles.z;

        if(nextMove == "left"){
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation - 90, z_rotation);
            currentState = ProgramState.DistanceCalculation;
        }else if(nextMove == "right"){
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + 90, z_rotation);
            currentState = ProgramState.DistanceCalculation;
        }else if(nextMove == "forward"){
            transform.position += transform.forward * speed * Time.deltaTime;
            if(Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                currentState = ProgramState.DistanceCalculation;
                if(currentIndex < path.Count-1){
                    currentIndex++;
                }else{
                    currentState = ProgramState.Wait;
                }
            }
        }else if(nextMove == "backward"){
            transform.position -= transform.forward * speed * Time.deltaTime;
            Debug.Log($"robot position: {RealToGrid(transform.position)}; {transform.position}; {RealToGrid(nextPosition)}; {nextPosition}; distanza {Vector3.Distance(transform.position, nextPosition)}");
            if(Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                currentState = ProgramState.DistanceCalculation;
            }
        }
    }

    public string OrientationToDirection(){
        Vector2Int robotCell = RealToGrid(transform.position);
        Vector2Int targetCell = new Vector2Int(path[currentIndex].x, path[currentIndex].y);

        string relativeOrientation = RelativeOrientation(robotCell, targetCell);
        int robotOrientation = RobotOrientation();

        Debug.Log($"relative orientation {relativeOrientation}");

        switch (relativeOrientation){
            case "down":
                if (robotOrientation == 0) return "forward";
                else if (robotOrientation == 180) return "backward";
                else if (robotOrientation == 90) return "left";
                else if (robotOrientation == 270) return "right";
                break;

            case "up":
                if (robotOrientation == 180) return "forward";
                else if (robotOrientation == 0) return "backward";
                else if (robotOrientation == 90) return "right";
                else if (robotOrientation == 270) return "left";
                break;

            case "left":
                if (robotOrientation == 90) return "backward";
                else if (robotOrientation == 270) return "forward";
                else if (robotOrientation == 0) return "left";
                else if (robotOrientation == 180) return "right";
                break;

            case "right":
                if (robotOrientation == 270) return "backward";
                else if (robotOrientation == 90) return "forward";
                else if (robotOrientation == 0) return "right";
                else if (robotOrientation == 180) return "left";
                break;

            default:
                break;
        }
        return "NON ADIACENTI";
    }

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
                grid[x, y] = 0;
            }
        }
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

    private void printGrid(int[,] matrice, Vector2Int posRobot){
        int righe = matrice.GetLength(0);
        int colonne = matrice.GetLength(1);

        for (int i = 0; i < righe; i++){
            string riga = "";
            for (int j = 0; j < colonne; j++){
                if(posRobot.x == j && posRobot.y == i){
                    riga += "R ";
                }else{
                    riga += matrice[i, j] + " ";
                }
            }
            Debug.Log(riga);
        }
    }
}
