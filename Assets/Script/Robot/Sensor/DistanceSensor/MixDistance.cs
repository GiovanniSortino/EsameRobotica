using UnityEngine;
using System.Linq;

public class MixDistance : MonoBehaviour
{
    public DistanceSensorDario[] distanceSensors;
    public float distanceCalculated;

    public float distance()
    {
        distanceCalculated = distanceSensors.Min(sensor => sensor.DetectObjects());
        return distanceCalculated;
    }
}
