using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParticleFilter : MonoBehaviour{
    // Riferimenti ai landmark nella scena
    public List<GameObject> landmarkObjects; // Riferimento ai GameObject dei landmark

    // Parametri principali
    public int particleCount = 1000; // Numero di particelle
    public float motionNoise = 0.1f; // Rumore di movimento
    public float sensorNoise = 0.5f; // Rumore dei sensori
    public float resamplingThreshold = 0.5f; // Soglia di riacampionamento

    public List<Particle> particles; // Lista delle particelle
    public List<Vector2> landmarks; // Lista delle posizioni dei landmark

    // Classe Particella
    public class Particle{
        public Vector2 position; // Posizione della particella (x, y)
        public float orientation; // Orientamento della particella (angolo in radianti)
        public float weight; // Peso della particella (probabilit�)

        public Particle(Vector2 position, float orientation){
            this.position = position;
            this.orientation = orientation;
            this.weight = 1.0f; // Peso iniziale uguale per tutte le particelle
        }
    }

    // Inizializza le particelle
    public void InitializeParticles(){
        particles = new List<Particle>();

        for (int i = 0; i < particleCount; i++){
            Vector2 randomPosition = new Vector2(Random.Range(-35, 35), Random.Range(-35, 35));
            float randomOrientation = Random.Range(0, 2 * Mathf.PI);
            particles.Add(new Particle(randomPosition, randomOrientation));
        }

        Debug.Log($"Initialized {particles.Count} particles.");
    }

    // Inizializza i landmark
    public void InitializeLandmarks(){
        if (landmarkObjects == null || landmarkObjects.Count == 0){
            Debug.LogError("No landmark objects assigned in the Inspector.");
            return;
        }

        landmarks = new List<Vector2>();

        foreach (GameObject landmark in landmarkObjects){
            if (landmark != null){
                Vector3 position = landmark.transform.position;
                landmarks.Add(new Vector2(position.x, position.z));
            }
        }

        Debug.Log($"Initialized {landmarks.Count} landmarks.");
    }


    // Predice la posizione delle particelle in base al movimento del robot
    public void Predict(float deltaX, float deltaY, float deltaTheta){
        foreach (Particle p in particles){
            p.position += new Vector2(
                Mathf.Cos(p.orientation) * deltaX - Mathf.Sin(p.orientation) * deltaY,
                Mathf.Sin(p.orientation) * deltaX + Mathf.Cos(p.orientation) * deltaY
            );

            p.orientation += deltaTheta;

            // Rumore limitato
            p.position.x += Random.Range(-motionNoise, motionNoise);
            p.position.y += Random.Range(-motionNoise, motionNoise);
            p.orientation += Random.Range(-motionNoise, motionNoise);
            p.orientation = Mathf.Repeat(p.orientation, 2 * Mathf.PI); // Mantieni l'orientamento entro [0, 2π]
        }
    }


    // Calcola i pesi delle particelle basandosi sui landmark
    public void UpdateWeights(float[] observedDistances){
        foreach (Particle p in particles){
            p.weight = CalculateWeight(p, landmarks, observedDistances);
        }
    }

    // Funzione per calcolare il peso di una particella
    public float CalculateWeight(Particle particle, List<Vector2> landmarks, float[] observedDistances){
        float weight = 1.0f;

        for (int i = 0; i < landmarks.Count; i++){
            // Calcola la distanza simulata dal landmark
            float simulatedDistance = Vector2.Distance(particle.position, landmarks[i]);

            // Calcola la probabilit� (funzione gaussiana)
            float error = observedDistances[i] - simulatedDistance;
            weight *= Mathf.Exp(-Mathf.Pow(error, 2) / (2 * Mathf.Pow(sensorNoise, 2)));
        }

        return weight;
    }

    // Riacampionamento delle particelle
    public void ResampleParticles(){
        List<Particle> newParticles = new List<Particle>();
        float maxWeight = particles.Max(p => p.weight);

        foreach (Particle p in particles){
            // Metodo della "ruota della fortuna"
            if (Random.Range(0.0f, maxWeight) <= p.weight){
                newParticles.Add(new Particle(p.position, p.orientation));
            }
        }

        particles = newParticles;
    }

    public void NormalizeWeights(){
        float totalWeight = particles.Sum(p => p.weight);
        if (totalWeight > 0){
            foreach (Particle p in particles){
                p.weight /= totalWeight;
            }
        }else{
            Debug.LogError("Total weight is zero or negative. Check sensor readings or weight calculations.");
        }
    }


    // Stima la posizione del robot basandosi sulle particelle
    public Vector2 EstimatePosition(){
        if (particles == null || particles.Count == 0){
            Debug.LogError("Particles list is null or empty. Cannot estimate position.");
            return Vector2.zero; // Valore di fallback
        }

        float xSum = 0, ySum = 0, weightSum = 0;

        foreach (Particle p in particles){
            xSum += p.position.x * p.weight;
            ySum += p.position.y * p.weight;
            weightSum += p.weight;
        }

        return new Vector2(xSum / weightSum, ySum / weightSum);
    }

    public void OnDrawGizmos(){
        if (particles != null){
            Gizmos.color = Color.green;
            foreach (Particle p in particles){
                Gizmos.DrawSphere(new Vector3(p.position.x, 0, p.position.y), 0.1f);
            }
        }

        if (landmarks != null){
            Gizmos.color = Color.red;
            foreach (Vector2 lm in landmarks){
                Gizmos.DrawSphere(new Vector3(lm.x, 0, lm.y), 0.5f);
            }
        }

        if (particles != null && particles.Count > 0){
            Vector2 estimatedPosition = EstimatePosition();
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(new Vector3(estimatedPosition.x, 0, estimatedPosition.y), 0.5f);
        }
    }
}
