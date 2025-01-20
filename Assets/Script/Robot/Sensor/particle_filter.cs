using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParticleFilter : MonoBehaviour{
    public List<GameObject> landmarkObjects;
    public float motionNoise = 0.1f;
    public float sensorNoise = 0.5f;
    public float mapWidth = 20f;
    public float mapHeight = 20f;

    public List<Particle> particles = new List<Particle>();
    public List<Vector2> landmarks;

    public class Particle
    {
        public Vector2 position;
        public float orientation;
        public float weight;

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
                Mathf.Clamp(initialPosition.x + Random.Range(-2f, 2f), (-mapWidth-2)/ 2, (mapWidth+2)/ 2),
                Mathf.Clamp(initialPosition.z + Random.Range(-2f, 2f), (-mapHeight-2)/ 2, (mapHeight+2)/ 2)
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