using UnityEngine;

public class MixDistance : MonoBehaviour
{

    public DistanceSensorDario distanceSensorCenter;
    public DistanceSensorDario distanceSensorLeft;
    public DistanceSensorDario distanceSensorRight;

    public float distance()
    {
        float centerDistance = distanceSensorCenter.DetectObjects();
        float leftDistance = distanceSensorLeft.DetectObjects();
        float rightDistance = distanceSensorRight.DetectObjects();

        return Mathf.Min(Mathf.Min(centerDistance, leftDistance), rightDistance);
    }
}
