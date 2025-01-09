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

    private ProgramState currentState = ProgramState.StartCalculation;

    public enum ProgramState{
        StartCalculation,
        Calculating,
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
        if(currentState == ProgramState.StartCalculation){
            nextMove = null;
            CalculatePath();
        }else if(currentState == ProgramState.Calculating){
            nextMove = null;
            if(path != null) currentState = ProgramState.FoundNextMove;
        }else if(currentState == ProgramState.FoundNextMove){
            currentState = ProgramState.CalculationgNextMove;
            nextMove = null;
            nextMove = NextMovement();
            Debug.Log(nextMove);
            DetectObstacle();
            if(currentIndex < path.Count-1){
                currentIndex++;
            }
        }else if(currentState == ProgramState.CalculationgNextMove){
            if(nextMove != null) currentState = ProgramState.Move;
        }else if(currentState == ProgramState.Move){
            Move();
        }     
    }

    public async void CalculatePath(){
        string zone = await databaseManager.GetYoungestMissingPersonAsync();
        currentIndex = 1;
        if(zone != null){
            target = FindCell(zone);
            if(target != null){
                Debug.Log($"posizione robot fisica {transform.position}, posizione cella: {RealToGrid(transform.position)}, posizione target {target}");
                path = aStar.TrovaPercorso(RealToGrid(transform.position), (Vector2Int) target, grid);
                currentState = ProgramState.Calculating;
            }
        }else{
            return;
        }
    }

    public async void DetectObstacle(){
        float centerDistance = distanceSensorCenter.currentDistance;
        float leftDistance = distanceSensorLeft.currentDistance;
        float rightDistance = distanceSensorRight.currentDistance;

        float min = Mathf.Min(Mathf.Min(centerDistance, leftDistance), rightDistance);

        if(min < obstacleDistance){
            Debug.Log($"Ostacolo, distanza {min}");          
            autoIncrementObstacle++;
            grid[nextCell.x, nextCell.y] = 1;
            //printGrid(grid, RealToGrid(transform.position));
            await databaseManager.CreateObstacleNodeAsync($"O{autoIncrementObstacle}", $"C{autoIncrementCell}");
            currentState = ProgramState.StartCalculation;
        }
    }

    string RelativeOrientation(Vector2Int primaCella, Vector2Int secondaCella){
        int deltaX = secondaCella.x - primaCella.x;
        int deltaY = secondaCella.y - primaCella.y;

        if (deltaX == 0 && deltaY >= 0) return "up";
        if (deltaX == 0 && deltaY < 0) return "down";
        if (deltaX >= 0 && deltaY == 0) return "right";
        if (deltaX < 0 && deltaY == 0) return "left";

        Debug.LogError("NON ADIACENTI");
        return "Non adiacente";
    }

    public string NextMovement(){

        Debug.Log($"NodoGriglia corrente: {currentIndex}/{path.Count}, Posizione corrente {transform.position}, Target Posizione: {nextPosition}");
        nextCell = path[currentIndex];
        nextPosition = GridToReal(nextCell);

        autoIncrementCell++;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //_ = databaseManager.CreateCellNodeAsync($"C{autoIncrementCell}", nextCell.x, nextCell.y, FindZone(nextCell)); //ATTENZIONE HO TOLTO AWAIT
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        Vector2Int robotCell = RealToGrid(transform.position);
        Vector2Int targetCell = new Vector2Int(path[currentIndex].x, path[currentIndex].y);

        string relativeOrientation = RelativeOrientation(robotCell, targetCell);
        int robotOrientation = RobotOrientation();

        switch (relativeOrientation){
            case "up":
                if (robotOrientation == 0) return "forward";
                else if (robotOrientation == 180) return "backward";
                else if (robotOrientation == 90) return "left";
                else if (robotOrientation == 270) return "right";
                break;

            case "down":
                if (robotOrientation == 180) return "forward";
                else if (robotOrientation == 0) return "backward";
                else if (robotOrientation == 90) return "right";
                else if (robotOrientation == 270) return "left";
                break;

            case "left":
                if (robotOrientation == 90) return "forward";
                else if (robotOrientation == 270) return "backward";
                else if (robotOrientation == 0) return "right";
                else if (robotOrientation == 180) return "left";
                break;

            case "right":
                if (robotOrientation == 270) return "forward";
                else if (robotOrientation == 90) return "backward";
                else if (robotOrientation == 0) return "left";
                else if (robotOrientation == 180) return "right";
                break;

            default:
                Debug.Log($"La direzione relativa non è valida o le celle non sono adiacenti. {robotCell}, {targetCell}");
                break;
        }

        return null;
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
        float x_rotation = transform.rotation.x;
        float y_rotation = transform.rotation.y;
        float z_rotation = transform.rotation.z;
        float w_rotation = transform.rotation.w;
        if(nextMove == "left"){
            transform.rotation = new Quaternion(x_rotation, y_rotation + 90, z_rotation, w_rotation);
            currentState = ProgramState.FoundNextMove;
        }else if(nextMove == "right"){
            transform.rotation = new Quaternion(x_rotation, y_rotation - 90, z_rotation, w_rotation);
            currentState = ProgramState.FoundNextMove;
        }else if(nextMove == "forward"){
            transform.position += transform.forward * speed * Time.deltaTime;
            if(Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                currentState = ProgramState.FoundNextMove;
            }
        }else if(nextMove == "backward"){
            transform.position += transform.forward * -speed * Time.deltaTime;
            if(Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                currentState = ProgramState.FoundNextMove;
            }
        }
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
                if(posRobot.x == i && posRobot.y == j){
                    riga += "R ";
                }else{
                    riga += matrice[i, j] + " ";
                }
            }
            Debug.Log(riga);
        }
    }
}