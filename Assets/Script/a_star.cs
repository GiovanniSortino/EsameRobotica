// using System.Collections.Generic;
// using System;
// using System.Linq;
// using UnityEngine; // Usato per Vector2Int compatibile con unity

// public class Nodo{
//     public int x, y; 
//     public float gCost; //dal nodo di partenza
//     public float hCost; //al nodo di destinazione (euristica) 
//     public float fCost => gCost + hCost; 
//     public Nodo parent; 

//     public Nodo(int x, int y){
//         this.x = x; 
//         this.y = y;
//     }

// }

// public class AStar{
//     private int[] grid;
//     private int rows, cols; 

//     // Direzioni di movimento (su, giù, sinistra, destra, diagonali)
//     private Vector2Int[] directions = {
//         new Vector2Int(0, 1), new Vector2Int(1, 0),
//         new Vector2Int(0, -1), new Vector2Int(-1, 0),
//         new Vector2Int(1, 1), new Vector2Int(1, -1),
//         new Vector2Int(-1, 1), new Vector2Int(-1, -1)
//     };

//     public AStar(int[] grid){
//         this.grid = grid; 
//         rows = grid.GetLength(0);
//         cols = grid.GetLength(1);
//     }
    
//     private List<Nodo> RicostruisciPercorso(Nodo node){
//         List<Nodo> path = new List<Nodo>();
//         while (node != null)
//         {
//             path.Add(node);
//             node = node.parent;
//         }
//         path.Reverse(); // Percorso dalla partenza alla destinazione
//         return path;
//     }

//     //distanza di Manhattan
//     private float CalcolaDistanza(Vector2Int a, Vector2Int b){
//         return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
//     }
//     //verifica se la pos rientra nella grid
//     private bool PosizioneValida(Vector2Int pos){
//         return pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols;
//     }

//     public List<Nodo> TrovaPercorso(Vector2Int start, Vector2Int goal){
//         PriorityQueue<Nodo, float> openList = new PriorityQueue<Nodo, float>(); //uso una coda con priorit per memorizzare i nodi da esplorare e li ordino per costo 
//         HashSet<Nodo> closedList = new HashSet<Nodo>();

//         Nodo startNode = new Nodo(start.x, start.y){
//             gCost = 0, 
//             hCost = CalcolaDistanza(start, goal)
//         };
//         openList.Enqueue(startNode, startNode.fCost);

//         while(openList.Count > 0){
//             Nodo currentNode = openList.Dequeue();
//             if (currentNode.x == goal.x && currentNode.y == goal.y){ //se il nodo attuale è uguale al goal
//                 return RicostruisciPercorso(currentNode);
//             }

//             openList.Remove(currentNode);
//             closedList.Add(currentNode);

//             foreach(var direction in directions){
//                 Vector2Int neighborPos = new Vector2Int(currentNode.x + direction.x, currentNode.y + direction.y);

//                 if (!PosizioneValida(neighborPos))
//                     continue; //se la posizione non è valida continua, verificare anche la presenza di ostacoli

//                 Nodo neighbor = new Nodo(neighborPos.x, neighborPos.y);
//                 if (closedList.Any(n => n.x == neighbor.x && n.y == neighbor.y))
//                     continue; // Se è già nella lista chiusa, salta

//                 float tentativeGCost = currentNode.gCost + CalcolaDistanza(new Vector2Int(currentNode.x, currentNode.y), neighborPos); //il costo per arrivare al nodo attuale piu quello per arrivare al vicino
//                 if (tentativeGCost < neighbor.gCost || !openList.Any(n => n.x == neighbor.x && n.y == neighbor.y)){
//                 //se il costo per arrivare al vicino dal nodo attuale è minore di quello per arrivare al vicino 
//                 //dal punto di partenza (il gCost del vicino) e se il vicino non è gia nella lista aperta
//                 //memorizzo anche il parent in modo da poter ricostruire il percorso una volta raggiunto il goal
//                     neighbor.gCost = tentativeGCost;
//                     neighbor.hCost = CalcolaDistanza(neighborPos, goal);
//                     neighbor.parent = currentNode;

//                 // Aggiungi il nodo alla lista aperta se non è già presente
//                 if (!openList.Any(n => n.x == neighbor.x && n.y == neighbor.y))
//                     openList.Enqueue(neighbor, neighbor.fCost);
//                 }
//             }
//         }

//         // Nessun percorso trovato
//         return null;
                
//     }

// }
