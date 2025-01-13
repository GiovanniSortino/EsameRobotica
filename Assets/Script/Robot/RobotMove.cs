using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RobotMovement : MonoBehaviour
{

    public Transform plane;
    private float cellSize = 1.0f;
    public MixDistance distanceSensorLeft;
    public MixDistance distanceSensorForward;
    public MixDistance distanceSensorRight;
    public MixDistance distanceSensorBackward;
    public float speed;
    public float rotationSpeed;
    public float obstacleDistance;
    public ThermalCamera thermalCamera;
    public GripperController gripper;
    private List<Cell> path;
    private List<Cell> explorationPath;
    private int currentIndex;
    private AStar aStar;
    private AStarExploration aStarExplore;
    public int[,] grid;
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
    private float distanceLeft = Mathf.Infinity;
    private float distanceForward = Mathf.Infinity;
    private float distanceRight = Mathf.Infinity;
    private float distanceBackward = Mathf.Infinity;
    private Vector2Int obstacleCell = new Vector2Int(0, 0);
    private Vector2Int obstacleCellLeft = new Vector2Int(0, 0);
    private Vector2Int obstacleCellForward = new Vector2Int(0, 0);
    private Vector2Int obstacleCellRight = new Vector2Int(0, 0);
    private Vector2Int obstacleCellBackward = new Vector2Int(0, 0);
    public Text actionText;
    private string navigationMode = "Navigation";
    public bool isUpdated = false;
    public bool insertObstacle = false;
    private ProgramState currentState = ProgramState.Initialization;
    private ExplorationState currentExplorationState = ExplorationState.StartCalculation;
    private bool car = false;
    private bool lowBattery = false;
    private bool detectPerson = false;
    // private bool first = true;
    // private float targetRotation;

    public enum ProgramState{
        Initialization,
        FindZone,
        WaitResponse,
        StartCalculation,
        Calculating,
        DistanceCalculation,
        Wait,
        DetectObstacle,
        FoundNextMove,
        CalculationgNextMove,
        Move,
        Explore,
        UpdatePersonState,
        WaitingUpdateState
    }

    public enum ExplorationState{
        StartCalculation,
        Calculating,
        DistanceCalculation,
        Wait,
        DetectObstacle,
        FoundNextMove,
        CalculationgNextMove,
        Move
    }

    private async void initialization(){
        currentState = ProgramState.Wait;
        Debug.Log("CREO BASE DI CONOSCENZA");
        offsetX = plane.localScale.x * 10f / 2f;
        offsetZ = plane.localScale.z * 10f / 2f;
        transform.position = GridToReal(new Cell(0, 7));
        GenerateGridFromPlane();
        aStar = new AStar();
        aStarExplore = new AStarExploration();
        nextPosition = transform.position;

        databaseManager = DatabaseManager.GetDatabaseManager();

        await databaseManager.ResetDatabaseAsync();

        LoadZone();
        await databaseManager.CreateRescueTeamAsync("P1", "Vigile");

        await databaseManager.CreateRobotNodeAsync("R1", "Agente1", "C1", 20, 80);
        await databaseManager.CreateChargerBaseAsync("B1", "C1");
        await databaseManager.LoadDataFromCSV();
        Debug.Log("FINE CREAZIONE");
        currentState = ProgramState.FindZone;
    }


    void OnApplicationQuit(){
        databaseManager.DestroyConnectionAsync();
    }

    void OnDrawGizmos(){
        if (grid == null){
            return;
        }

        for (int x = 0; x < grid.GetLength(0); x++){
            for (int z = 0; z < grid.GetLength(1); z++){
                Vector3 cellCenter = new Vector3(x * cellSize - offsetX, 0, z * cellSize - offsetZ);

                switch (grid[x, z]){
                    case 0: // Cella libera
                        Gizmos.color = Color.green;
                        break;
                    case 1: // Ostacolo
                        Gizmos.color = Color.red;
                        break;
                    case 2: // Già visitata
                        Gizmos.color = Color.blue;
                        break;
                    default: // Valore sconosciuto
                        Gizmos.color = Color.gray;
                        break;
                }

                Gizmos.DrawCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize)); // Disegna il cubo pieno
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize)); // Disegna il bordo
            }
        }
    }


    async void Update(){
        car = Microphone.detectCar && !MicrophoneClacson.detectClacson;
        lowBattery = PercentageBattery.percentage <= 0;
        detectPerson = ThermalCamera.detectPerson;

        if (GripperController.isGripperActive || car || lowBattery || detectPerson){
            return;
        }

        switch (currentState){
            case ProgramState.Initialization:{
                initialization();
                break;
            }
            case ProgramState.FindZone:{
                Debug.Log("CALCOLO ZONA");
                currentState = ProgramState.WaitResponse;
                zone = null;
                zone = await databaseManager.GetYoungestMissingPersonAsync();
                currentExplorationState = ExplorationState.StartCalculation;
                break;
            }

            case ProgramState.WaitResponse:{
                if (zone != null) currentState = ProgramState.StartCalculation;
                break;
            }

            case ProgramState.StartCalculation:{
                nextMove = null;
                path = null;
                CalculatePath();
                break;
            }

            case ProgramState.Calculating:{
                nextMove = null;
                if (path != null) currentState = ProgramState.DistanceCalculation;
                break;
            }

            case ProgramState.DistanceCalculation:{
                currentState = ProgramState.Wait;
                DistanceCalculation(path);
                break;
            }

            case ProgramState.DetectObstacle:{
                currentState = DetectObstacle() ? ProgramState.FindZone : ProgramState.FoundNextMove;
                break;
            }

            case ProgramState.FoundNextMove:{
                currentState = ProgramState.CalculationgNextMove;
                if (nextMove == "NON ADIACENTI"){
                    currentState = ProgramState.FindZone;
                }
                // first = true;
                NextMovement(path);
                Debug.Log(nextMove);
                break;
            }

            case ProgramState.CalculationgNextMove:{
                if (nextMove != null) currentState = ProgramState.Move;
                break;
            }

            case ProgramState.Move:{
                Move();
                break;
            }

            case ProgramState.Explore:{
                navigationMode = "Exploration";
                Explore();
                break;
            }

            case ProgramState.UpdatePersonState:{
                Debug.Log("Aggiorno stato persone");
                navigationMode = "Navigation";
                UpdatePersonState();
                break;
            }

            case ProgramState.WaitingUpdateState:{
                if(isUpdated == true) currentState = ProgramState.FindZone;
                break;
            }

            default:
                break;
        }
    }

    public void CalculatePath(){
        currentIndex = 1;
        if (zone != null){
            target = FindCell(zone);
            if (target != null){
                currentState = ProgramState.Calculating;
                path = aStar.TrovaPercorso(RealToGrid(transform.position), (Vector2Int)target, grid);
            }
        }else{
            return;
        }
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

    public string OrientationToDirection(List<Cell> selectedPath){
        Vector2Int robotCell = RealToGrid(transform.position);
        Debug.Log($"current index {currentIndex}, path.count {selectedPath.Count}");
        Vector2Int targetCell = new Vector2Int(selectedPath[currentIndex].x, selectedPath[currentIndex].y);

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

    public void DistanceCalculation(List<Cell> selectedPath){
        string activeSensor = OrientationToDirection(selectedPath);

        Vector2Int robotPosition = RealToGrid(transform.position);
        int robotOrientation = RobotOrientation();

        Debug.Log($"Active Sensor: {activeSensor}");

        distanceLeft = distanceSensorLeft.distance();
        distanceForward = distanceSensorForward.distance();
        distanceRight = distanceSensorRight.distance();
        distanceBackward = distanceSensorBackward.distance();

        obstacleCellLeft = robotOrientation switch{
            0 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Nord
            90 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Est
            180 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Sud
            270 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Ovest
            _ => robotPosition // Orientamento non valido
        };

        obstacleCellForward = robotOrientation switch{
            0 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Nord
            90 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Est
            180 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Sud
            270 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Ovest
            _ => robotPosition // Orientamento non valido
        };

        obstacleCellRight = robotOrientation switch{
            0 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Nord
            90 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Est
            180 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Sud
            270 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Ovest
            _ => robotPosition // Orientamento non valido
        };

        obstacleCellBackward = robotOrientation switch{
            0 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Nord
            90 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Est
            180 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Sud
            270 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Ovest
            _ => robotPosition // Orientamento non valido
        };

        if (activeSensor == "left"){
            distance = distanceLeft;
            obstacleCell = obstacleCellLeft;
        }else if (activeSensor == "forward"){
            distance = distanceForward;
            obstacleCell = obstacleCellForward;
        }else if (activeSensor == "right"){
            distance = distanceRight;
            obstacleCell = obstacleCellRight;
        }else if(activeSensor == "backward"){
            distance = distanceBackward;
            obstacleCell = obstacleCellBackward;
        }

        if (navigationMode == "Exploration"){
            currentExplorationState = ExplorationState.DetectObstacle;
        }else{
            currentState = ProgramState.DetectObstacle;
        }
    }

    public bool DetectObstacle(){
        if (navigationMode == "Exploration"){
            currentExplorationState = ExplorationState.Wait;
        }else{
            currentState = ProgramState.Wait;
        }

        if(insertObstacle){
            if (distanceLeft < obstacleDistance && obstacleCellLeft.x >= 0 && obstacleCellLeft.y >= 0){
                autoIncrementObstacle++;
                grid[obstacleCellLeft.x, obstacleCellLeft.y] = 1;
                _ = databaseManager.CreateObstacleNodeAsync($"O{autoIncrementObstacle}", $"C{autoIncrementCell}");
            }

            if (distanceForward < obstacleDistance && obstacleCellForward.x >= 0 && obstacleCellForward.y >= 0){
                autoIncrementObstacle++;
                grid[obstacleCellForward.x, obstacleCellForward.y] = 1;
                _ = databaseManager.CreateObstacleNodeAsync($"O{autoIncrementObstacle}", $"C{autoIncrementCell}");
            }

            if (distanceRight < obstacleDistance && obstacleCellRight.x >= 0 && obstacleCellRight.y >= 0){
                autoIncrementObstacle++;
                grid[obstacleCellRight.x, obstacleCellRight.y] = 1;
                _ = databaseManager.CreateObstacleNodeAsync($"O{autoIncrementObstacle}", $"C{autoIncrementCell}");
            }

            if (distanceBackward < obstacleDistance && obstacleCellBackward.x >= 0 && obstacleCellBackward.y >= 0){
                autoIncrementObstacle++;
                grid[obstacleCellBackward.x, obstacleCellBackward.y] = 1;
                _ = databaseManager.CreateObstacleNodeAsync($"O{autoIncrementObstacle}", $"C{autoIncrementCell}");
            }
        }

        if (distance < obstacleDistance){
            Debug.Log($"OSTACOLO IN {obstacleCell.x}, {obstacleCell.y}, distanza {distance}");
            distance = Mathf.Infinity;

            autoIncrementObstacle++;
            grid[obstacleCell.x, obstacleCell.y] = 1;

            _ = databaseManager.CreateObstacleNodeAsync($"O{autoIncrementObstacle}", $"C{autoIncrementCell}");
            return true;
        }
        return false;
    }

    public void NextMovement(List<Cell> selectedPath){
        nextCell = selectedPath[currentIndex];
        nextPosition = GridToReal(nextCell);
        Debug.Log($"NodoGriglia corrente: {currentIndex}/{selectedPath.Count}, Posizione corrente {transform.position} -> {RealToGrid(transform.position)}, Target Posizione: {nextPosition} -> {RealToGrid(nextPosition)}");
        autoIncrementCell++;

        _ = databaseManager.CreateCellNodeAsync($"C{autoIncrementCell}", nextCell.x, nextCell.y, FindZone(nextCell));
        nextMove = OrientationToDirection(selectedPath);
        if (nextMove == "NON ADIACENTI" && navigationMode == "Navigation"){
            currentState = ProgramState.FindZone;
        }else if (nextMove == "NON ADIACENTI" && navigationMode == "Exploration"){
            currentExplorationState = ExplorationState.StartCalculation;
        }
        actionText.text = "Azione: " + nextMove;

        Debug.Log($"next move {nextMove}");
    }

    public void Move(){
        float x_rotation = transform.eulerAngles.x;
        float y_rotation = transform.eulerAngles.y;
        float z_rotation = transform.eulerAngles.z;

        // if (nextMove == "left"){
        //     insertObstacle = false;
        //     if(first) targetRotation = Mathf.Repeat(y_rotation - 90, 360);
        //     first = false;
        //     transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(x_rotation, targetRotation, z_rotation), rotationSpeed * Time.deltaTime);
        //     if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetRotation)) <= 0.2f){
        //         transform.rotation = Quaternion.Euler(x_rotation, targetRotation, z_rotation);
        //         currentState = ProgramState.DistanceCalculation;
        //     }
        // }else if (nextMove == "right"){
        //     insertObstacle = false;
        //     if(first) targetRotation = Mathf.Repeat(y_rotation + 90, 360);
        //     first = false;
        //     transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(x_rotation, targetRotation, z_rotation), rotationSpeed * Time.deltaTime);
        //     if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetRotation)) <= 0.2f){
        //         transform.rotation = Quaternion.Euler(x_rotation, targetRotation, z_rotation);
        //         currentState = ProgramState.DistanceCalculation; // Passa allo stato successivo
        //     }
        // }
        
        if (nextMove == "left"){
            insertObstacle = false;
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation - 90, z_rotation);
            currentState = ProgramState.DistanceCalculation;
        }else if (nextMove == "right"){
            insertObstacle = false;
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + 90, z_rotation);
            currentState = ProgramState.DistanceCalculation;
        }else if (nextMove == "forward"){
            insertObstacle = true;
            transform.position += transform.forward * speed * Time.deltaTime;
            if (Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                if (currentIndex < path.Count - 1){
                    currentState = ProgramState.DistanceCalculation;
                    currentIndex++;
                }else{
                    currentIndex = 1;
                    currentState = ProgramState.Explore;
                }
            }
        }else if (nextMove == "backward"){
            insertObstacle = true;
            transform.position -= transform.forward * speed * Time.deltaTime;
            Debug.Log($"robot position: {RealToGrid(transform.position)}; {transform.position}; {RealToGrid(nextPosition)}; {nextPosition}; distanza {Vector3.Distance(transform.position, nextPosition)}");
            if (Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                if (currentIndex < path.Count - 1){
                    currentState = ProgramState.DistanceCalculation;
                    currentIndex++;
                }else{
                    currentIndex = 1;
                    currentState = ProgramState.Explore;
                }
            }
        }
    }

    public void Explore(){
        switch (currentExplorationState){
            case ExplorationState.StartCalculation:{
                    explorationPath = null;
                    nextMove = null;
                    CalculateExplorationPath();
                    break;
                }

            case ExplorationState.Calculating:{
                    nextMove = null;
                    if (explorationPath != null) currentExplorationState = ExplorationState.DistanceCalculation;
                    Vector2Int target = FindTarget(zone);
                    if (grid[target.x, target.y] == 1){
                        Debug.Log("CAMBIO MODALITA'");
                        currentState = ProgramState.UpdatePersonState;
                    }
                    break;
                }

            case ExplorationState.DistanceCalculation:{
                    currentExplorationState = ExplorationState.Wait;
                    DistanceCalculation(explorationPath);
                    break;
                }

            case ExplorationState.DetectObstacle:{
                    currentExplorationState = DetectObstacle() ? ExplorationState.StartCalculation : ExplorationState.FoundNextMove;
                    break;
                }

            case ExplorationState.FoundNextMove:{
                    currentExplorationState = ExplorationState.CalculationgNextMove;
                    if (nextMove == "NON ADIACENTI"){
                        currentExplorationState = ExplorationState.Calculating;
                    }
                    NextMovement(explorationPath);
                    Debug.Log(nextMove);
                    break;
                }

            case ExplorationState.CalculationgNextMove:{
                    if (nextMove != null) currentExplorationState = ExplorationState.Move;
                    break;
                }

            case ExplorationState.Move:{
                    ExplorationMove();
                    break;
                }

            default:
                break;
        }
    }

    public void CalculateExplorationPath(){
        currentIndex = 1;
        currentExplorationState = ExplorationState.Calculating;
        Vector2Int point = RealToGrid(transform.position);
        Vector2Int start = FindCell(zone);
        Vector2Int target = FindTarget(zone);
        grid = aStarExplore.Check(grid, point.x, point.y, start.x,start.y,target.x,target.y);
        explorationPath = aStarExplore.EsploraZona(RealToGrid(transform.position), FindTarget(zone), grid, zone);
    }

    public void ExplorationMove(){
        float x_rotation = transform.eulerAngles.x;
        float y_rotation = transform.eulerAngles.y;
        float z_rotation = transform.eulerAngles.z;

        if (nextMove == "left"){
            insertObstacle = false;
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation - 90, z_rotation);
            currentExplorationState = ExplorationState.DistanceCalculation;
        }else if (nextMove == "right"){
            insertObstacle = false;
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + 90, z_rotation);
            currentExplorationState = ExplorationState.DistanceCalculation;
        }else if (nextMove == "forward"){
            insertObstacle = true;
            transform.position += transform.forward * speed * Time.deltaTime;
            if (Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                if (currentIndex < explorationPath.Count - 1){
                    currentExplorationState = ExplorationState.DistanceCalculation;
                    currentIndex++;
                }else{
                    Debug.Log("HO CAMBIATO MODALITA IN NAVIGATION");
                    currentState = ProgramState.UpdatePersonState;
                }
            }
        }else if (nextMove == "backward"){
            insertObstacle = true;
            transform.position -= transform.forward * speed * Time.deltaTime;
            Debug.Log($"robot position: {RealToGrid(transform.position)}; {transform.position}; {RealToGrid(nextPosition)}; {nextPosition}; distanza {Vector3.Distance(transform.position, nextPosition)}");
            if (Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                if (currentIndex < explorationPath.Count - 1){
                    currentExplorationState = ExplorationState.DistanceCalculation;
                    currentIndex++;
                }else{
                    Debug.Log("HO CAMBIATO MODALITA IN NAVIGATION");
                    currentState = ProgramState.UpdatePersonState;
                }
            }
        }
        // Debug.Log($"Mi sposto in ({explorationPath[currentIndex].x}, {explorationPath[currentIndex].y}), {explorationPath[currentIndex].gCost}");
        Vector2Int robotCell = RealToGrid(transform.position);
        grid[robotCell.x, robotCell.y] = 2;
    }

    private async void UpdatePersonState(){
        currentState = ProgramState.WaitingUpdateState;
        isUpdated = await databaseManager.UpdateNotFoundPersonAsync(zone, "Non trovato");
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
        int x = Mathf.FloorToInt(position.x + offsetX);
        int y = Mathf.FloorToInt(position.z + offsetZ);
        x = x > 0 ? x : 0;
        y = y > 0 ? y : 0;
        return new Vector2Int(x, y);
    }

    private Vector3 GridToReal(Cell nextCell){
        return new Vector3((nextCell.x * cellSize) - offsetX, transform.position.y, (nextCell.y * cellSize) - offsetZ);
    }

    private string FindZone(Cell cell){
        int x = cell.x;
        int y = cell.y;

        int zoneX = x / 14;
        int zoneY = y / 14;

        int zoneNumber = zoneY * 5 + zoneX + 1;
        return "Z" + zoneNumber;
    }

    private Vector2Int FindCell(string zone){
        int zoneNumber = int.Parse(zone.Substring(1));

        int zoneIndex = zoneNumber - 1;
        int zoneX = zoneIndex % 5;
        int zoneY = zoneIndex / 5;

        return new Vector2Int(zoneX * 14, zoneY * 14);
    }

    private Vector2Int FindTarget(string zone){
        Vector2Int target = FindCell(zone);
        target.x += 13;
        target.y += 13;
        return target;
    }

    private async void LoadZone(){
        for (int row = 0; row < 5; row++) {
            for (int col = 0; col < 5; col++){
                int zoneNumber = row * 5 + col + 1;
                string zoneName = "Z" + zoneNumber;

                int x1 = col * 14;
                int y1 = row * 14;
                int x2 = x1 + 14;
                int y2 = y1 + 14;

                await databaseManager.CreateZoneNodeAsync(zoneName, x1, y1, x2, y2, false);
            }
        }
    }

}
