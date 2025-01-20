using UnityEngine;

public class GripperController : MonoBehaviour
{
    [Header("Riferimenti bracci pinza")]
    public Transform braccioSinistro;
    public Transform braccioDestro;
    public Transform robot;

    [Header("Parametri pinza")]
    public float velocitaMovimento = 1f;
    public float aperturaMassima = 2f;
    public float chiusuraMinima = 0.1f;

    [Header("Riferimenti sensore distanza")]
    public MixDistance mixDistance;
    public float targetDistance;
    public float distanceArm;

    private Transform oggettoPreso = null;

    public bool object_detected = false;
    private bool isGrabbing = false;

    public float rotationSpeed = 100f;
    private bool rotationActive = false;
    private bool isMovingForward = false;
    private bool isMovingBack = false;
    private bool relase = false;
    private bool flagTrigger = true;

    public static bool isGripperActive = false;


    enum GripperState
    {
        Idle,
        Grabbing,
        RotatingForward,
        MovingForward,
        Releasing,
        MovingBackward,
        RotatingBackward
    }

    GripperState currentState = GripperState.Idle;

    private Quaternion targetRotation;
    private float rotationProgress = 0f;
    public float moveSpeed = 5f;
    private Vector3 moveTarget;       

    void Update()
    {
        targetDistance = mixDistance.distance();
        isGripperActive = currentState != GripperState.Idle;

        switch (currentState)
        {
            case GripperState.Idle:
                flagTrigger = true;
                if (targetDistance < 0.25f && oggettoPreso) { 
                    currentState = GripperState.Grabbing; 
                }
                else
                    {
                        oggettoPreso = null;
                    }
                //Debug.Log(oggettoPreso);

                break;

            case GripperState.Grabbing:
                ControllaMovimentoPinza();
                break;

            case GripperState.RotatingForward:
                rotationActive = true;
                break;

            case GripperState.MovingForward:
                isMovingForward = true;
                break;

            case GripperState.Releasing:
                relase = true;
                break;

            case GripperState.MovingBackward:
                isMovingBack = true;
                break;

            case GripperState.RotatingBackward:
                rotationActive = true;
                break;
        }

        switch (currentState)
        {
            case GripperState.Grabbing:
                ControllaMovimentoPinza();
                if (!isGrabbing)
                {
                    currentState = GripperState.RotatingForward;
                }
                break;

            case GripperState.RotatingForward:
                Ruota(180f);
                if (!rotationActive) currentState = GripperState.MovingForward;
                IniziaMovimento(1f);
                break;

            case GripperState.MovingForward:

                MuoviAvanti(true);
                if (!isMovingForward) currentState = GripperState.Releasing;
                break;

            case GripperState.Releasing:
                RilasciaOggetto();

                if (!relase) currentState = GripperState.MovingBackward;
                IniziaMovimento(-1f);
                break;

            case GripperState.MovingBackward:
                MuoviAvanti(false);

                if (!isMovingBack) currentState = GripperState.RotatingBackward;
                break;

            case GripperState.RotatingBackward:

                Ruota(180f);
                if (!rotationActive) currentState = GripperState.Idle;
                break;
        }
    }

    void ControllaMovimentoPinza()
    {
        if (oggettoPreso == null)
            return;

        oggettoPreso.SetParent(transform);
        flagTrigger = false;
        isGrabbing = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Object") && flagTrigger)
        {
            oggettoPreso = other.transform;
            object_detected = true;

            string info = $"[OnTriggerEnter]\n" +
                          $"- Collider: {other}\n";
            //Debug.Log(info, other.gameObject);
        }
    }

 

    void Ruota(float angle)
    {
        if (rotationProgress == 0)
        {
            targetRotation = Quaternion.Euler(0, angle, 0) * robot.rotation;
        }

        rotationProgress += rotationSpeed * Time.fixedDeltaTime / 180f;

        robot.rotation = Quaternion.Slerp(robot.rotation, targetRotation, rotationProgress);

        if (rotationProgress >= 1.0f)
        {
            robot.rotation = targetRotation;
            rotationProgress = 0f;          

            rotationActive = false;
        }

    }
    void IniziaMovimento(float distanza)
    {
        isMovingForward = true;
        moveTarget = robot.position + robot.forward * distanza;
    }

    void MuoviAvanti(bool flag)
    {
        robot.position = Vector3.MoveTowards(robot.position, moveTarget, moveSpeed * Time.fixedDeltaTime);

        if (Vector3.Distance(robot.position, moveTarget) < 0.01f)
        {
            if (flag)
            {
                isMovingForward = false;
            }
            else
            {
                isMovingBack = false;
            }
        }
    }

    void RilasciaOggetto()
    {
        if (oggettoPreso != null)
        {
            oggettoPreso.SetParent(null);

            Rigidbody rb = oggettoPreso.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                Vector3 forceDirection = transform.forward;
                float forceMagnitude = 2f;
                rb.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse);
            }

            oggettoPreso = null;
            relase = false;

            //Debug.Log("Oggetto rilasciato e lanciato in avanti.");
        }
    }

}
