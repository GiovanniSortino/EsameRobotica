using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RobotMovement : MonoBehaviour{

    public Transform plane;
    public MixDistance distanceSensorLeft;
    public MixDistance distanceSensorForward;
    public MixDistance distanceSensorRight;
    public MixDistance distanceSensorBackward;
    public float speed;
    public float obstacleDistance;
    public ThermalCamera thermalCamera;
    public GripperController gripper;
    public Battery battery;
    public int batterythreshold;
    public Text actionText;
    public ParticleFilter particleFilter;

    private float cellSize = 1.0f;
    private List<Cell> path;
    private List<Cell> explorationPath;
    private List<Cell> chargingPath;
    private int currentIndex;
    private AStar aStar;
    private DStarLite aStarExplore;
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
    private float distanceLeft = Mathf.Infinity;
    private float distanceForward = Mathf.Infinity;
    private float distanceRight = Mathf.Infinity;
    private float distanceBackward = Mathf.Infinity;
    private Vector2Int obstacleCell = new Vector2Int(0, 0);
    private Vector2Int obstacleCellLeft = new Vector2Int(0, 0);
    private Vector2Int obstacleCellForward = new Vector2Int(0, 0);
    private Vector2Int obstacleCellRight = new Vector2Int(0, 0);
    private Vector2Int obstacleCellBackward = new Vector2Int(0, 0);
    private OperatingMode operatingMode = OperatingMode.Navigation;
    private bool isUpdated = false;
    private bool insertObstacle = false;
    private NavigationState currentNavigationState = NavigationState.Initialization;
    private ExplorationState currentExplorationState = ExplorationState.StartCalculation;
    private ChargingState currentChargingState = ChargingState.StartCalculation;
    private bool car = false;
    private bool detectPerson = false;
    private int rotation = 0;
    private int FrameCounter = 0;
    private Vector2Int? chargingBasePosition = null;
    private bool isFirst = true;

    private enum OperatingMode{
        Navigation, 
        Exploration, 
        Charging
    }

    public enum NavigationState{
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
        WaitingUpdateState,
        Charging
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

    public enum ChargingState{
        StartCalculation,
        Calculating,
        DistanceCalculation,
        Wait,
        DetectObstacle,
        FoundNextMove, 
        CalculationgNextMove,
        Move,
        Charging
    }

    void OnDrawGizmos(){
        DrawGizmos();
    }

    void Update(){
        NavigationControll();
    }

    void OnApplicationQuit(){
        databaseManager.DestroyConnectionAsync();
    }

    private async void NavigationControll(){
        car = Microphone.detectCar && !MicrophoneClacson.detectClacson;
        detectPerson = ThermalCamera.detectPerson;

        if(battery.percentage <= 0){
            if(isFirst) Debug.Log("BATTERIA SCARICA");
            isFirst = false;
            return;
        }
        isFirst = true;

        if (GripperController.isGripperActive || car || detectPerson){
            return;
        }

        battery.isCharging = chargingBasePosition != null && Mathf.Abs(GetRobotPosition().x - ((Vector2Int)chargingBasePosition).x) <= 1 && 
                                Mathf.Abs(GetRobotPosition().y - ((Vector2Int)chargingBasePosition).y) <= 1;

        switch (currentNavigationState){
            case NavigationState.Initialization:{
                initialization();
                break;
            }
            case NavigationState.FindZone:{
                Debug.Log("CALCOLO ZONA");
                currentNavigationState = NavigationState.WaitResponse;
                zone = null;
                zone = await databaseManager.GetYoungestMissingPersonAsync();
                currentExplorationState = ExplorationState.StartCalculation;
                currentChargingState = ChargingState.StartCalculation;
                break;
            }

            case NavigationState.WaitResponse:{
                if (zone != null){
                    if(zone.StartsWith("Z")){
                        currentNavigationState = NavigationState.StartCalculation;
                    }else if(zone.StartsWith("C")){
                        currentNavigationState = NavigationState.Charging;
                    }
                }
                break;
            }

            case NavigationState.StartCalculation:{
                operatingMode = OperatingMode.Navigation;
                FrameCounter = 0;
                nextMove = null;
                path = null;
                CalculateNavigationPath();
                break;
            }

            case NavigationState.Calculating:{
                nextMove = null;
                if (path != null) currentNavigationState = NavigationState.DistanceCalculation;
                break;
            }

            case NavigationState.DistanceCalculation:{
                currentNavigationState = NavigationState.Wait;
                if(rotation >= 4){
                    transform.rotation = Quaternion.Euler(transform.rotation.x, RobotOrientation(), transform.rotation.z);
                    Debug.Log($"Rotazione aggiornata a {RobotOrientation()} gradi.");
                    Debug.Log("GESTIONE GUASTO ROTAZIONI CONTINUE");
                }
                DistanceCalculation(path);
                break;
            }

            case NavigationState.DetectObstacle:{
                currentNavigationState = DetectObstacle() ? NavigationState.FindZone : NavigationState.FoundNextMove;
                break;
            }

            case NavigationState.FoundNextMove:{
                currentNavigationState = NavigationState.CalculationgNextMove;
                if (nextMove == "NON ADIACENTI"){
                    currentNavigationState = NavigationState.FindZone;
                }
                NextMovement(path);
                // Debug.Log(nextMove);
                break;
            }

            case NavigationState.CalculationgNextMove:{
                if (nextMove != null) currentNavigationState = NavigationState.Move;
                break;
            }

            case NavigationState.Move:{
                NavigationMove();
                break;
            }

            case NavigationState.Explore:{
                operatingMode = OperatingMode.Exploration;
                ExplorationControll();
                break;
            }

            case NavigationState.UpdatePersonState:{
                Debug.Log("Aggiorno stato persone");
                operatingMode = OperatingMode.Navigation;
                UpdatePersonState();
                break;
            }

            case NavigationState.WaitingUpdateState:{
                if(isUpdated == true) currentNavigationState = NavigationState.FindZone;
                break;
            }

            case NavigationState.Charging:{
                operatingMode = OperatingMode.Charging;
                ChargingControll();
                break;
            }

            default:
                break;
        }
    }

    private void ExplorationControll(){
        switch (currentExplorationState){
            case ExplorationState.StartCalculation:{
                FrameCounter = 0;
                explorationPath = null;
                nextMove = null;
                CalculateExplorationPath();
                break;
            }

            case ExplorationState.Calculating:{
                nextMove = null;
                if (explorationPath != null){
                    rotation = 0;
                    currentExplorationState = ExplorationState.DistanceCalculation;
                    
                    string pathString = "";
                    foreach (Cell cell in explorationPath){
                        pathString += cell.ToString() + "\n"; // Aggiunge ogni cella su una nuova riga
                    }
                    Debug.Log(pathString);
                }
                Vector2Int target = FindTarget(zone);
                if (grid[target.x, target.y] == 1 || (explorationPath != null && explorationPath.Count <= 1)){
                    Debug.Log("CAMBIO MODALITA'");
                    currentNavigationState = NavigationState.UpdatePersonState;
                }
                break;
            }

            case ExplorationState.DistanceCalculation:{
                currentExplorationState = ExplorationState.Wait;
                if(rotation >= 4){
                    transform.rotation = Quaternion.Euler(transform.rotation.x, RobotOrientation(), transform.rotation.z);
                    Debug.Log($"Rotazione aggiornata a {RobotOrientation()} gradi.");
                    Debug.Log("GESTIONE GUASTO ROTAZIONI CONTINUE");
                }
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
                    break;
                }
                NextMovement(explorationPath);
                // Debug.Log(nextMove);
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

    private void ChargingControll(){
        switch(currentChargingState){
            case ChargingState.StartCalculation:{
                FrameCounter = 0;
                chargingPath = null;
                nextMove = null;
                CalculateChargingnPath();
                break;
            }

            case ChargingState.Calculating:{
                if (chargingPath != null) currentChargingState = ChargingState.DistanceCalculation;
                break;
            }

            case ChargingState.DistanceCalculation:{
                currentChargingState = ChargingState.Wait;
                if(rotation >= 4){
                    transform.rotation = Quaternion.Euler(transform.rotation.x, RobotOrientation(), transform.rotation.z);
                    Debug.Log($"Rotazione aggiornata a {RobotOrientation()} gradi.");
                    Debug.Log("GESTIONE GUASTO ROTAZIONI CONTINUE");
                }
                DistanceCalculation(chargingPath);
                break;
            }

            case ChargingState.DetectObstacle:{
                currentChargingState = DetectObstacle() ? ChargingState.StartCalculation : ChargingState.FoundNextMove;
                break;
            }

            case ChargingState.FoundNextMove:{
                currentChargingState = ChargingState.CalculationgNextMove;
                if (nextMove == "NON ADIACENTI"){
                    currentChargingState = ChargingState.Calculating;
                    break;
                }
                NextMovement(chargingPath);
                // Debug.Log(nextMove);
                break;
            }

            case ChargingState.CalculationgNextMove:{
                if (nextMove != null) currentChargingState = ChargingState.Move;
                    break;
            }

            case ChargingState.Move:{
                ChargingMove();
                break;
            }

            case ChargingState.Charging: {
                if(battery.percentage >= batterythreshold){
                    currentNavigationState = NavigationState.FindZone;
                    Debug.Log("CARICAMENTO TERMINATO");
                }
                break;
            }

            default:
                break;
        }
    }

    private async void initialization(){
        currentNavigationState = NavigationState.Wait;

        // particleFilter.InitializeLandmarks();
        // particleFilter.InitializeParticles();

        Debug.Log("CREO BASE DI CONOSCENZA");
        offsetX = plane.localScale.x * 10f / 2f;
        offsetZ = plane.localScale.z * 10f / 2f;
        transform.position = GridToReal(new Cell(32, 25));
        GenerateGridFromPlane();
        aStar = new AStar();
        aStarExplore = new DStarLite();
        nextPosition = transform.position;

        System.Random random = new System.Random();
        int batteryPercentage = random.Next(30, 80);

        databaseManager = DatabaseManager.GetDatabaseManager();

        await databaseManager.ResetDatabaseAsync();

        LoadZone();
        await databaseManager.CreateRescueTeamAsync("P1", "Vigile");

        await databaseManager.CreateRobotNodeAsync("R1", "Agente1", "C1", 20, 80, batteryPercentage);
        await databaseManager.CreateCellNodeAsync("C0", 34, 34, "Z13");
        await databaseManager.CreateChargerBaseAsync("B1", "C0");
        await databaseManager.LoadDataFromCSV();
        chargingBasePosition = await databaseManager.GetBasePosition();

        battery.SetBatteryPercentage(batteryPercentage);
        battery.isActive = true;
        battery.isCharging = false;

        Debug.Log("FINE CREAZIONE");
        currentNavigationState = NavigationState.FindZone;
    }

    private void CalculateNavigationPath(){
        currentIndex = 1;
        target = FindCell(zone);
        if (target != null){
            currentNavigationState = NavigationState.Calculating;
            path = aStar.TrovaPercorso(GetRobotPosition(), (Vector2Int)target, grid);
        }
    }

    private void CalculateExplorationPath(){
        List<Cell> pathTronc = new();
        Vector2Int start = FindCell(zone);
        Vector2Int target = FindTarget(zone);

        if (explorationPath != null){
            Debug.Log($"Lunghezza ExplorationPath {explorationPath.Count}");
            Debug.Log($"Lunghezza currentIndex {currentIndex}");
            pathTronc = explorationPath.GetRange(currentIndex + 1, explorationPath.Count - currentIndex - 2);
            Debug.Log($"Lunghezza pathTronc {explorationPath.Count - 1}");

        }else{
            pathTronc.Add(new Cell(target));
        }

        currentIndex = 1;
        explorationPath = null;
        currentExplorationState = ExplorationState.Calculating;
        Vector2Int robotPosition = GetRobotPosition();

        grid = aStarExplore.Check(grid, robotPosition.x, robotPosition.y, start.x, start.y, target.x, target.y);
        pathTronc.Reverse();
        explorationPath = aStarExplore.UpdatePath(pathTronc, new Cell(RealToGrid(transform.position)), new Cell(target), grid, zone);
    }

    private void CalculateChargingnPath(){
        currentIndex = 1;
        currentChargingState = ChargingState.Calculating;
        chargingPath = aStar.TrovaPercorso(GetRobotPosition(), (Vector2Int)chargingBasePosition, grid);
    }

    private void DistanceCalculation(List<Cell> selectedPath){
        string activeSensor = OrientationToDirection(selectedPath);

        Vector2Int robotPosition = GetRobotPosition();
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

        if (operatingMode == OperatingMode.Exploration){
            currentExplorationState = ExplorationState.DetectObstacle;
        }else if(operatingMode == OperatingMode.Navigation){
            currentNavigationState = NavigationState.DetectObstacle;
        }else if(operatingMode == OperatingMode.Charging){
            currentChargingState = ChargingState.DetectObstacle;
        }
    }

    private bool DetectObstacle(){
        if (operatingMode == OperatingMode.Exploration){
            currentExplorationState = ExplorationState.Wait;
        }else if(operatingMode == OperatingMode.Navigation){
            currentNavigationState = NavigationState.Wait;
        }else if(operatingMode == OperatingMode.Charging){
            currentChargingState = ChargingState.Wait;
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

    private void NextMovement(List<Cell> selectedPath){
        FrameCounter = 0;
        nextCell = selectedPath[currentIndex];
        nextPosition = GridToReal(nextCell);
        Debug.Log($"NodoGriglia corrente: {currentIndex}/{selectedPath.Count}, Posizione corrente {transform.position} -> {GetRobotPosition()}, Target Posizione: {nextPosition} -> {RealToGrid(nextPosition)}");
        autoIncrementCell++;

        _ = databaseManager.CreateCellNodeAsync($"C{autoIncrementCell}", nextCell.x, nextCell.y, FindZone(nextCell));
        nextMove = OrientationToDirection(selectedPath);
        if (nextMove == "NON ADIACENTI" && operatingMode == OperatingMode.Navigation){
            currentNavigationState = NavigationState.FindZone;
        }else if (nextMove == "NON ADIACENTI" && operatingMode == OperatingMode.Exploration){
            currentExplorationState = ExplorationState.StartCalculation;
        }else if (nextMove == "NON ADIACENTI" && operatingMode == OperatingMode.Charging){
            currentChargingState = ChargingState.StartCalculation;
        }
        actionText.text = "Azione: " + nextMove;

        Debug.Log($"next move {nextMove}");
    }

    private void NavigationMove(){
        if(FrameCounter >= 60){
            transform.position = GridToReal(new Cell(GetRobotPosition()));
            Debug.Log("OOOOOOOOOPS MI SONO PERSO");
            currentNavigationState = NavigationState.StartCalculation;
            return;
        }
        float x_rotation = transform.eulerAngles.x;
        float y_rotation = transform.eulerAngles.y;
        float z_rotation = transform.eulerAngles.z;
        
        if (nextMove == "left"){
            rotation++;
            insertObstacle = false;
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation - 90, z_rotation);
            currentNavigationState = NavigationState.DistanceCalculation;
        }else if (nextMove == "right"){
            rotation++;
            insertObstacle = false;
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + 90, z_rotation);
            currentNavigationState = NavigationState.DistanceCalculation;
        }else if (nextMove == "forward"){
            FrameCounter++;
            rotation = 0;
            insertObstacle = true;

            transform.position += transform.forward * speed * Time.deltaTime;

            // Vector3 movement = transform.forward * speed * Time.deltaTime;
            // particleFilter.Predict(movement.x, movement.z, 0);

            // transform.position += movement;

            // float[] observedDistances = GetObservedDistances();
            // particleFilter.UpdateWeights(observedDistances);

            // particleFilter.NormalizeWeights();

            // // Step 3: Ricampionamento delle particelle
            // particleFilter.ResampleParticles();

            // // Step 4: Stima della posizione
            // Vector2 estimatedPosition = particleFilter.EstimatePosition();
            // Debug.Log($"Posizione stimata dal filtro: {estimatedPosition} -> {RealToGrid(estimatedPosition)}, transform position: {transform.position} -> {RealToGrid(transform.position)}");


            if (Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                if (currentIndex < path.Count - 1){
                    currentNavigationState = NavigationState.DistanceCalculation;
                    currentIndex++;
                }else{
                    currentIndex = 1;
                    currentNavigationState = NavigationState.Explore;
                }
            }
        }else if (nextMove == "backward"){
            FrameCounter++;
            rotation = 0;
            insertObstacle = true;

            transform.position -= transform.forward * speed * Time.deltaTime;
            // Vector3 movement = transform.forward * speed * Time.deltaTime;
            // particleFilter.Predict(movement.x, movement.z, 0);

            // transform.position -= movement;

            // float[] observedDistances = GetObservedDistances();
            // particleFilter.UpdateWeights(observedDistances);

            // particleFilter.NormalizeWeights();

            // // Step 3: Ricampionamento delle particelle
            // particleFilter.ResampleParticles();

            // // Step 4: Stima della posizione
            // Vector2 estimatedPosition = particleFilter.EstimatePosition();
            // Debug.Log($"Posizione stimata dal filtro: {estimatedPosition} -> {RealToGrid(estimatedPosition)}, transform position: {transform.position} -> {RealToGrid(transform.position)}");

            Debug.Log($"robot position: {GetRobotPosition()}; {transform.position}; {RealToGrid(nextPosition)}; {nextPosition}; distanza {Vector3.Distance(transform.position, nextPosition)}");
            if (Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                if (currentIndex < path.Count - 1){
                    currentNavigationState = NavigationState.DistanceCalculation;
                    currentIndex++;
                }else{
                    currentIndex = 1;
                    currentNavigationState = NavigationState.Explore;
                }
            }
        }
        Vector2Int robotCell = GetRobotPosition();
        grid[robotCell.x, robotCell.y] = 2;
    }

    private void ExplorationMove(){
        if(FrameCounter >= 60){
            transform.position = GridToReal(new Cell(GetRobotPosition()));
            Debug.Log("OOOOOOOOOPS MI SONO PERSO");
            currentExplorationState = ExplorationState.StartCalculation;
            return;
        }

        float x_rotation = transform.eulerAngles.x;
        float y_rotation = transform.eulerAngles.y;
        float z_rotation = transform.eulerAngles.z;

        if (nextMove == "left"){
            rotation++;
            insertObstacle = false;
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation - 90, z_rotation);
            currentExplorationState = ExplorationState.DistanceCalculation;
        }else if (nextMove == "right"){
            rotation++;
            insertObstacle = false;
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + 90, z_rotation);
            currentExplorationState = ExplorationState.DistanceCalculation;
        }else if (nextMove == "forward"){
            FrameCounter++;
            rotation = 0;
            insertObstacle = true;
            transform.position += transform.forward * speed * Time.deltaTime;
            if (Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                if (currentIndex < explorationPath.Count - 1){
                    currentExplorationState = ExplorationState.DistanceCalculation;
                    currentIndex++;
                }else{
                    Debug.Log("HO CAMBIATO MODALITA IN NAVIGATION");
                    currentNavigationState = NavigationState.UpdatePersonState;
                }
            }
        }else if (nextMove == "backward"){
            FrameCounter++;
            rotation = 0;
            insertObstacle = true;
            transform.position -= transform.forward * speed * Time.deltaTime;
            Debug.Log($"robot position: {GetRobotPosition()}; {transform.position}; {RealToGrid(nextPosition)}; {nextPosition}; distanza {Vector3.Distance(transform.position, nextPosition)}");
            if (Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                if (currentIndex < explorationPath.Count - 1){
                    currentExplorationState = ExplorationState.DistanceCalculation;
                    currentIndex++;
                }else{
                    Debug.Log("HO CAMBIATO MODALITA IN NAVIGATION");
                    currentNavigationState = NavigationState.UpdatePersonState;
                }
            }
        }
        // Debug.Log($"Mi sposto in ({explorationPath[currentIndex].x}, {explorationPath[currentIndex].y}), {explorationPath[currentIndex].gCost}");
        Vector2Int robotCell = GetRobotPosition();
        grid[robotCell.x, robotCell.y] = 2;
    }

    private void ChargingMove(){
        if(FrameCounter >= 60){
            transform.position = GridToReal(new Cell(GetRobotPosition()));
            Debug.Log("OOOOOOOOOPS MI SONO PERSO");
            currentChargingState = ChargingState.StartCalculation;
            return;
        }

        float x_rotation = transform.eulerAngles.x;
        float y_rotation = transform.eulerAngles.y;
        float z_rotation = transform.eulerAngles.z;

        if (nextMove == "left"){
            rotation++;
            insertObstacle = false;
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation - 90, z_rotation);
            currentChargingState = ChargingState.DistanceCalculation;
        }else if (nextMove == "right"){
            rotation++;
            insertObstacle = false;
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + 90, z_rotation);
            currentChargingState = ChargingState.DistanceCalculation;
        }else if (nextMove == "forward"){
            FrameCounter++;
            rotation = 0;
            insertObstacle = true;
            transform.position += transform.forward * speed * Time.deltaTime;
            if (Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                if (currentIndex < chargingPath.Count - 1){
                    currentChargingState = ChargingState.DistanceCalculation;
                    currentIndex++;
                }else{
                    currentChargingState = ChargingState.Charging;
                    Debug.Log("CARICAMENTO IN CORSO");
                }
            }
        }else if (nextMove == "backward"){
            FrameCounter++;
            rotation = 0;
            insertObstacle = true;
            transform.position -= transform.forward * speed * Time.deltaTime;
            Debug.Log($"robot position: {GetRobotPosition()}; {transform.position}; {RealToGrid(nextPosition)}; {nextPosition}; distanza {Vector3.Distance(transform.position, nextPosition)}");
            if (Vector3.Distance(transform.position, nextPosition) < 0.2f){
                transform.position = nextPosition;
                if (currentIndex < chargingPath.Count - 1){
                    currentChargingState = ChargingState.DistanceCalculation;
                    currentIndex++;
                }else{
                    currentChargingState = ChargingState.Charging;
                    Debug.Log("CARICAMENTO IN CORSO");
                }
            }
        }

        Vector2Int robotCell = GetRobotPosition();
        grid[robotCell.x, robotCell.y] = 2;
    }

    private Vector2Int GetRobotPosition(){
        return RealToGrid(transform.position);;
    }
    private string RelativeOrientation(Vector2Int primaCella, Vector2Int secondaCella){
        int deltaX = secondaCella.x - primaCella.x;
        int deltaY = secondaCella.y - primaCella.y;

        if (deltaX == 0 && deltaY >= 0) return "down";
        if (deltaX == 0 && deltaY < 0) return "up";
        if (deltaX >= 0 && deltaY == 0) return "right";
        if (deltaX < 0 && deltaY == 0) return "left";

        return "Non adiacente";
    }

    private string OrientationToDirection(List<Cell> selectedPath){
        Vector2Int robotCell = GetRobotPosition();
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

    private int RobotOrientation(){
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

    private async void UpdatePersonState(){
        currentNavigationState = NavigationState.WaitingUpdateState;
        isUpdated = await databaseManager.UpdateNotFoundPersonAsync(zone, "Non trovato");
    }

    private void GenerateGridFromPlane(){
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

    private void DrawGizmos(){
        if (grid == null){
            return;
        }

        for (int x = 0; x < grid.GetLength(0); x++){
            for (int z = 0; z < grid.GetLength(1); z++){
                Vector3 cellCenter = new Vector3(x * cellSize - offsetX, 0, z * cellSize - offsetZ);

                switch (grid[x, z]){
                    case 0:
                        Gizmos.color = Color.green;
                        break;
                    case 1:
                        Gizmos.color = Color.red;
                        break;
                    case 2:
                        Gizmos.color = Color.blue;
                        break;
                    default:
                        Gizmos.color = Color.gray;
                        break;
                }

                Gizmos.DrawCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
            }
        }

        Gizmos.color = Color.yellow;
        int zoneSize = 14;
        float lineThickness = 0.1f;

        for (int x = 0; x < grid.GetLength(0); x += zoneSize){
            for (int z = 0; z < grid.GetLength(1); z += zoneSize){
                Vector3 bottomLeft = new Vector3(x * cellSize - offsetX - cellSize / 2, 0, z * cellSize - offsetZ - cellSize / 2);
                Vector3 bottomRight = new Vector3((x + zoneSize) * cellSize - offsetX - cellSize / 2, 0, z * cellSize - offsetZ - cellSize / 2);
                Vector3 topLeft = new Vector3(x * cellSize - offsetX - cellSize / 2, 0, (z + zoneSize) * cellSize - offsetZ - cellSize / 2);
                Vector3 topRight = new Vector3((x + zoneSize) * cellSize - offsetX - cellSize / 2, 0, (z + zoneSize) * cellSize - offsetZ - cellSize / 2);

                Gizmos.DrawCube((bottomLeft + bottomRight) / 2, new Vector3(Vector3.Distance(bottomLeft, bottomRight), lineThickness, lineThickness)); // Bordi inferiori
                Gizmos.DrawCube((topLeft + topRight) / 2, new Vector3(Vector3.Distance(topLeft, topRight), lineThickness, lineThickness)); // Bordi superiori
                Gizmos.DrawCube((bottomLeft + topLeft) / 2, new Vector3(lineThickness, lineThickness, Vector3.Distance(bottomLeft, topLeft))); // Bordi sinistra
                Gizmos.DrawCube((bottomRight + topRight) / 2, new Vector3(lineThickness, lineThickness, Vector3.Distance(bottomRight, topRight))); // Bordi destra
            }
        }
    }

    float[] GetObservedDistances(){
        float[] distances = new float[particleFilter.landmarks.Count];

        for (int i = 0; i < particleFilter.landmarks.Count; i++){
            Vector2 landmarkPosition = particleFilter.landmarks[i];
            float distance = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), landmarkPosition);
            distances[i] = distance; // Distanza osservata
        }

        return distances;
    }

}
