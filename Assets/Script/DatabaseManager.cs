using System.Collections.Generic;
using UnityEngine;
using Neo4j.Driver;
using System.Threading.Tasks;
using System;

public class DatabaseManager{
    private static IDriver _driver;
    private static DatabaseManager databaseManager;

    private DatabaseManager(){}

    public static DatabaseManager GetDatabaseManager(){
        if (databaseManager == null){
            databaseManager = new();
            CreateConnetion();
        }
        return databaseManager;
    }

    private static void CreateConnetion(){
        var uri = "bolt://localhost:7687";
        var user = "neo4j";
        var password = "password";

        try{
            //Debug.Log("Connessione...");
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
            //Debug.Log("Connessione al database Neo4j riuscita!");
        }catch (System.Exception ex){
            Debug.LogError($"Errore durante la connessione o l'esecuzione della query: {ex.Message}");
        }
    }

    public async void DestroyConnectionAsync(){
        //Debug.Log("Disconnessione dal database...");
        if (_driver != null){
            await _driver.DisposeAsync();
            //Debug.Log("Driver disconnesso correttamente.");
        }
    }

    public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, bool isWrite = true){
        var results = new List<Dictionary<string, object>>();

        if (_driver == null){
            //Debug.LogError("Driver non inizializzato. Assicurati di essere connesso al database.");
            return results;
        }

        try{  
            await using var session = _driver.AsyncSession(o => o.WithDatabase("neo4j"));        
            if (isWrite){
                await session.ExecuteWriteAsync(async tx =>{
                    var cursor = await tx.RunAsync(query);

                    while (await cursor.FetchAsync()){
                        var record = cursor.Current;
                        var row = new Dictionary<string, object>();

                        foreach (var key in record.Keys){
                            row[key] = record[key];
                        }

                        results.Add(row);
                    }
                });
            }else{
                await session.ExecuteReadAsync(async tx =>{
                    var cursor = await tx.RunAsync(query);

                    while (await cursor.FetchAsync()){
                        var record = cursor.Current;
                        var row = new Dictionary<string, object>();

                        foreach (var key in record.Keys){
                            row[key] = record[key];
                        }

                        results.Add(row);
                    }
                });
            }

            //Debug.Log("Query eseguita con successo.");
        }catch (System.Exception ex){
            Debug.LogError($"Errore durante l'esecuzione della query: {ex.Message}");
        }

        return results;
    }

    public async Task<int> CountNodesAsync(){
        string query = "MATCH (n) RETURN count(n) AS count";
        var result = await ExecuteQueryAsync(query, false);

        if (result.Count > 0 && result[0].ContainsKey("count")){
            return System.Convert.ToInt32(result[0]["count"]);
        }
        return 0;
    }

    public async Task CreateObstacleNodeAsync(String id, String cella){
        string query = $"CREATE (:Ostacolo {{id: '{id}', cella: '{cella}' }})";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Ostacolo creato: id={id}, cella={cella}");
        query = $"MATCH (r:Robot {{id: 'R1'}}), (o:Ostacolo {{id:'{id}'}}) CREATE (r)-[:SEGNALA]->(o);";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Segnala creata: Robot_id=R1 segnala Ostacoll_id:{id}");
        query = $"MATCH (o:Ostacolo {{id: '{id}'}}), (c:Cella {{id: '{cella}'}}) CREATE (o)-[:BLOCCA]->(c);";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Blocca creata: Ostacolo_id:{id} blocca Cella_id:{cella}");
    }

    public async Task CreateCellNodeAsync(String id, int x, int y, String zona){
        string query = $"CREATE (:Cella {{id: '{id}', posizione_x: '{x}', posizione_y: '{y}', zona: '{zona}'}})";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Cella creato: id={id}, posizione_x={x}, posizione_y={y}, zona={zona}");
        query = $"MATCH (c:Cella {{id: '{id}'}}), (z:Zona {{id:'{zona}'}}) CREATE (z)-[:E_FORMATA]->(c);";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione é_formata creata: Zona_id={zona} è formata da Cella_id:{id}");
    }

    public async Task CreateMissingPersonNodeAsync(String id, String nome, int eta, String cella, String stato, String zona){
        string query = $"CREATE (:Disperso {{id: '{id}', nome: '{nome}', eta: '{eta}', cella: '{cella}', stato_rilevamento: '{stato}', zona: '{zona}'}})";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Disperso creato: id={id}, nome={nome}, eta={eta}, cella={cella}, stato_rilevamento: '{stato}', zona: '{zona}'");        
        query = $"MATCH (z:Zona {{id: '{zona}'}}), (d:Disperso {{id:'{id}'}}) CREATE (d)-[:APPARTIENE]->(z);";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Appartiene creata: Disperso_id={id} appartiene a Zona_id:{zona}");
        query = $"MATCH (d:Disperso {{id: '{id}'}}), (r:Robot {{id:'R1'}}) CREATE (r)-[:CERCA]->(d);";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Cerca creata: Robot_id=R1 Cerca Disperso_id={id}");
    }

    public async Task UpdateMissingPersonNodeAsync(String id, Vector2Int cella){
        string idCella = await GetIDCellFromPosition(cella);
        string query = $"MATCH (d:Disperso {{id: '{id}'}}) SET d.cella = '{idCella}', d.stato_rilevamento = 'Trovato' RETURN d";
        await ExecuteQueryAsync(query);
        ////Debug.Log($"Nodo Disperso creato: id={id}, nome={nome}, eta={eta}, cella={idCella}, stato_rilevamento: '{stato}', zona: '{zona}'");        
        query = $"MATCH (r:Robot {{id:'R1'}})-[rel:CERCA]->(d:Disperso {{id: '{id}'}}) DELETE rel";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Cerca rimossa: Robot_id=R1 Cerca Disperso_id:{id}");
        query = $"MATCH (d:Disperso {{id: '{id}'}}), (r:Robot {{id:'R1'}}) CREATE (r)-[:HA_TROVATO]->(d);";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Ha_Trovato creata: Robot_id=R1 Ha_Trovato Disperso_id={id}");
    }

    public async Task CreateChargerBaseAsync(String id, String cella){
        string query = $"CREATE (b:Base {{id: '{id}', cella: '{cella}'}})";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Base creato: id={id}, cella={cella}");        
        query = $"MATCH (b:Base {{id: '{id}'}}), (r:Robot {{id: 'R1'}}) CREATE (b)-[:RICARICA]->(r)";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Ricarica creata: Base_id={id} Ricarica Robot_id=R1");
    }
    
    public async Task CreateRescueTeamAsync(String id, String ruolo){
        string query = $"CREATE (pc:Personale_Competente {{id: '{id}', ruolo: '{ruolo}'}})";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Personale Competente creato: id={id}, ruolo={ruolo}");        
        query = $"MATCH (pc:Personale_Competente {{id:'{id}'}}), (r:Robot {{id: 'R1'}}) CREATE (r)-[:COMUNICA]->(pc)";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Comunica creata: Robot_id=R1 Comunica Personale_Competente_id={id}");
    }

    public async Task<String> FindCellIdAsync(int pos_x, int pos_y){
        string query = $"MATCH (c:Cella {{posizione_x:'{pos_x}', posizione_y:'{pos_y}'}}) RETURN c.id as id_cell";
        var result = await ExecuteQueryAsync(query);

        if (result.Count > 0 && result[0].ContainsKey("id_cell")){
            // Convertire il valore in int in modo sicuro
            String id_cella = System.Convert.ToString(result[0]["id_cell"]);
            //Debug.Log($"Proprietà id_cella trovato: cella_x: {pos_x}, cella_y: {pos_y} in cella_id: {id_cella}");        
            return id_cella;
        }
        //Debug.Log($"Proprietà id_cella non trovato: cella_x: {pos_x}, cella_y: {pos_y} in cella_id: none");
        return null;
    }

    public async Task UpdateRobotPositionAsync(int pos_x, int pos_y){
        string id_cell = await FindCellIdAsync(pos_x, pos_y);
        if (id_cell != null){
            string query = $"MATCH (r:Robot {{id: 'R1'}}) SET r.cella = '{id_cell}'";
            await ExecuteQueryAsync(query);
            //Debug.Log($"Nodo Robot aggiornato: robot_id: R1, cella_x: {pos_x}, cella_y: {pos_y} in cella_id: {id_cell}");       
        }
    }


    
    // -----------------------------------------------------------------------------------------------------------------------

    public async Task<int> GetBatteryLevelAsync(){
        string query = "MATCH (r:Robot {id: 'R1'}) RETURN r.livello_batteria AS livello_batteria";
        var result = await ExecuteQueryAsync(query);

        if (result.Count > 0 && result[0].ContainsKey("livello_batteria")){
            int livelloBatteria = System.Convert.ToInt32(result[0]["livello_batteria"]);
            //Debug.Log($"Attributo livello_batteria recuperato: robot_id: R1, livello_batteria: {livelloBatteria}");
            return livelloBatteria;
        }

        //Debug.LogError("Impossibile recuperare il livello_batteria per robot_id: R1.");
        return 0;
    }

    public async System.Threading.Tasks.Task LoadDataFromCSV(){
        TextAsset csvFile = Resources.Load<TextAsset>("persone_disperse");
        if (csvFile == null){
            //Debug.LogError("File CSV non trovato!");
            return;
        }

        // await ExecuteQueryAsync("MATCH (p:Persona) DETACH DELETE p;");
        string[] lines = csvFile.text.Split('\n'); 
        for (int i = 1; i < lines.Length-1; i++){ 
            string[] columns = lines[i].Split(',');

            if (columns.Length < 6){
                //Debug.LogWarning($"Riga {i + 1} ha un formato non valido e sarà ignorata.");
                continue;
            }

            try{
                string id = columns[0].Trim();
                string nome = columns[1].Trim();
                string cognome = columns[2].Trim();
                string zona = columns[3].Trim();
                int eta = int.Parse(columns[4].Trim());
                string stato = columns[5].Trim();
                await CreateMissingPersonNodeAsync(id, $"{nome} {cognome}", eta, "", "Disperso", zona);
                // await ExecuteQueryAsync(
                //     $"CREATE (:Persona {{id: '{id}', nome: '{nome}', cognome: '{cognome}', zona: '{zona}', eta: '{eta}', stato: '{stato}'}})"
                // );
                //Debug.Log($"Dati caricati per {nome} {cognome}!");
            }catch (Exception ex){
                Debug.LogError($"Errore alla riga {i + 1}: {ex.Message}");
            }
        }

        //Debug.Log("Caricamento completato!");
    }


    public async Task<string> GetRobotZoneAsync(){
        string query = @"
            MATCH (r:Robot), (c:Cella)
            WHERE r.cella = c.id
            RETURN c.zona AS zona
            LIMIT 1";

        var result = await ExecuteQueryAsync(query, false);

        if (result.Count > 0 && result[0].ContainsKey("zona")){
            return result[0]["zona"].ToString();
        }
        return null;
    }

    public async Task<string> GetBaseCell(){
        string query = "MATCH (b:Base) RETURN b.cella AS cella";
        var result = await ExecuteQueryAsync(query, false); 
        if(result.Count > 0 && result[0].ContainsKey("cella")){
            return result[0]["cella"].ToString();
        }
        return null;
    }

    

    public async Task<string> GetYoungestMissingPersonAsync(){
        
        // string query = @"
        //     MATCH (p:Disperso {stato_rilevamento: 'Disperso'})
        //     RETURN p.id AS id, p.nome AS nome, p.eta AS eta, p.zona AS zona
        //     ORDER BY p.eta ASC
        //     LIMIT 1";
        
        int batteryLevel = await GetBatteryLevelAsync();
        Debug.Log($"batteryLevel {batteryLevel}");
        string robotZone = await GetRobotZoneAsync();
        Debug.Log($"robotZone {robotZone}");
        string baseCell = await GetBaseCell();
        Debug.Log($"baseCell {baseCell}");

        string query = @$"
                MATCH (p:Disperso {{stato_rilevamento: 'Disperso'}})
                WITH p.zona AS zona, 
                    COUNT(CASE WHEN p.eta <= 12 THEN 1 ELSE 0 END) AS under12Count
                WITH 
                    zona,
                    under12Count,
                    CASE
                        WHEN {batteryLevel} < 20 THEN '{baseCell}'
                        WHEN {batteryLevel} < 50 AND zona = '{robotZone}' THEN zona
                        ELSE zona
                    END AS result,
                    CASE
                        WHEN {batteryLevel} < 20 THEN 0
                        WHEN {batteryLevel} < 50 AND zona = '{robotZone}' THEN 1
                        ELSE 2
                    END AS priority
                ORDER BY 
                    priority ASC,
                    under12Count DESC
                RETURN 
                    result AS zona
                LIMIT 1";

        

        var result = await ExecuteQueryAsync(query, false);

        if (result.Count > 0 && result[0].ContainsKey("zona")){
            // string id = result[0]["id"].ToString();
            // string nome = result[0]["nome"].ToString();
            // int eta = Convert.ToInt32(result[0]["eta"]);
            string zona = result[0]["zona"].ToString();

            // Debug.Log($"Persona trovata: {nome}, Età: {eta}, Zona: {zona}, ID: {id}");
            Debug.Log($"RESULT {zona}");

            return zona;
        }
        

        //Debug.Log("Nessuna persona dispersa trovata.");
        return null;
    }


    public async Task<bool> UpdateNotFoundPersonAsync(string id_zona, string nuovoStato){

        try{
            string query = @$"
                MATCH (p:Disperso {{zona: '{id_zona}', stato_rilevamento: 'Disperso'}})
                SET p.stato_rilevamento = '{nuovoStato}'
                RETURN p.nome AS nome, p.stato_rilevamento AS stato";

            var result = await ExecuteQueryAsync(query, true);

            if (result.Count > 0 && result[0].ContainsKey("nome") && result[0].ContainsKey("stato")){
                string nome = result[0]["nome"].ToString();
                string statoAggiornato = result[0]["stato"].ToString();
                return true;
                //Debug.Log($"Stato aggiornato con successo per {nome}. Nuovo stato: {statoAggiornato}");
            }else{
                return false;
                //Debug.LogError("Aggiornamento stato fallito. Nessun risultato trovato.");
            }
        }catch (Exception ex){
            Debug.LogError($"Errore durante l'aggiornamento dello stato: {ex.Message}");
        }
        return false;
    }

    public async Task ComunicaConPersonaleAsync(string personaleId, string messaggio){
        string query = @$"
            MATCH (r:Robot {{id: 'R1'}}), (pc:PersonaleCompetente {{id: '{personaleId}'}})
            CREATE (r)-[:COMUNICA {{messaggio: '{messaggio}'}}]->(pc)";

        await ExecuteQueryAsync(query, true);
        //Debug.Log($"Messaggio inviato: '{messaggio}' dal robot R1 al personale {personaleId}");
    }

    public async Task ResetDatabaseAsync(){
        string query = "MATCH (n) DETACH DELETE n";
        await ExecuteQueryAsync(query,true);
        //Debug.Log("Database svuotato con successo!");
    }
 
    public async Task CreateRobotNodeAsync(String id, String nome, String cella, int livello_minimo_batteria, int livello_massimo_batteria, int livello_batteria = 100){
        string query = $@"CREATE (r:Robot {{id: '{id}',nome: '{nome}',livello_batteria: {livello_batteria},livello_minimo_batteria: {livello_minimo_batteria},livello_massimo_batteria: {livello_massimo_batteria},cella: '{cella}'}})"; 
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Robot creato: id={id}, nome={nome}, cella={cella}, livello_minimo_batteria:{livello_minimo_batteria}, livello_massimo_batteria:{livello_massimo_batteria}");
 
        query = $"MATCH (r:Robot {{id: 'R1'}}), (z:Zona) CREATE (r)-[:ESPLORA]->(z);";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Esplora creata: Robot_id=R1 Comunica Zona={id}");
    }
 
    public async Task CreateZoneNodeAsync(String id, int min_x, int min_y, int max_x, int max_y, bool visitata){
        string query = $"CREATE (:Zona {{ id: '{id}', min_x: {min_x}, min_y: {min_y}, max_x: {max_x}, max_y: {max_y}, visitata: '{(visitata ? "true" : "false")}' }})";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Zona creato: id={id}, min_x={min_x}, min_y={min_y}, max_x={max_x},max_y={max_y}, visitata: '{visitata}'");
    }

    public async Task RobotRilevaDispersoAsync(String idD){
        string query = $"MATCH (r:Robot {{id:'R1'}}), (d:Disperso {{id: '{idD}'}}) CREATE (r)-[:RILEVA]->(d)";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Rileva creata: Robot_id={idR} Comunica Disperso={idD}");
    }
    
    public async Task RobotSegnalaPersonaleCompetenteAsync(string idPc){
        string query = $"MATCH (r:Robot {{id:'R1'}}), (pc:Personale_Competente {{id: '{idPc}'}}) CREATE (r)-[:SEGNALA]->(pc)";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Segnala creata: Robot_id={idR} Segnala Personale_Competente_id={idPc}");
    }

    public async Task SetBatteryLevelAsync(int livello_batteria){
        string query = $"MATCH (r:Robot {{id: 'R1'}}) SET r.livello_batteria = '{livello_batteria}'";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Robot aggiornato: robot_id: R1, livello_batteria: {livello_batteria}");
    }

    public async Task<string> GetIDCellFromPosition(Vector2Int cell){
        string query = $"MATCH (c:Cella) WHERE c.posizione_x = {cell.x} AND c.posizione_y = {cell.y} RETURN c.id AS id_cella";
        var result = await ExecuteQueryAsync(query);
        if (result != null && result.Count > 0 && result[0].ContainsKey("id_cella"))
        {
            string id_cella = result[0]["id_cella"].ToString();
            return id_cella;
        }
        return null;
    }

}
