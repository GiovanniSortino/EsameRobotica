using System.Collections.Generic;
using UnityEngine;
using Neo4j.Driver;
using System.Threading.Tasks;
using System;

public class DatabaseManager{
    private static IDriver _driver;
    private static DatabaseManager databaseManager;
    public static SapiTextToSpeech sapiTextToSpeech;
    private DatabaseManager(){}

    public static DatabaseManager GetDatabaseManager(){
        if (databaseManager == null){
            databaseManager = new();
            sapiTextToSpeech = new();
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
    
    //OK
    public async Task CreateObstacleNodeAsync(String id, String cellaId, Vector2Int obstacleCell, string zone){
        String cella = await FindCellIdAsync(obstacleCell.x, obstacleCell.y);
        if(cella == null){
            await CreateCellNodeAsync(cellaId, obstacleCell.x, obstacleCell.y, zone);
        }else{
            cellaId = cella;
        }
        string query = $"CREATE (:Ostacolo {{id: '{id}', cella: '{cellaId}' }})";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Ostacolo creato: id={id}, cella={cella}");
        query = $"MATCH (r:Robot {{id: 'R1'}}), (o:Ostacolo {{id:'{id}'}}) CREATE (r)-[:SEGNALA]->(o);";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Segnala creata: Robot_id=R1 segnala Ostacoll_id:{id}");
        query = $"MATCH (o:Ostacolo {{id: '{id}'}}), (c:Cella {{id: '{cellaId}'}}) CREATE (o)-[:BLOCCA]->(c);";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Blocca creata: Ostacolo_id:{id} blocca Cella_id:{cella}");
    }


    //OK
    public async Task CreateCellNodeAsync(String id, int x, int y, String zona){
        string query = $@"
            MERGE (c:Cella {{id: '{id}'}})
            ON CREATE SET c.posizione_x = {x}, c.posizione_y = {y}, c.zona = '{zona}'
            RETURN c";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Cella creato: id={id}, posizione_x={x}, posizione_y={y}, zona={zona}");
        query = $@"
            MATCH (c:Cella {{id: '{id}'}}), (z:Zona {{id: '{zona}'}})
            MERGE (z)-[:E_FORMATA]->(c);
        ";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione é_formata creata: Zona_id={zona} è formata da Cella_id:{id}");
    }

    //OK
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

    //OK
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

    //OK
    public async Task CreateChargerBaseAsync(String id, String cella){
        string query = $"CREATE (b:Base {{id: '{id}', cella: '{cella}'}})";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Base creato: id={id}, cella={cella}");        
        query = $"MATCH (b:Base {{id: '{id}'}}), (r:Robot {{id: 'R1'}}) CREATE (b)-[:RICARICA]->(r)";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Ricarica creata: Base_id={id} Ricarica Robot_id=R1");
    }
    
    //OK
    public async Task CreateRescueTeamAsync(String id, String ruolo){
        string query = $"CREATE (pc:Personale_Competente {{id: '{id}', ruolo: '{ruolo}'}})";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Personale Competente creato: id={id}, ruolo={ruolo}");        
        query = $"MATCH (pc:Personale_Competente {{id:'{id}'}}), (r:Robot {{id: 'R1'}}) CREATE (r)-[:COMUNICA]->(pc)";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Comunica creata: Robot_id=R1 Comunica Personale_Competente_id={id}");
    }

    //OK
    public async Task<String> FindCellIdAsync(int pos_x, int pos_y){
        string query = $"MATCH (c:Cella {{posizione_x:'{pos_x}', posizione_y:'{pos_y}'}}) RETURN c.id as id_cell";
        var result = await ExecuteQueryAsync(query);

        if (result.Count > 0 && result[0].ContainsKey("id_cell")){
            String id_cella = System.Convert.ToString(result[0]["id_cell"]);
            //Debug.Log($"Proprietà id_cella trovato: cella_x: {pos_x}, cella_y: {pos_y} in cella_id: {id_cella}");        
            return id_cella;
        }
        //Debug.Log($"Proprietà id_cella non trovato: cella_x: {pos_x}, cella_y: {pos_y} in cella_id: none");
        return null;
    }

    //OK
    public async Task UpdateRobotPositionAsync(int pos_x, int pos_y){
        string id_cell = await FindCellIdAsync(pos_x, pos_y);
        if (id_cell != null){
            string query = $"MATCH (r:Robot {{id: 'R1'}}) SET r.cella = '{id_cell}'";
            await ExecuteQueryAsync(query);
            //Debug.Log($"Nodo Robot aggiornato: robot_id: R1, cella_x: {pos_x}, cella_y: {pos_y} in cella_id: {id_cell}");       
        }
    }

    //OK
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

    //OK
    public async Task LoadDataFromCSV(){
        TextAsset csvFile = Resources.Load<TextAsset>("persone_disperse");
        if (csvFile == null){
            //Debug.LogError("File CSV non trovato!");
            return;
        }

        string[] lines = csvFile.text.Split('\n'); 
        for (int i = 1; i < lines.Length-1; i++){ 
            string[] columns = lines[i].Split(',');

            if (columns.Length < 6){
                Debug.LogWarning($"Riga {i + 1} ha un formato non valido e sarà ignorata.");
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
            }catch (Exception ex){
                Debug.LogError($"Errore alla riga {i + 1}: {ex.Message}");
            }
        }
    }

    //OK
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

    //OK
    public async Task<string> GetBaseCell(){
        string query = "MATCH (b:Base) RETURN b.cella AS cella";
        var result = await ExecuteQueryAsync(query, false); 
        if(result.Count > 0 && result[0].ContainsKey("cella")){
            return result[0]["cella"].ToString();
        }
        return null;
    }

    //OK
    public async Task<string> GetBasePositionZone(){
        string query = @"
            MATCH (b:Base), (c:Cella), (z:Zona)
            WHERE b.cella = c.id AND (z)-[:E_FORMATA]->(c)
            RETURN z.id AS zonaId
            ";

        var result = await ExecuteQueryAsync(query, false);
        if (result.Count > 0 && result[0].ContainsKey("zona")){
            return result[0]["zona"].ToString();
        }
        return null;
    }

    //OK
    public async Task<Vector2Int?> GetBasePosition(){
        string query = @"
            MATCH (b: Base), (c:Cella)
            WHERE b.cella = c.id
            RETURN c.posizione_x AS x, c.posizione_y as y
            LIMIT 1";

        var result = await ExecuteQueryAsync(query, false);
        if (result.Count > 0 && result[0].ContainsKey("x")){
            return new Vector2Int(int.Parse(result[0]["x"].ToString()), int.Parse(result[0]["y"].ToString()));
        }
        return null;
    }        

    //OK
    public async Task<string> GetYoungestMissingPersonAsync(bool isEnabled){        
        int batteryLevel = await GetBatteryLevelAsync();
        Debug.Log($"batteryLevel {batteryLevel}");
        string robotZone = await GetRobotZoneAsync();
        Debug.Log($"robotZone {robotZone}");
        string baseCell = await GetBaseCell();
        Debug.Log($"baseCell {baseCell}");
        string baseZone = await GetBasePositionZone();
        Debug.Log($"baseZone {baseZone}");

        string query = @$"
                MATCH (p:Disperso {{stato_rilevamento: 'Disperso'}})
                WITH 
                    p.zona AS zona, 
                    SUM(CASE WHEN toInteger(p.eta) <= 12 THEN 1 ELSE 0 END) AS under12Count,
                    MIN(toInteger(p.eta)) AS minEta
                WITH 
                    zona,
                    under12Count,
                    minEta,
                    CASE
                        WHEN {batteryLevel} < 30 THEN '{baseCell}'
                        ELSE zona
                    END AS result,
                    CASE
                        WHEN {batteryLevel} < 30 THEN 0
                        ELSE 1
                    END AS priority
                ORDER BY 
                    priority ASC,
                    under12Count DESC,
                    minEta ASC
                RETURN 
                    result AS zona,
                    under12Count AS Count,
                    minEta AS eta
                LIMIT 1";

        var result = await ExecuteQueryAsync(query, false);

        if (result.Count > 0 && result[0].ContainsKey("zona")){
            string zona = result[0]["zona"].ToString();
            string under12Count = result[0]["Count"].ToString();
            string minEta = result[0]["eta"].ToString();
            if(isEnabled){
                if (zona == "C0"){
                sapiTextToSpeech.Speak($"Sto andando a ricaricarmi in cella {zona}");
                }else{
                    sapiTextToSpeech.Speak($"Sto andando in zona {zona} perchè il mio livello di batteria è {batteryLevel}%, in questa zona ho trovato {under12Count} {(under12Count == "1" ? "persona" : "persone")} con meno di 12 anni e l'età minima è {minEta}");
                }
            }

            return zona;
        }
        sapiTextToSpeech.Speak($"Ho terminato la ricerca. Torno alla base di ricarica.");
        return "End";
    }

    //OK
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

    //OK
    public async Task ResetAsync(){
        string query = "MATCH (n) DETACH DELETE n";
        await ExecuteQueryAsync(query,true);
        //Debug.Log("Database svuotato con successo!");
    }
    
    //OK
    public async Task CreateRobotNodeAsync(String id, String nome, String cella, int livello_minimo_batteria, int livello_massimo_batteria, int livello_batteria){
        string query = $@"CREATE (r:Robot {{id: '{id}',nome: '{nome}',livello_batteria: {livello_batteria},livello_minimo_batteria: {livello_minimo_batteria},livello_massimo_batteria: {livello_massimo_batteria},cella: '{cella}'}})"; 
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Robot creato: id={id}, nome={nome}, cella={cella}, livello_minimo_batteria:{livello_minimo_batteria}, livello_massimo_batteria:{livello_massimo_batteria}");
 
        query = $"MATCH (r:Robot {{id: 'R1'}}), (z:Zona) CREATE (r)-[:ESPLORA]->(z);";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Relazione Esplora creata: Robot_id=R1 Comunica Zona={id}");
    }
 
    //OK
    public async Task CreateZoneNodeAsync(String id, int min_x, int min_y, int max_x, int max_y, bool visitata){
        string query = $"CREATE (:Zona {{ id: '{id}', min_x: {min_x}, min_y: {min_y}, max_x: {max_x}, max_y: {max_y}, visitata: '{(visitata ? "true" : "false")}' }})";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Zona creato: id={id}, min_x={min_x}, min_y={min_y}, max_x={max_x},max_y={max_y}, visitata: '{visitata}'");
    }

    //OK
    public async Task SetBatteryLevelAsync(int livello_batteria){
        string query = $"MATCH (r:Robot {{id: 'R1'}}) SET r.livello_batteria = '{livello_batteria}'";
        await ExecuteQueryAsync(query);
        //Debug.Log($"Nodo Robot aggiornato: robot_id: R1, livello_batteria: {livello_batteria}");
    }

    //OK
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

    public async Task RemoveObstacleAsync(string zone){
        string query = @$"MATCH (o:Ostacolo), (c:Cella), (z:Zona)
                        WHERE (o)-[:BLOCCA]->(c) AND c.zona = {zone}
                        DETACH DELETE o";
        var result = await ExecuteQueryAsync(query); 
    }

    public async Task UpdateZoneNodeAsync(String id){
        string query = $"MATCH (z:Zona {{id: '{id}'}}) SET z.visitata = 'true' RETURN z";
        await ExecuteQueryAsync(query);
    }

}
