using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParticleFilter : MonoBehaviour{
    public List<GameObject> landmarkObjects; // Landmark nella scena
    // public int particleCount = 500; // Numero di particelle
    public float motionNoise = 0.1f; // Rumore di movimento
    public float sensorNoise = 0.5f; // Rumore dei sensori
    public float mapWidth = 20f; // Larghezza della mappa
    public float mapHeight = 20f; // Altezza della mappa

    public List<Particle> particles = new List<Particle>();
    public List<Vector2> landmarks; // Posizioni dei landmark

    public class Particle
    {
        public Vector2 position; // Posizione (x, y)
        public float orientation; // Orientamento
        public float weight; // Peso della particella

        public Particle(Vector2 position, float orientation)
        {
            this.position = position;
            this.orientation = orientation;
            this.weight = 1.0f;
        }
    }

    public void InitializeParticles(Vector3 initialPosition, int count = 1000)
    {

        for (int i = 0; i < count; i++)
        {
            Vector2 randomPosition = new Vector2(
                Mathf.Clamp(initialPosition.x + Random.Range(-1f, 1f), -mapWidth / 2, mapWidth / 2),
                Mathf.Clamp(initialPosition.z + Random.Range(-1f, 1f), -mapHeight / 2, mapHeight / 2)
            );
            float randomOrientation = Random.Range(0, 2 * Mathf.PI);
            particles.Add(new Particle(randomPosition, randomOrientation));
        }
    }

    public void InitializeLandmarks()
    {
        if (landmarkObjects == null || landmarkObjects.Count == 0)
        {
            //Debug.LogError("Nessun landmark assegnato.");
            return;
        }

        landmarks = landmarkObjects.Select(landmark =>
        {
            Vector3 position = landmark.transform.position;
            return new Vector2(position.x, position.z);
        }).ToList();

        //Debug.Log($"Landmark inizializzati: {landmarks.Count}");
    }

    public void Predict(float deltaX, float deltaY, float deltaTheta)
    {
        foreach (var p in particles)
        {
            p.position += new Vector2(
                Mathf.Cos(p.orientation) * deltaX - Mathf.Sin(p.orientation) * deltaY + Random.Range(-motionNoise, motionNoise),
                Mathf.Sin(p.orientation) * deltaX + Mathf.Cos(p.orientation) * deltaY + Random.Range(-motionNoise, motionNoise)
            );
            p.orientation = Mathf.Repeat(
                p.orientation + deltaTheta + Random.Range(-motionNoise, motionNoise),
                2 * Mathf.PI
            );
        }
    }


    public void UpdateWeights(float[] observedDistances)
    {
        foreach (var p in particles)
        {
            p.weight = CalculateWeight(p, landmarks, observedDistances);
        }
    }

    public float CalculateWeight(Particle particle, List<Vector2> landmarks, float[] observedDistances)
    {
        float weight = 1.0f;
        float adjustedSensorNoise = Mathf.Max(sensorNoise, 0.01f);
        

        for (int i = 0; i < landmarks.Count; i++)
        {
            float simulatedDistance = Vector2.Distance(particle.position, landmarks[i]);
            float error = observedDistances[i] - simulatedDistance;
            float gauss = Mathf.Exp(-Mathf.Pow(error, 2) / (2 * Mathf.Pow(adjustedSensorNoise, 2))) 
              / Mathf.Sqrt(2 * Mathf.PI * Mathf.Pow(adjustedSensorNoise, 2));
            weight *= Mathf.Min(gauss, 1e-6f);
        }

        return weight;
    }

    public void Resample(){
        NormalizeWeights();
        int count = 0;
        List<Particle> new_particles = new();
        float weightMax = particles.Max(p => p.weight);
        float threshold = Random.Range(weightMax/2, 3*weightMax/4);
        foreach (Particle p in particles){
            if (p.weight >= threshold){
                new_particles.Add(new Particle(
                        p.position,
                        p.orientation
                    ));
            }else{
                count++;
            }
        }
        particles = new_particles;
        InitializeParticles(EstimatePosition(), count);
        NormalizeWeights();
        //Debug.Log($"Il numero di particelle è {particles.Count}");
    }

    // public List<Particle> Resample()
    // {
    //     // Normalizza i pesi
    //     NormalizeWeights();

    //     // Calcola la distribuzione cumulativa
    //     List<float> cumulativeWeights = new List<float>();
    //     float cumulativeSum = 0;
    //     foreach (var particle in particles)
    //     {
    //         cumulativeSum += particle.weight;
    //         cumulativeWeights.Add(cumulativeSum);
    //     }

    //     // Esegui il resampling
    //     List<Particle> newParticles = new List<Particle>();
    //     int numParticles = particles.Count;
    //     System.Random random = new System.Random();

    //     for (int i = 0; i < numParticles; i++)
    //     {
    //         float rand = (float)random.NextDouble(); // Numero casuale tra 0 e 1
    //         for (int j = 0; j < cumulativeWeights.Count; j++)
    //         {
    //             if (rand <= cumulativeWeights[j])
    //             {
    //                 // Crea una nuova particella copiando quella selezionata
    //                 var selectedParticle = particles[j];
    //                 newParticles.Add(new Particle(
    //                     selectedParticle.position,
    //                     selectedParticle.orientation
    //                 ));
    //                 break;
    //             }
    //         }
    //     }

    //     particles = newParticles;
    //     return newParticles;
    // }

    public void NormalizeWeights(){
        float totalWeight = particles.Sum(p => p.weight);

        if (totalWeight > 0){
            foreach (var p in particles){
                p.weight /= totalWeight;
            }
        }else{
            //Debug.LogWarning("Tutti i pesi sono nulli, reinizializzazione intorno all'ultima posizione stimata.");

            Vector3 lastEstimatedPosition = EstimatePosition();
            InitializeParticles(lastEstimatedPosition);
        }
    }


   public Vector3 EstimatePosition()
    {

        float xSum = 0, ySum = 0, weightSum = 0;

        foreach (var p in particles)
        {
            xSum += p.position.x * p.weight;
            ySum += p.position.y * p.weight;
            weightSum += p.weight;
        }

        return weightSum > 0 ? new Vector3(xSum / weightSum, transform.position.y, ySum / weightSum) : Vector3.zero;
    }

    public void OnDrawGizmos()
    {
        if (particles != null)
        {
            Gizmos.color = Color.red;
            foreach (var p in particles)
            {
                Gizmos.DrawSphere(new Vector3(p.position.x, 0, p.position.y), 0.1f);
            }
        }

        if (landmarks != null)
        {
            Gizmos.color = Color.grey;
            foreach (var lm in landmarks)
            {
                Gizmos.DrawSphere(new Vector3(lm.x, 0, lm.y), 0.5f);
            }
        }

        Gizmos.color = Color.blue;
        Vector3 estimatedPosition = EstimatePosition();
        Gizmos.DrawSphere(estimatedPosition, 0.2f);
    }
}




// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// public class ParticleFilter : MonoBehaviour
// {
//     public List<GameObject> landmarkObjects;
//     public float motionNoise = 0.1f;
//     public float sensorNoise = 0.5f;
//     public float mapWidth;
//     public float mapHeight;

//     public List<Particle> particles = new List<Particle>();
//     public List<Vector2> landmarks;
//     public static int numParticle = 2000;

//     public class Particle
//     {
//         public Vector2 position;
//         public float orientation;
//         public float weight;

//         public Particle(Vector2 position, float orientation)
//         {
//             this.position = position;
//             this.orientation = orientation;
//             this.weight = 1.0f / numParticle;
//         }
//     }

//     public void InitializeParticles(Vector3 initialPosition, int count = -1)
//     {
//         if (count == -1) count = numParticle;

//         particles.Clear();
//         for (int i = 0; i < count; i++)
//         {
//             Vector2 randomPosition = new Vector2(
//                 Mathf.Clamp(initialPosition.x + Random.Range(-2f, 2f), -mapWidth / 2, mapWidth / 2),
//                 Mathf.Clamp(initialPosition.z + Random.Range(-2f, 2f), -mapHeight / 2, mapHeight / 2)
//             );
//             float randomOrientation = Random.Range(0, 2 * Mathf.PI);
//             particles.Add(new Particle(randomPosition, randomOrientation));
//         }
//     }

//     public void InitializeLandmarks()
//     {
//         if (landmarkObjects == null || landmarkObjects.Count == 0)
//         {
//             Debug.LogError("Nessun landmark assegnato.");
//             return;
//         }

//         landmarks = landmarkObjects.Select(landmark =>
//         {
//             Vector3 position = landmark.transform.position;
//             return new Vector2(position.x, position.z);
//         }).ToList();

//         Debug.Log($"Landmark inizializzati: {landmarks.Count}");
//     }

//     public void Predict(float deltaX, float deltaY, float deltaTheta)
//     {
//         foreach (var p in particles)
//         {
//             float noiseX = GaussianNoise(0, motionNoise);
//             float noiseY = GaussianNoise(0, motionNoise);
//             float noiseTheta = GaussianNoise(0, motionNoise / 2);

//             float newX = p.position.x + (deltaX + noiseX) * Mathf.Cos(p.orientation);
//             float newY = p.position.y + (deltaY + noiseY) * Mathf.Sin(p.orientation);
//             float newTheta = Mathf.Repeat(p.orientation + deltaTheta + noiseTheta, 2 * Mathf.PI);

//             p.position = new Vector2(newX, newY);
//             p.orientation = newTheta;
//         }
//     }

//     private float GaussianNoise(float mean, float variance)
//     {
//         float u1 = 1.0f - Random.value;
//         float u2 = 1.0f - Random.value;
//         float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
//         return mean + variance * randStdNormal;
//     }

//     public void UpdateWeights(float[] observedDistances)
//     {
//         float totalWeight = 0f;

//         foreach (var p in particles)
//         {
//             p.weight = CalculateWeight(p, landmarks, observedDistances);
//             totalWeight += p.weight;
//         }

//         if (totalWeight > 0)
//         {
//             foreach (var p in particles)
//                 p.weight /= totalWeight; // Normalizzazione
//         }
//         else
//         {
//             Debug.LogWarning("Tutti i pesi sono nulli, reinizializzazione intorno all'ultima posizione stimata.");
//             InitializeParticles(EstimatePosition());
//         }
//     }

//     public float CalculateWeight(Particle particle, List<Vector2> landmarks, float[] observedDistances)
//     {
//         float logWeight = 0.0f;
//         float adjustedSensorNoise = Mathf.Max(sensorNoise, 0.01f);

//         for (int i = 0; i < landmarks.Count; i++)
//         {
//             float simulatedDistance = Vector2.Distance(particle.position, landmarks[i]);
//             float error = observedDistances[i] - simulatedDistance;

//             float exponent = -Mathf.Pow(error, 2) / (2 * Mathf.Pow(adjustedSensorNoise, 2));
//             float logGauss = exponent - Mathf.Log(Mathf.Sqrt(2 * Mathf.PI) * adjustedSensorNoise);

//             logWeight += logGauss;
//         }

//         return Mathf.Exp(logWeight);
//     }

//     // 🔹 Implementazione del RESAMPLING CUMULATIVO SISTEMATICO 🔹
//     public void Resample()
//     {
//         NormalizeWeights();

//         int N = particles.Count;
//         List<Particle> newParticles = new();

//         float[] cumulativeWeights = new float[N];
//         cumulativeWeights[0] = particles[0].weight;

//         for (int i = 1; i < N; i++)
//             cumulativeWeights[i] = cumulativeWeights[i - 1] + particles[i].weight;

//         float step = 1.0f / N;
//         float start = Random.Range(0, step);
//         int index = 0;

//         for (int i = 0; i < N; i++)
//         {
//             float threshold = start + i * step;
//             while (index < cumulativeWeights.Count() && threshold > cumulativeWeights[index])
//                 index++;

//             newParticles.Add(new Particle(particles[index].position, particles[index].orientation));
//         }

//         particles = newParticles;

//         // Dopo il resampling, tutti i pesi devono essere uguali
//         foreach (var p in particles)
//         {
//             p.weight = 1.0f / N;
//         }

//         //Debug.Log($"Il numero di particelle dopo il resampling: {particles.Count}");
//     }

//     public void NormalizeWeights()
//     {
//         float totalWeight = particles.Sum(p => p.weight);

//         if (totalWeight > 0)
//         {
//             foreach (var p in particles)
//                 p.weight /= totalWeight;
//         }
//         else
//         {
//             Debug.LogWarning("Tutti i pesi sono nulli, reinizializzazione intorno all'ultima posizione stimata.");
//             InitializeParticles(EstimatePosition());
//         }
//     }

//     public Vector3 EstimatePosition()
//     {
//         float xSum = 0, ySum = 0, weightSum = 0;

//         foreach (var p in particles)
//         {
//             xSum += p.position.x * p.weight;
//             ySum += p.position.y * p.weight;
//             weightSum += p.weight;
//         }

//         return weightSum > 0 ? new Vector3(xSum / weightSum, transform.position.y, ySum / weightSum) : Vector3.zero;
//     }

//     public void OnDrawGizmos()
//     {
//         if (particles != null)
//         {
//             Gizmos.color = Color.red;
//             foreach (var p in particles)
//                 Gizmos.DrawSphere(new Vector3(p.position.x, 0, p.position.y), 0.1f);
//         }

//         if (landmarks != null)
//         {
//             Gizmos.color = Color.grey;
//             foreach (var lm in landmarks)
//                 Gizmos.DrawSphere(new Vector3(lm.x, 0, lm.y), 0.5f);
//         }

//         Gizmos.color = Color.blue;
//         Vector3 estimatedPosition = EstimatePosition();
//         Gizmos.DrawSphere(estimatedPosition, 0.2f);
//     }
// }
