using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Neo4j.Driver;
using System.Threading.Tasks;

public class Prova : MonoBehaviour
{
    private IDriver _driver;

    async void Start()
    {
        Debug.Log("Avvio del tentativo di connessione a Neo4j...");
        await ConnectToDatabaseAsync();
    }

    private async Task ConnectToDatabaseAsync()
    {
        var uri = "bolt://localhost:7687";
        var user = "neo4j";
        var password = "password";

        try
        {
            // Creazione del driver
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
            Debug.Log("Connessione al database Neo4j riuscita!");

            // Creazione della sessione asincrona
            await using var session = _driver.AsyncSession(o => o.WithDatabase("neo4j"));
            Debug.Log("Sessione avviata, eseguo la query...");

            // Esecuzione della transazione in lettura
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("MATCH (n) RETURN count(n) AS count");
                var record = await cursor.SingleAsync();
                return record["count"].As<int>();
            });

            Debug.Log($"Numero di nodi nel database: {result}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Errore durante la connessione o l'esecuzione della query: {ex.Message}");
        }
    }

    private async void OnDestroy()
    {
        Debug.Log("Disconnessione dal database...");
        if (_driver != null)
        {
            await _driver.DisposeAsync();
            Debug.Log("Driver disconnesso correttamente.");
        }
    }
}
