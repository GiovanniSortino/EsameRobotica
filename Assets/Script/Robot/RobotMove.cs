using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private float cellSize = 1;
    private List<Cell> path;
    private List<Cell> explorationPath;
    private List<Cell> recoveryPath;
    private List<Cell> explorationPathCopy;
    private List<Cell> chargingPath;
    private int currentIndex;
    private int currentIndexCopy;
    private AStar aStar;
    private DStarLite dStarExploration;
    private int[,] grid;
    private DatabaseManager databaseManager;
    private int autoIncrementCell = 0, autoIncrementObstacle = 0;
    private Vector3 nextPosition;
    private float offsetX;
    private float offsetZ;
    private Cell nextCell;
    private Move nextMove = Move.Undefined;
    private string zone;
    private float distance = Mathf.Infinity;
    private Vector2Int? obstacleCell = null;
    private OperatingMode operatingMode = OperatingMode.Navigation;
    private bool isUpdated = false;
    private bool insertObstacle = false;
    private NavigationState currentNavigationState = NavigationState.Initialization;
    private ExplorationState currentExplorationState = ExplorationState.StartCalculation;
    private RecoveryState currentRecoveryState = RecoveryState.StartCalculation;
    private ChargingState currentChargingState = ChargingState.StartCalculation;
    private bool car = false;
    private bool detectPerson = false;
    private int rotation = 0;
    private int FrameCounter = 0;
    private Vector2Int? chargingBasePosition = null;
    private bool isFirstBattery = true;
    private bool isFirstSpeack = true;
    private Vector3 estimatedPosition;
    private Vector3 movement;
    private float angle;

    private enum Direction{
        Up,
        Right,
        Down,
        Left,
        NotAdjacent
    }

    private enum Move{
        Forward, 
        Left,
        Backward,
        Right,
        NotAdjacent,
        Undefined
    }

    private enum OperatingMode{
        Navigation, 
        Exploration,
        Recovery,
        Charging,
        ChargingEnd
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
        Charging,
        End
    }

    public enum ExplorationState{
        StartCalculation,
        Calculating,
        DistanceCalculation,
        Wait,
        DetectObstacle,
        FoundNextMove,
        CalculationgNextMove,
        Move,
        Recovery
    }

    public enum RecoveryState{
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
            if(isFirstBattery) Debug.Log("BATTERIA SCARICA");
            isFirstBattery = false;
            return;
        }
        isFirstBattery = true;

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
            case NavigationState.FindZone:{ //punto di ingresso dalle altre FSM resetto tutte le FSM
                Debug.Log("CALCOLO ZONA");
                currentNavigationState = NavigationState.WaitResponse;
                zone = null; //serve per gestire la risposta della base di conoscenza in NavigationState.WaitResponse
                zone = await databaseManager.GetYoungestMissingPersonAsync(isFirstSpeack);
                isFirstSpeack = false;
                currentExplorationState = ExplorationState.StartCalculation;
                currentChargingState = ChargingState.StartCalculation;
                currentRecoveryState = RecoveryState.StartCalculation;
                break;
            }

            case NavigationState.WaitResponse:{
                if (zone != null){
                    if(zone.StartsWith("Z")){
                        currentNavigationState = NavigationState.StartCalculation;
                    }else{
                            currentChargingState = ChargingState.StartCalculation;
                            currentNavigationState = NavigationState.Charging;
                        if(zone.StartsWith("C")){
                            operatingMode = OperatingMode.Charging;
                            isFirstSpeack = true;
                            Debug.Log("CAMBIO MODALITA' DA NAVIGATION MODE A CHARGING MODE");
                        }else{
                            operatingMode = OperatingMode.ChargingEnd;
                            isFirstSpeack = true;
                            Debug.Log("CAMBIO MODALITA' DA NAVIGATION MODE A CHARGING END MODE");
                        }
                    }
                }
                break;
            }

            case NavigationState.StartCalculation:{
                currentNavigationState = NavigationState.Calculating;
                operatingMode = OperatingMode.Navigation;
                FrameCounter = 0; //serve per capire quando il robot si perde
                path = null; //serve per gestire la risposta dalla base di conoscenza
                currentIndex = 1;
                CalculateNavigationPath();
                break;
            }

            case NavigationState.Calculating:{
                if (path != null) currentNavigationState = NavigationState.DistanceCalculation;
                break;
            }

            case NavigationState.DistanceCalculation:{//punto di ingresso da NavigationState.Move
                currentNavigationState = NavigationState.Wait;
                if(rotation >= 4){ //serve per gestire il guasto delle rotazioni continue
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
                nextMove = Move.Undefined; //serve per gestire il calcolo della prossima mossa
                currentNavigationState = NavigationState.CalculationgNextMove;
                NextMovement(path);
                break;
            }

            case NavigationState.CalculationgNextMove:{
                if (nextMove != Move.Undefined){
                    if(nextMove == Move.NotAdjacent){ //il robot non sa dove andare quindi ricalcola il percorso
                        currentNavigationState = NavigationState.FindZone;
                    }else{
                        currentNavigationState = NavigationState.Move;
                    }
                }
                break;
            }

            case NavigationState.Move:{
                NavigationMove();
                break;
            }

            case NavigationState.Explore:{ //punto di ingresso da Navigation.Move
                ExplorationControll();
                break;
            }

            case NavigationState.UpdatePersonState:{
                Debug.Log("Aggiorno stato persone");
                UpdatePersonState();
                break;
            }

            case NavigationState.WaitingUpdateState:{
                if(isUpdated == true) currentNavigationState = NavigationState.FindZone;
                break;
            }

            case NavigationState.Charging:{
                ChargingControll();
                break;
            }

            case NavigationState.End:{
                //fine della FSM
                break;
            }

            default:
                break;
        }
    }

    private void ExplorationControll(){
        switch (currentExplorationState){
            case ExplorationState.StartCalculation:{
                if(explorationPath != null){ //serve per il calcolo del recovery path
                    explorationPathCopy = DeepClone(explorationPath);
                    currentIndexCopy = currentIndex;
                }
                FrameCounter = 0; //serve per capire quando il robot si perde
                //NON TOGLIERE IL COMMENTO SOTTO
                //explorationPath = null; serve per gestire la risposta della base di conoscenza ma per gestire il percorso troncato l'ho inserito in CalculateExplorationPath()
                CalculateExplorationPath();
                break;
            }

            case ExplorationState.Calculating:{
                if (explorationPath != null){
                    if (explorationPath.Count <= 1){ //il robot non trova un percorso per finire l'esplorazione, attiva la modalità di recovey che riporta il robot nell'ultima posizione visitata della zona
                        currentExplorationState = ExplorationState.Recovery;
                        operatingMode = OperatingMode.Recovery;
                        Debug.Log("CAMBIO MODALITA' DA EXPLORATION MODE A RECOVERY MODE");
                        explorationPath = null;
                        currentRecoveryState = RecoveryState.StartCalculation;
                    }else{
                        grid[GetRobotPosition().x, GetRobotPosition().y] = 2;
                        currentExplorationState = ExplorationState.DistanceCalculation;
                    }
                }
                break;
            }

            case ExplorationState.DistanceCalculation:{
                currentExplorationState = ExplorationState.Wait;
                if(rotation >= 4){ //serve per gestire il guasto delle rotazioni continue
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
                nextMove = Move.Undefined; //serve per gestire il calcolo della prossima mossa
                currentExplorationState = ExplorationState.CalculationgNextMove;
                NextMovement(explorationPath);
                break;
            }

            case ExplorationState.CalculationgNextMove:{
                if (nextMove != Move.Undefined){
                    if (nextMove == Move.NotAdjacent){
                        currentExplorationState = ExplorationState.StartCalculation;
                    }else{
                        currentExplorationState = ExplorationState.Move;
                    }
                }
                break;
            }

            case ExplorationState.Move:{
                ExplorationMove();
                break;
            }

            case ExplorationState.Recovery:{
                RecoveryControll();
                break;
            }

            default:
                break;
        }
    }

    private void RecoveryControll(){
        switch(currentRecoveryState){
            case RecoveryState.StartCalculation:{
                FrameCounter = 0; //serve per capire quando il robot si perde
                recoveryPath = null;  //serve per gestire la risposta dalla base di conoscenza
                currentRecoveryState = RecoveryState.Calculating;
                CalculateRecoveryPath();
                break;
            }

            case RecoveryState.Calculating:{
                if(recoveryPath != null) currentRecoveryState = RecoveryState.DistanceCalculation;
                break;
            }

            case RecoveryState.DistanceCalculation:{
                currentRecoveryState = RecoveryState.Wait;
                if(rotation >= 4){ //serve per gestire il guasto delle rotazioni continue
                    transform.rotation = Quaternion.Euler(transform.rotation.x, RobotOrientation(), transform.rotation.z);
                    Debug.Log($"Rotazione aggiornata a {RobotOrientation()} gradi.");
                    Debug.Log("GESTIONE GUASTO ROTAZIONI CONTINUE");
                }
                DistanceCalculation(recoveryPath);
                break;
            }

            case RecoveryState.DetectObstacle:{
                currentRecoveryState = DetectObstacle() ? RecoveryState.StartCalculation : RecoveryState.FoundNextMove;
                break;
            }

            case RecoveryState.FoundNextMove:{
                nextMove = Move.Undefined; //serve per gestire il calcolo della prossima mossa
                currentRecoveryState = RecoveryState.CalculationgNextMove;
                NextMovement(recoveryPath);
                break;
            }

            case RecoveryState.CalculationgNextMove:{
                if (nextMove != Move.Undefined){
                    if(nextMove == Move.NotAdjacent){
                        currentRecoveryState = RecoveryState.StartCalculation;
                    }else{
                        currentRecoveryState = RecoveryState.Move;
                    }
                }
                break;
            }

            case RecoveryState.Move:{
                RecoveryMove();
                break;
            }

            default:
                break;
        }
    }

    private void ChargingControll(){
        switch(currentChargingState){
            case ChargingState.StartCalculation:{
                FrameCounter = 0; //serve per capire quando il robot si perde
                chargingPath = null;  //serve per gestire la risposta dalla base di conoscenza
                CalculateChargingnPath();
                break;
            }

            case ChargingState.Calculating:{
                if (chargingPath != null) currentChargingState = ChargingState.DistanceCalculation;
                break;
            }

            case ChargingState.DistanceCalculation:{
                currentChargingState = ChargingState.Wait;
                if(rotation >= 4){ //serve per gestire il guasto delle rotazioni continue
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
                nextMove = Move.Undefined; //serve per gestire il calcolo della prossima mossa
                currentChargingState = ChargingState.CalculationgNextMove;
                NextMovement(chargingPath);
                break;
            }

            case ChargingState.CalculationgNextMove:{
                if (nextMove != Move.Undefined){
                    if(nextMove == Move.NotAdjacent){
                        currentChargingState = ChargingState.StartCalculation;
                    }else{
                        currentChargingState = ChargingState.Move;
                    }
                }
                    break;
            }

            case ChargingState.Move:{
                ChargingMove();
                break;
            }

            case ChargingState.Charging: {
                if(battery.percentage >= batterythreshold){
                    if(operatingMode == OperatingMode.Charging){
                        currentNavigationState = NavigationState.FindZone;
                        operatingMode = OperatingMode.Navigation;
                        Debug.Log("CARICAMENTO TERMINATO");
                        Debug.Log("CAMBIO MODALITA' DA CHARGING MODE A NAVIGATION MODE");
                        chargingPath = null;
                        currentChargingState = ChargingState.StartCalculation;
                    }else if(operatingMode == OperatingMode.ChargingEnd){
                        currentNavigationState = NavigationState.End;
                        operatingMode = OperatingMode.Navigation;
                        Debug.Log("NON CI SONO PIU DISPERSI");
                        Debug.Log("CAMBIO MODALITA' DA CHARGING MODE A NAVIGATION MODE");
                        chargingPath = null;
                        currentChargingState = ChargingState.StartCalculation;
                    }
                }
                break;
            }

            default:
                break;
        }
    }

    private async void initialization(){
        currentNavigationState = NavigationState.Wait;
        currentIndex = 1;
        operatingMode = OperatingMode.Navigation;
        Debug.Log("CREO BASE DI CONOSCENZA");
        offsetX = plane.localScale.x * 10f / 2f;
        offsetZ = plane.localScale.z * 10f / 2f;
        transform.position = GridToReal(new Cell(34, 34));
        GenerateGridFromPlane();
        aStar = new AStar();
        dStarExploration = new DStarLite();
        nextPosition = transform.position;

        particleFilter.InitializeLandmarks();
        particleFilter.InitializeParticles(transform.position);

        System.Random random = new System.Random();
        int batteryPercentage = random.Next(30, 80);

        databaseManager = DatabaseManager.GetDatabaseManager();

        await databaseManager.ResetAsync();

        await LoadZone();
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
        Vector2Int target = FindFreeCell(zone);
        Vector2Int robotPosition = GetRobotPosition();
        string currentZone = FindZone(new Cell(robotPosition));
        gridCorrection(robotPosition, FindCell(currentZone), FindTarget(currentZone));
        path = aStar.TrovaPercorso(GetRobotPosition(), target, grid);
    }

    private void CalculateExplorationPath(){
        List<Cell> pathTronc = new();
        Vector2Int start = FindCell(zone);
        Vector2Int target = FindTarget(zone);

        if (explorationPath != null){
            Debug.Log($"Lunghezza ExplorationPath {explorationPath.Count}");
            Debug.Log($"Lunghezza currentIndex {currentIndex}");
            pathTronc = explorationPath.GetRange(currentIndex + 1, Math.Max(explorationPath.Count - currentIndex - 2, 0));
            if(pathTronc.Count < 1){
                pathTronc.Add(new Cell(target));
            }
        }else{
            pathTronc.Add(new Cell(target));
        }

        Debug.Log($"Lunghezza pathTronc {pathTronc.Count - 1}");
        currentIndex = 1;
        explorationPath = null;
        currentExplorationState = ExplorationState.Calculating;
        Vector2Int robotPosition = GetRobotPosition();

        grid = dStarExploration.Check(grid, robotPosition.x, robotPosition.y, start.x, start.y, target.x, target.y);
        gridCorrection(robotPosition, start, target);
        pathTronc.Reverse();
        explorationPath = dStarExploration.UpdatePath(pathTronc, new Cell(GetRobotPosition()), new Cell(target), grid, zone);
    }

    private void CalculateRecoveryPath(){
        currentIndex = 1;
        Cell lastValidCell = explorationPathCopy[currentIndexCopy];
        Vector2Int robotPosition = GetRobotPosition();
        string currentZone = FindZone(new Cell(robotPosition));
        gridCorrection(robotPosition, FindCell(currentZone), FindTarget(currentZone));
        recoveryPath = aStar.TrovaPercorso(GetRobotPosition(), new Vector2Int(lastValidCell.x, lastValidCell.y), grid);
    }

    private void CalculateChargingnPath(){
        currentIndex = 1;
        currentChargingState = ChargingState.Calculating;
        Vector2Int robotPosition = GetRobotPosition();
        string currentZone = FindZone(new Cell(robotPosition));
        gridCorrection(robotPosition, FindCell(currentZone), FindTarget(currentZone));
        chargingPath = aStar.TrovaPercorso(GetRobotPosition(), (Vector2Int)chargingBasePosition, grid);
    }

    private void DistanceCalculation(List<Cell> selectedPath){
        Move activeSensor = OrientationToDirection(selectedPath);

        Vector2Int robotPosition = GetRobotPosition();
        int robotOrientation = RobotOrientation();

        Debug.Log($"Active Sensor: {activeSensor}");

        if (activeSensor == Move.Left){
            distance = distanceSensorLeft.distance();
            obstacleCell = robotOrientation switch{
                0 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Nord
                90 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Est
                180 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Sud
                270 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Ovest
                _ => null
            };
        }else if (activeSensor == Move.Forward){
            distance = distanceSensorForward.distance();
            obstacleCell = robotOrientation switch{
                0 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Nord
                90 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Est
                180 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Sud
                270 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Ovest
                _ => null
            };
        }else if (activeSensor == Move.Right){
            distance = distanceSensorRight.distance();
            obstacleCell = robotOrientation switch{
                0 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Nord
                90 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Est
                180 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Sud
                270 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Ovest
                _ => null
            };
        }else if(activeSensor == Move.Backward){
            distance = distanceSensorBackward.distance();
            obstacleCell = robotOrientation switch{
                0 => new Vector2Int(robotPosition.x, robotPosition.y - 1), // Nord
                90 => new Vector2Int(robotPosition.x - 1, robotPosition.y), // Est
                180 => new Vector2Int(robotPosition.x, robotPosition.y + 1), // Sud
                270 => new Vector2Int(robotPosition.x + 1, robotPosition.y), // Ovest
                _ => null
            };
        }else{ //celle non adiacenti, il robot non sa dove andare quindi calcola un nuovo percorso che parte dal punto in cui si trova
            if(operatingMode == OperatingMode.Navigation){
                currentNavigationState = NavigationState.FindZone;
            }else if (operatingMode == OperatingMode.Exploration){
                currentExplorationState = ExplorationState.StartCalculation;
            }else if(operatingMode == OperatingMode.Recovery){
                currentRecoveryState = RecoveryState.StartCalculation;
            }else if(operatingMode == OperatingMode.Charging || operatingMode == OperatingMode.ChargingEnd){
                currentChargingState = ChargingState.StartCalculation;
            }
            return;
        }

        if(operatingMode == OperatingMode.Navigation){
            currentNavigationState = NavigationState.DetectObstacle;
        }else if (operatingMode == OperatingMode.Exploration){
            currentExplorationState = ExplorationState.DetectObstacle;
        }else if(operatingMode == OperatingMode.Recovery){
            currentRecoveryState = RecoveryState.DetectObstacle;
        }else if(operatingMode == OperatingMode.Charging || operatingMode == OperatingMode.ChargingEnd){
            currentChargingState = ChargingState.DetectObstacle;
        }

    }

    private bool DetectObstacle(){
        if (operatingMode == OperatingMode.Exploration){
            currentExplorationState = ExplorationState.Wait;
        }else if(operatingMode == OperatingMode.Navigation){
            currentNavigationState = NavigationState.Wait;
        }else if(operatingMode == OperatingMode.Charging || operatingMode == OperatingMode.ChargingEnd){
            currentChargingState = ChargingState.Wait;
        }else if(operatingMode == OperatingMode.Recovery){
            currentRecoveryState = RecoveryState.Wait;
        }

        if(insertObstacle && distance < obstacleDistance && obstacleCell != null){
            Vector2Int obstacleCellNonNull = (Vector2Int)obstacleCell;
            if(obstacleCellNonNull.x >= 0 && obstacleCellNonNull.y >= 0){
                Debug.Log($"OSTACOLO IN {obstacleCellNonNull.x}, {obstacleCellNonNull.y}, distanza {distance}");
                distance = Mathf.Infinity;
                autoIncrementObstacle++;
                grid[obstacleCellNonNull.x, obstacleCellNonNull.y] = 1;
                autoIncrementCell++;
                //correzione base di conoscenza: se la cella esiste deve creare l'ostacolo nella cella già esistente mentre se non esiste deve creare sia la cella che l'ostacolo
                _ = databaseManager.CreateObstacleNodeAsync($"O{autoIncrementObstacle}", $"C{autoIncrementCell}", obstacleCellNonNull, FindZone(new Cell(obstacleCellNonNull)));
                return true;
            }          
        }
        return false;
    }

    private void NextMovement(List<Cell> selectedPath){
        nextCell = selectedPath[currentIndex];
        nextPosition = GridToReal(nextCell);
        Debug.Log($"NodoGriglia corrente: {currentIndex}/{selectedPath.Count}, Posizione corrente {transform.position} -> {GetRobotPosition()}, Target Posizione: {nextPosition} -> {RealToGrid(nextPosition)}");
        autoIncrementCell++;
        _ = databaseManager.CreateCellNodeAsync($"C{autoIncrementCell}", nextCell.x, nextCell.y, FindZone(nextCell));
        nextMove = OrientationToDirection(selectedPath);
        actionText.text = "Azione: " + nextMove;

        Debug.Log($"next move {nextMove}");
    }

    private void NavigationMove(){
        if(FrameCounter >= 200 * cellSize/speed){
            currentNavigationState = NavigationState.StartCalculation;
            return;
        }

        float x_rotation = transform.eulerAngles.x;
        float y_rotation = transform.eulerAngles.y;
        float z_rotation = transform.eulerAngles.z;
        
        if (nextMove == Move.Left){
            movement = Vector3.zero;
            angle = 270;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentNavigationState = NavigationState.DistanceCalculation;
        }else if (nextMove == Move.Right){
            movement = Vector3.zero;
            angle = 90;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentNavigationState = NavigationState.DistanceCalculation;
        }else if (nextMove == Move.Forward){
            movement = transform.forward * speed * Time.deltaTime;
            angle = 0;
            UpdateEstimatedPosition(movement, angle);
            FrameCounter++; //serve per controllare se il robot per più di tot frame non completa un movimento, ovvero se si è perso
            rotation = 0; //se mi sposto in avanti non sto facendo rotazioni quindi resetto il numero di rotazioni consecutive effettuate
            insertObstacle = true; //inserisco gli ostacoli solo quando il movimento fatto non è una rotazione, se facessi diversamente il robot durante la rotazione calcolerebbe distanze errate
            transform.position += movement;
            if(RealToGrid(transform.position).x == nextCell.x && RealToGrid(transform.position).y == nextCell.y){
                _ = databaseManager.UpdateRobotPositionAsync(GetRobotPosition().x, GetRobotPosition().y);
                FrameCounter = 0; //se termino un movimento significa che non mi sono perso quindi pongo la variabile a 0 per il prossimo controllo
                Vector2Int robotCell = GetRobotPosition();
                grid[robotCell.x, robotCell.y] = 2;
                if (currentIndex < path.Count - 1){
                    currentNavigationState = NavigationState.DistanceCalculation;
                    currentIndex++; //passo alla cella successiva
                }else{
                    currentIndex = 1;
                    operatingMode = OperatingMode.Exploration;
                    Debug.Log("CAMBIO MODALITA' DA NAVIGATION MODE A EXPLORATION MODE");
                    currentNavigationState = NavigationState.Explore;
                    operatingMode = OperatingMode.Exploration;
                }
            }
        }else if (nextMove == Move.Backward){
            movement = Vector3.zero;
            angle = 180;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentNavigationState = NavigationState.DistanceCalculation;
        }
    }

    private void ExplorationMove(){
        if(FrameCounter >= 200 * cellSize/speed){
            Debug.Log("OPS MI SONO PERSO");
            currentExplorationState = ExplorationState.StartCalculation;
            return;
        }

        float x_rotation = transform.eulerAngles.x;
        float y_rotation = transform.eulerAngles.y;
        float z_rotation = transform.eulerAngles.z;

        if (nextMove == Move.Left){
            movement = Vector3.zero;
            angle = 270;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentExplorationState = ExplorationState.DistanceCalculation;
        }else if (nextMove == Move.Right){
            movement = Vector3.zero;
            angle = 90;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentExplorationState = ExplorationState.DistanceCalculation;
        }else if (nextMove == Move.Forward){
            movement = transform.forward * speed * Time.deltaTime;
            angle = 0;
            UpdateEstimatedPosition(movement, angle);
            FrameCounter++; //serve per controllare se il robot per più di tot frame non completa un movimento, ovvero se si è perso
            rotation = 0; //se mi sposto in avanti non sto facendo rotazioni quindi resetto il numero di rotazioni consecutive effettuate
            insertObstacle = true; //inserisco gli ostacoli solo quando il movimento fatto non è una rotazione, se facessi diversamente il robot durante la rotazione calcolerebbe distanze errate
            transform.position += movement;
            if(RealToGrid(transform.position).x == nextCell.x && RealToGrid(transform.position).y == nextCell.y){
                _ = databaseManager.UpdateRobotPositionAsync(GetRobotPosition().x, GetRobotPosition().y);
                FrameCounter = 0; //se termino un movimento significa che non mi sono perso quindi pongo la variabile a 0 per il prossimo controllo
                Vector2Int robotCell = GetRobotPosition();
                grid[robotCell.x, robotCell.y] = 2;
                if (currentIndex < explorationPath.Count - 1 && !IsAllVisited(explorationPath)){
                    currentExplorationState = ExplorationState.DistanceCalculation;
                    currentIndex++; //passo alla cella successiva
                }else{
                    isFirstSpeack = true;
                    _ = databaseManager.UpdateZoneNodeAsync(zone);
                    Debug.Log("CAMBIO MODALITA' DA EXPLORATION MODE A NAVIGATION MODE");
                    currentNavigationState = NavigationState.UpdatePersonState;
                    operatingMode = OperatingMode.Navigation;
                    explorationPath = null;
                }
                
            }
        }else if (nextMove == Move.Backward){
            movement = Vector3.zero;
            angle = 180;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentExplorationState = ExplorationState.DistanceCalculation;
        }
    }

    private void RecoveryMove(){
        if(FrameCounter >= 200 * cellSize/speed){
            Debug.Log("OPS MI SONO PERSO");
            currentRecoveryState = RecoveryState.StartCalculation;
            return;
        }

        float x_rotation = transform.eulerAngles.x;
        float y_rotation = transform.eulerAngles.y;
        float z_rotation = transform.eulerAngles.z;

        if (nextMove == Move.Left){
            movement = Vector3.zero;
            angle = 270;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentRecoveryState = RecoveryState.DistanceCalculation;
        }else if (nextMove == Move.Right){
            movement = Vector3.zero;
            angle = 90;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentRecoveryState = RecoveryState.DistanceCalculation;
        }else if (nextMove == Move.Forward){
            movement = transform.forward * speed * Time.deltaTime;
            angle = 0;
            UpdateEstimatedPosition(movement, angle);
            FrameCounter++; //serve per controllare se il robot per più di tot frame non completa un movimento, ovvero se si è perso
            rotation = 0; //se mi sposto in avanti non sto facendo rotazioni quindi resetto il numero di rotazioni consecutive effettuate
            insertObstacle = true; //inserisco gli ostacoli solo quando il movimento fatto non è una rotazione, se facessi diversamente il robot durante la rotazione calcolerebbe distanze errate
            transform.position += movement;
            if(RealToGrid(transform.position).x == nextCell.x && RealToGrid(transform.position).y == nextCell.y){
                _ = databaseManager.UpdateRobotPositionAsync(GetRobotPosition().x, GetRobotPosition().y);
                FrameCounter = 0; //se termino un movimento significa che non mi sono perso quindi pongo la variabile a 0 per il prossimo controllo
                Vector2Int robotCell = GetRobotPosition();
                grid[robotCell.x, robotCell.y] = 2;
                if (currentIndex < recoveryPath.Count - 1){
                    currentRecoveryState = RecoveryState.DistanceCalculation;
                    currentIndex++; //passo alla cella successiva
                }else{
                    Debug.Log("CAMBIO MODALITA'DA RECOVERY MODE A EXPLORATION MODE");
                    currentExplorationState = ExplorationState.StartCalculation;
                    operatingMode = OperatingMode.Exploration;
                    recoveryPath = null;
                }
            }
        }else if (nextMove == Move.Backward){
            movement = Vector3.zero;
            angle = 180;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentRecoveryState = RecoveryState.DistanceCalculation;
        }
    }

    private void ChargingMove(){
        if(FrameCounter >= 200 * cellSize/speed){
            Debug.Log("OPS MI SONO PERSO");
            currentChargingState = ChargingState.StartCalculation;
            return;
        }

        float x_rotation = transform.eulerAngles.x;
        float y_rotation = transform.eulerAngles.y;
        float z_rotation = transform.eulerAngles.z;

        if (nextMove == Move.Left){
            movement = Vector3.zero;
            angle = 270;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentChargingState = ChargingState.DistanceCalculation;
        }else if (nextMove == Move.Right){
            movement = Vector3.zero;
            angle = 90;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentChargingState = ChargingState.DistanceCalculation;
        }else if (nextMove == Move.Forward){
            movement = transform.forward * speed * Time.deltaTime;
            angle = 0;
            UpdateEstimatedPosition(movement, angle);
            FrameCounter++; //serve per controllare se il robot per più di tot frame non completa un movimento, ovvero se si è perso
            rotation = 0; //se mi sposto in avanti non sto facendo rotazioni quindi resetto il numero di rotazioni consecutive effettuate
            insertObstacle = true; //inserisco gli ostacoli solo quando il movimento fatto non è una rotazione, se facessi diversamente il robot durante la rotazione calcolerebbe distanze errate
            transform.position += movement;
            if(RealToGrid(transform.position).x == nextCell.x && RealToGrid(transform.position).y == nextCell.y){
                _ = databaseManager.UpdateRobotPositionAsync(GetRobotPosition().x, GetRobotPosition().y);
                FrameCounter = 0; //se termino un movimento significa che non mi sono perso quindi pongo la variabile a 0 per il prossimo controllo
                Vector2Int robotCell = GetRobotPosition();
                grid[robotCell.x, robotCell.y] = 2;
                if (currentIndex < chargingPath.Count - 1){
                    currentChargingState = ChargingState.DistanceCalculation;
                    currentIndex++; //passo alla cella successiva
                }else{
                    currentChargingState = ChargingState.Charging;
                    Debug.Log("CARICAMENTO IN CORSO");
                }
            }
        }else if (nextMove == Move.Backward){
            movement = Vector3.zero;
            angle = 180;
            UpdateEstimatedPosition(movement, angle);
            rotation++; //serve per gestire il guasto delle rotazioni
            insertObstacle = false; //disattivo l'inserimento degli ostacoli, il calcolo delle distanze in questo caso non è corretto
            transform.rotation = Quaternion.Euler(x_rotation, y_rotation + angle, z_rotation);
            currentChargingState = ChargingState.DistanceCalculation;
        }
    }

    private Vector2Int GetRobotPosition(){
        return RealToGrid(transform.position);
    }

    private Direction RelativeTargetPosition(Vector2Int robotCell, Vector2Int targetCell){
        int deltaX = targetCell.x - robotCell.x;
        int deltaY = targetCell.y - robotCell.y;

        if (deltaX == 0 && deltaY >= 0) return Direction.Up;
        if (deltaX == 0 && deltaY < 0) return Direction.Down;
        if (deltaX >= 0 && deltaY == 0) return Direction.Right;
        if (deltaX < 0 && deltaY == 0) return Direction.Left;

        return Direction.NotAdjacent;
    }

    private Move OrientationToDirection(List<Cell> selectedPath){
        Vector2Int robotCell = GetRobotPosition();
        Debug.Log($"current index {currentIndex}, path.count {selectedPath.Count}");
        Vector2Int targetCell = new Vector2Int(selectedPath[currentIndex].x, selectedPath[currentIndex].y);

        Direction relativeTargetPosition = RelativeTargetPosition(robotCell, targetCell);
        int robotOrientation = RobotOrientation();

        Debug.Log($"Relative target Position {relativeTargetPosition}");

        switch (relativeTargetPosition){
            case Direction.Down:{
                if (robotOrientation == 180) return Move.Forward;
                else if (robotOrientation == 0) return Move.Backward;
                else if (robotOrientation == 90) return Move.Right;
                else if (robotOrientation == 270) return Move.Left;
                break;
            }

            case Direction.Up:{
                if (robotOrientation == 0) return Move.Forward;
                else if (robotOrientation == 180) return Move.Backward;
                else if (robotOrientation == 90) return Move.Left;
                else if (robotOrientation == 270) return Move.Right;
                break;
            }

            case Direction.Left:{
                if (robotOrientation == 90) return Move.Backward;
                else if (robotOrientation == 270) return Move.Forward;
                else if (robotOrientation == 0) return Move.Left;
                else if (robotOrientation == 180) return Move.Right;
                break;
            }

            case Direction.Right:{
                if (robotOrientation == 270) return Move.Backward;
                else if (robotOrientation == 90) return Move.Forward;
                else if (robotOrientation == 0) return Move.Right;
                else if (robotOrientation == 180) return Move.Left;
                break;
            }

            default:
                break;
        }
        return Move.NotAdjacent;
    }

    private bool IsAllVisited(List<Cell> path){
        foreach(Cell cell in path){
            if(grid[cell.x, cell.y] == 0){
                return false;
            }
        }
        return true;
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

        int gridWidth = (int)width;
        int gridHeight = (int)height;

        grid = new int[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++){
            for (int y = 0; y < gridHeight; y++){
                grid[x, y] = 0;
            }
        }
    }

    private Vector2Int RealToGrid(Vector3 position){
        float marginX = 0;
        float marginY = 0;
        if(RobotOrientation() == 0){
            marginY = -0.5f;
        }else if(RobotOrientation() == 90){
            marginX = -0.5f;
        }else if(RobotOrientation() == 180){
            marginY = 0.5f;
        }else if(RobotOrientation() == 270){
            marginX = 0.5f;
        }
        
        int x = (int)Mathf.Round(position.x + marginX + offsetX);
        int y = (int)Mathf.Round(position.z + marginY + offsetZ);
        x = x > 0 ? x : 0;
        y = y > 0 ? y : 0;
        return new Vector2Int(x, y);
    }

    private Vector3 GridToReal(Cell nextCell){
        return new Vector3(nextCell.x - offsetX, transform.position.y, nextCell.y - offsetZ);
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

    private Vector2Int FindFreeCell(string zone){
        Vector2Int start = FindCell(zone);
        Vector2Int end = FindTarget(zone);

        for (int x = start.x; x <= end.x; x++){
            for (int y = start.y; y <= end.y; y++){
                if (grid[x, y] != 1){
                    return new Vector2Int(x, y);
                }
            }
        }

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

    private async Task<bool> LoadZone(){
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
        return true;
    }

    private void gridCorrection(Vector2Int cell, Vector2Int startZone, Vector2Int target){
        if (IsRobotTrulyBlocked(cell.x, cell.y, startZone.x, startZone.y, target.x, target.y)){
            Debug.Log($"SONO bloccato e sto andando a resettare la zona {cell.x} {cell.y} {FindZone(new Cell(cell.x, cell.y))}");
            ResetZone(cell);
        }
    }

    private bool IsRobotTrulyBlocked(int x_start, int y_start, int min_x, int min_y, int max_x, int max_y){
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        bool[,] visited = new bool[rows, cols];
        Queue<Vector2Int> coda = new Queue<Vector2Int>();

        visited[x_start, y_start] = true;
        coda.Enqueue(new Vector2Int(x_start, y_start));

        while (coda.Count > 0){
            Vector2Int current = coda.Dequeue();

            foreach (Vector2Int neighbor in AddNeighbor(current.x, current.y, min_x, min_y, max_x, max_y, visited)){
                if (grid[neighbor.x, neighbor.y] == 0){
                    Debug.Log($"La posizione libera è {neighbor.x} {neighbor.y}");
                    return false;
                }else if(grid[neighbor.x, neighbor.y] == 2){
                    coda.Enqueue(neighbor);
                }
            }
        }
        Debug.Log("SONO BLOCCATO");
        return true;
    }

    private IEnumerable<Vector2Int> AddNeighbor(int x, int y, int min_x, int min_y, int max_x, int max_y, bool[,] visited){
        Vector2Int[] directions = {new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1)};

        foreach (var dir in directions){
            int nx = x + dir.x;
            int ny = y + dir.y;

            if (nx >= 0 && nx <= 69 && ny >= 0 && ny <= 69 && !visited[nx, ny]){
                visited[nx, ny] = true;
                yield return new Vector2Int(nx, ny);
            }
        }
    }
    
    private void ResetZone(Vector2Int cell){
        string zone = FindZone(new Cell(cell));
        Vector2Int targetZone = FindTarget(zone);
        Vector2Int startZone = FindCell(zone);
        int min_x = startZone.x;
        int min_y = startZone.y;
        int max_x = targetZone.x;
        int max_y = targetZone.y;

        if (min_x < 0 || min_y < 0 || max_x >= grid.GetLength(0) || max_y >= grid.GetLength(1)){
            Debug.Log("SONO FUORI DALLA GRIGLIA E NON POSSO RESETARE");
            return;
        }
        _ = databaseManager.RemoveObstacleAsync(zone);
        for (int x = min_x; x <= max_x; x++){
            for (int y = min_y; y <= max_y; y++){
                grid[x, y] = 0;
            }
        }

        Debug.Log($"Zona {zone} reinizializzata (da ({min_x}, {min_y}) a ({max_x}, {max_y}))");
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

    private List<Cell> DeepClone(List<Cell> list){
        List<Cell> copy = new();
        foreach (Cell cell in list){
            copy.Add(new Cell(cell));
        }
        return copy;
    }

    void UpdateEstimatedPosition(Vector3 movement, float deltaTheta){
        particleFilter.Predict(movement.x, movement.z, deltaTheta); 
        float[] observedDistances = GetObservedDistances();
        particleFilter.UpdateWeights(observedDistances);
        particleFilter.Resample();
        estimatedPosition = particleFilter.EstimatePosition();
    }

    float[] GetObservedDistances(){
        float[] distances = new float[particleFilter.landmarks.Count];
        for (int i = 0; i < particleFilter.landmarks.Count; i++){
            Vector2 landmarkPosition = particleFilter.landmarks[i];
            float distance = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), landmarkPosition);
            distances[i] = distance;
        }
        return distances;
    }
}
