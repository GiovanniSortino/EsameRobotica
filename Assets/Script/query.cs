using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Neo4j.Driver;
using System.Threading.Tasks;
using System;
using System.Data.Common;
using Unity.VisualScripting;

public class Prova : MonoBehaviour{
    private IDriver _driver;

    async void Start(){
    Debug.Log("Avvio del tentativo di connessione a Neo4j...");
    await ConnectToDatabaseAsync();

    // Testa la creazione di un nodo Ostacolo
    await CreateObstacleNodeAsync("O1", 10, "C1");

    // Testa la creazione di un nodo Cella
    await CreateCellNodeAsync("C1", 5, 5, "Z1");

    // Testa la creazione di un nodo Disperso
    await CreateMissingPersonNodeAsync("D1", "Mario Rossi", 35, "C1", "Non trovato", "Z1");

    // Testa l'aggiornamento di un nodo Disperso
    await UpdataeMissingPersonNodeAsync("D1", "Mario Rossi", 35, "C2", "Trovato", "Z1");

    // Testa la creazione di un nodo Robot
    await CreateRobotNodeAsync("R1", "Robot Alpha", 80, "Operativo", "C1");

    // Testa la creazione di una Base di ricarica
    await CreateChargerBaseAsync("B1", "C1", 20, 100);

    // Testa la creazione di un nodo Personale Competente
    await CreateRescueTeamAsync("PC1", "Medico");

    // Testa il metodo per trovare una cella dato un x, y
    string cellId = await FindCellIdAsync(5, 5);
    if (!string.IsNullOrEmpty(cellId))
    {
        Debug.Log($"ID della cella trovata: {cellId}");
    }

    // Testa l'aggiornamento della posizione del Robot
    //await UpdateRobotPositionAsync(6, 6);

    Debug.Log("Test completati!");
    }


    private async void OnDestroy(){
        Debug.Log("Disconnessione dal database...");
        if (_driver != null){
            await _driver.DisposeAsync();
            Debug.Log("Driver disconnesso correttamente.");
        }
    }

    /// <summary>
    /// Metodo per la creazione della connessione con il db.
    /// </summary>
    private async Task ConnectToDatabaseAsync(){
        var uri = "bolt://localhost:7687";
        var user = "neo4j";
        var password = "password";

        try{
            // Creazione del driver
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
            Debug.Log("Connessione al database Neo4j riuscita!");

            // Chiamata al metodo CountNodesAsync
            int nodeCount = await CountNodesAsync();
            Debug.Log($"Numero totale di nodi nel database: {nodeCount}");

            // Puoi aggiungere ulteriori logica qui se necessario
        }
        catch (System.Exception ex){
            Debug.LogError($"Errore durante la connessione o l'esecuzione della query: {ex.Message}");
        }
    }

    /// <summary>
    /// Metodo pubblico per eseguire query Cypher personalizzate
    /// </summary>
    /// <param name="query">La query Cypher da eseguire</param>
    /// <returns>Il risultato della query come lista di dizionari</returns>
    /// 
    public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, bool isWrite = true){
        var results = new List<Dictionary<string, object>>();

        if (_driver == null)
        {
            Debug.LogError("Driver non inizializzato. Assicurati di essere connesso al database.");
            return results;
        }

        try
        {
            await using var session = _driver.AsyncSession(o => o.WithDatabase("neo4j"));

            if (isWrite)
            {
                // Per query di scrittura
                await session.ExecuteWriteAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query);

                    while (await cursor.FetchAsync())
                    {
                        var record = cursor.Current;
                        var row = new Dictionary<string, object>();

                        foreach (var key in record.Keys)
                        {
                            row[key] = record[key];
                        }

                        results.Add(row);
                    }
                });
            }
            else
            {
                // Per query di lettura
                await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query);

                    while (await cursor.FetchAsync())
                    {
                        var record = cursor.Current;
                        var row = new Dictionary<string, object>();

                        foreach (var key in record.Keys)
                        {
                            row[key] = record[key];
                        }

                        results.Add(row);
                    }
                });
            }

            Debug.Log("Query eseguita con successo.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Errore durante l'esecuzione della query: {ex.Message}");
        }

        return results;
    }



    /// <summary>
    /// Metodo per contare tutti i nodi nel database.
    /// </summary>
    public async Task<int> CountNodesAsync(){
        string query = "MATCH (n) RETURN count(n) AS count";
        var result = await ExecuteQueryAsync(query, false);

        if (result.Count > 0 && result[0].ContainsKey("count")){
            // Convertire il valore in int in modo sicuro
            return System.Convert.ToInt32(result[0]["count"]);
        }
        return 0;
    }

    public async Task CreateObstacleNodeAsync(String id, int peso, String cella){
        string query = $"CREATE (:Ostacolo {{id: '{id}', peso: {peso}, cella: '{cella}' }})";
        await ExecuteQueryAsync(query);
        Debug.Log($"Nodo Ostacolo creato: id={id}, peso={peso}, cella={cella}");
        query = $"MATCH (r:Robot {{id: 'R1'}}), (o:Ostacolo {{id:'{id}'}}) CREATE (r)-[:SEGNALA]->(o);";
        await ExecuteQueryAsync(query);
        Debug.Log($"Relazione Segnala creata: Robot_id=R1 segnala Ostacoll_id:{id}");
        query = $"MATCH (o:Ostacolo {{id: '{id}'}}), (c:Cella {{id: '{cella}'}}) CREATE (o)-[:BLOCCA]->(c);";
        await ExecuteQueryAsync(query);
        Debug.Log($"Relazione Blocca creata: Ostacolo_id:{id} blocca Cella_id:{cella}");
    }

    public async Task CreateCellNodeAsync(String id, int x, int y, String zona){
        string query = $"CREATE (:Cella {{id: '{id}', posizione_x: {x}, posizione_y: {y}, zona: '{zona}'}})";
        await ExecuteQueryAsync(query);
        Debug.Log($"Nodo Cella creato: id={id}, posizione_x={x}, posizione_y={y}, zona={zona}");
        query = $"MATCH (c:Cella {{id: '{id}'}}), (z:Zona {{id:'{zona}'}}) CREATE (z)-[:E_FORMATA]->(c);";
        await ExecuteQueryAsync(query);
        Debug.Log($"Relazione é_formata creata: Zona_id={zona} è formata da Cella_id:{id}");
    }

    /// <summary>
    /// Metodo inserire il nodo Disperso non ancora trovato
    /// /// </summary>
    public async Task CreateMissingPersonNodeAsync(String id, String nome, int eta, String cella, String stato, String zona){
        string query = $"CREATE (:Disperso {{id: '{id}', nome: '{nome}', eta: {eta}, cella: '{cella}', stato_rilevamento: '{stato}', zona: '{zona}'}})";
        await ExecuteQueryAsync(query);
        Debug.Log($"Nodo Disperso creato: id={id}, nome={nome}, eta={eta}, cella={cella}, stato_rilevamento: '{stato}', zona: '{zona}'");        
        query = $"MATCH (z:Zona {{id: '{zona}'}}), (d:Disperso {{id:'{id}'}}) CREATE (d)-[:APPARTIENE]->(z);";
        await ExecuteQueryAsync(query);
        Debug.Log($"Relazione Appartiene creata: Disperso_id={id} appartiene a Zona_id:{zona}");
        query = $"MATCH (d:Disperso {{id: '{id}'}}), (r:Robot {{id:'R1'}}) CREATE (r)-[:CERCA]->(d);";
        await ExecuteQueryAsync(query);
        Debug.Log($"Relazione Cerca creata: Robot_id=R1 Cerca Disperso_id={id}");
    }

    /// <summary>
    /// Metodo aggiornare il nodo Disperso quando viene ritrovato
    /// /// </summary>
    public async Task UpdataeMissingPersonNodeAsync(String id, String nome, int eta, String cella, String stato, String zona){
        string query = $"MATCH (d:Disperso {{id: '{id}'}}) SET d.cella = '{cella}', d.stato_rilevamento = 'Trovato' RETURN d";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Disperso creato: id={id}, nome={nome}, eta={eta}, cella={cella}, stato_rilevamento: '{stato}', zona: '{zona}'");        
        query = $"MATCH (r:Robot {{id:'R1'}})-[rel:CERCA]->(d:Disperso {{id: '{id}'}}) DELETE rel";
        await ExecuteQueryAsync(query);
        Debug.Log($"Relazione Cerca rimossa: Robot_id=R1 Cerca Disperso_id:{id}");
        query = $"MATCH (d:Disperso {{id: '{id}'}}), (r:Robot {{id:'R1'}}) CREATE (r)-[:HA_TROVATO]->(d);";
        await ExecuteQueryAsync(query);
        Debug.Log($"Relazione Ha_Trovato creata: Robot_id=R1 Ha_Trovato Disperso_id={id}");
    }

    public async Task CreateRobotNodeAsync(String id, String nome, int livello_batteria, String stato, String cella){
        string query = $"CREATE (r:Robot {{id: '{id}', nome: '{nome}', livello_batteria: {livello_batteria}, stato: '{stato}', cella: '{cella}'}})";
        await ExecuteQueryAsync(query);
        Debug.Log($"Nodo Robot creato: id={id}, nome={nome}, livello batteria={livello_batteria}, stato={stato}, cella: '{cella}'");        
    }

    public async Task CreateChargerBaseAsync(String id, String cella, int livello_minimo_batteria, int livello_massimo_batteria){
        string query = $"CREATE (b:Base {{id: '{id}', cella: '{cella}', livello_minimo_batteria: '{livello_minimo_batteria}', livello_massimo_batteria: '{livello_massimo_batteria}'}})";
        await ExecuteQueryAsync(query);
        Debug.Log($"Nodo Base creato: id={id}, cella={cella}, livello livello_minimo_batteria={livello_minimo_batteria}, livello_massimo_batteria={livello_massimo_batteria}");        
        query = $"MATCH (b:Base {{id: '{id}'}}), (r:Robot {{id: 'R1'}}) CREATE (b)-[:RICARICA]->(r)";
        await ExecuteQueryAsync(query);
        Debug.Log($"Relazione Ricarica creata: Base_id={id} Ricarica Robot_id=R1");
    }

    public async Task CreateRescueTeamAsync(String id, String ruolo){
        string query = $"CREATE (pc:Personale_Competente {{id: '{id}', ruolo: '{ruolo}'}})";
        await ExecuteQueryAsync(query);
        Debug.Log($"Nodo Personale Competente creato: id={id}, ruolo={ruolo}");        
        query = $"MATCH (pc:Personale_Competente {{id:'{id}'}}), (r:Robot {{id: 'R1'}}) CREATE (r)-[:COMUNICA]->(pc)";
        await ExecuteQueryAsync(query);
        Debug.Log($"Relazione Comunica creata: Robot_id=R1 Comunica Personale_Competente_id={id}");
    }

    public async Task<String> FindCellIdAsync(int pos_x, int pos_y){
        string query = $"MATCH (c:Cella {{posizione_x:'{pos_x}', posizione_y:'{pos_y}'}}) RETURN c.id as id_cell";
        var result = await ExecuteQueryAsync(query);

        if (result.Count > 0 && result[0].ContainsKey("id_cell")){
            // Convertire il valore in int in modo sicuro
            String id_cella = System.Convert.ToString(result[0]["id_cell"]);
            Debug.Log($"Proprietà id_cella trovato: cella_x: {pos_x}, cella_y: {pos_y} in cella_id: {id_cella}");        
            return id_cella;
        }
        Debug.Log($"Proprietà id_cella non trovato: cella_x: {pos_x}, cella_y: {pos_y} in cella_id: none");
        return null;
    }

    public async Task UpdateRobotPositionAsync(int pos_x, int pos_y){
        string id_cell = await FindCellIdAsync(pos_x, pos_y);
        if (id_cell != null){
            string query = $"MATCH (r:Robot {{id: 'R1'}}) SET r.cella = '{id_cell}'";
            await ExecuteQueryAsync(query);
            Debug.Log($"Nodo Robot aggiornato: robot_id: R1, cella_x: {pos_x}, cella_y: {pos_y} in cella_id: {id_cell}");       
        }
    }


    
    // -----------------------------------------------------------------------------------------------------------------------

    public async Task<int> GetBatteryLevelAsync(){
        string query = "MATCH (r:Robot {id: 'R1'}) RETURN r.livello_batteria AS livello_batteria";
        var result = await ExecuteQueryAsync(query);

        if (result.Count > 0 && result[0].ContainsKey("livello_batteria"))
        {
            int livelloBatteria = System.Convert.ToInt32(result[0]["livello_batteria"]);
            Debug.Log($"Attributo livello_batteria recuperato: robot_id: R1, livello_batteria: {livelloBatteria}");
            return livelloBatteria;
        }

        Debug.LogError("Impossibile recuperare il livello_batteria per robot_id: R1.");
        return 0;
    }

    public async Task<(int x, int y)> GetRobotPositionAsync(){
        // Prima query: Trova la cella associata al robot
        string queryCella = "MATCH (r:Robot {id: 'R1'}) RETURN r.cella AS id_cella";

        var resultCella = await ExecuteQueryAsync(queryCella);

        if (resultCella.Count == 0 || !resultCella[0].ContainsKey("id_cella")){
            Debug.LogError("Impossibile trovare la cella associata al robot.");
            return (-1, -1);
        }

        // Estrai l'ID della cella
        string id_cella = resultCella[0]["id_cella"].ToString();
        string queryPosition = $"MATCH (c:Cella {{id: '{id_cella}'}}) RETURN c.x AS x, c.y AS y";

        var resultPosition = await ExecuteQueryAsync(queryPosition);

        if (resultPosition.Count > 0 && resultPosition[0].ContainsKey("x") && resultPosition[0].ContainsKey("y"))
        {
            int x = Convert.ToInt32(resultPosition[0]["x"]);
            int y = Convert.ToInt32(resultPosition[0]["y"]);
            Debug.Log($"Posizione del robot: x={x}, y={y}");
            return (x, y);
        }

        Debug.LogError("Impossibile trovare la posizione della cella.");
        return (-1, -1);
    }

    // public async Task FindNextPosition(){
    //     string batteryLevel = GetBatteryLevelAsync();
    //     var (robotX, robotY) = await GetRobotPositionAsync();
    //     string query =      @"MATCH (c:Cella), (r:Robot {id: 'R1'})
    //                         WITH c, r, distance(point({x: c.x, y: c.y}), point({x: $robotX, y: $robotY})) AS dist
    //                         WHERE ($batteryLevel < 20 AND dist < 10) OR ($batteryLevel >= 20 AND $batteryLevel < 50 AND dist >= 10)
    //                         RETURN c.x AS x, c.y AS y
    //                         ORDER BY dist ASC
    //                         LIMIT 1";
        
    // }


    // public async Task FindCellAsync(){

    // }











    // public async Task CreateRobotNodeAsync(String id, String nome, int livello_batteria, String stato, String cella){
    //     string query = $"CREATE (r:Robot {{id: '{id}', nome: '{nome}', livello_batteria: {livello_batteria}, stato: '{}'}}) SET d.cella = {{cella}}, d.stato_rilevamento = 'Trovato' RETURN n";
    //     await ExecuteQueryAsync(query);
    //     Debug.Log($"Nodo Disperso creato: id={id}, nome={nome}, eta={eta}, cella={cella}, stato_rilevamento: '{stato}', zona: '{zona}'");        
    //     query = $"MATCH (r:Roboto {{id:'R1'}})-[rel:CERCA]->(d:Disperso {{id: '{id}'}}) DELETE r";
    //     await ExecuteQueryAsync(query);
    //     Debug.Log($"Relazione Cerca rimossa: Robot_id=R1 Cerca Disperso_id:{id}");
    //     query = $"MATCH (d:Disperso {{id: '{id}'}}), (r:Robot {{id:'R1'}}) CREATE (r)-[:HA_TROVATO]->(d);";
    //     await ExecuteQueryAsync(query);
    //     Debug.Log($"Relazione Ha_Trovato creata: Robot_id=R1 Ha_Trovato Disperso_id={id}");
    // }


}
