using UnityEngine;

public class MixDistance: MonoBehaviour{

    public DistanceSensor distanceSensorCenter;
    public DistanceSensor distanceSensorLeft;
    public DistanceSensor distanceSensorRight;

    // Update is called once per frame
    public float distance(){
        float centerDistance = distanceSensorCenter.currentDistance;
        float leftDistance = distanceSensorLeft.currentDistance;
        float rightDistance = distanceSensorRight.currentDistance;

        return Mathf.Min(Mathf.Min(centerDistance, leftDistance), rightDistance);
    }
}
