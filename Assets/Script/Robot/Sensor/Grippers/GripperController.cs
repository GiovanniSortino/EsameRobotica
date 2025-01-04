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
    public DistanceSensor distanceSensor;
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


    private Quaternion targetRotation; // Rotazione target
    private float rotationProgress = 0f;
    public float moveSpeed = 5f;
    private Vector3 moveTarget;           // Destinazione per il movimento

    void Update()
    {
        targetDistance = distanceSensor.currentDistance;

        switch (currentState)
        {
            case GripperState.Idle:
                if (targetDistance < 0.25f) currentState = GripperState.Grabbing;
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
    }


    void FixedUpdate()
    {
        switch (currentState)
        {
            case GripperState.Grabbing:
                ControllaMovimentoPinza();
                if (!isGrabbing) currentState = GripperState.RotatingForward;
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
        // Fissa l'oggetto alla pinza
        oggettoPreso.SetParent(transform);

        // Disabilita la fisica per impedire il movimento
        Rigidbody rb = oggettoPreso.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Disabilita la fisica
        }

        Vector3 posSinistro = braccioSinistro.localPosition;
        Vector3 posDestro = braccioDestro.localPosition;

        // Controlla se targetDistance � oltre i limiti
        targetDistance = Mathf.Clamp(targetDistance, chiusuraMinima, aperturaMassima);

        // Movimento graduale delle braccia
        posSinistro.z = Mathf.MoveTowards(posSinistro.z, -targetDistance, velocitaMovimento * Time.deltaTime);
        posDestro.z = Mathf.MoveTowards(posDestro.z, targetDistance, velocitaMovimento * Time.deltaTime);

        // Imposta le posizioni limitate
        braccioSinistro.localPosition = posSinistro;
        braccioDestro.localPosition = posDestro;
        isGrabbing = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Object"))
        {
            oggettoPreso = other.transform;
            object_detected = true;
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (!isGrabbing && oggettoPreso != null && other.transform == oggettoPreso)
        {
            object_detected = false;
        }
    }

    void Ruota(float angle)
    {
        if (rotationProgress == 0) // Calcola il target solo all'inizio
        {
            targetRotation = Quaternion.Euler(0, angle, 0) * robot.rotation;
        }

        // Incrementa la progressione
        rotationProgress += rotationSpeed * Time.fixedDeltaTime / 180f;

        // Interpolazione graduale
        robot.rotation = Quaternion.Slerp(robot.rotation, targetRotation, rotationProgress);

        // Controlla se ha raggiunto la rotazione finale
        if (rotationProgress >= 1.0f)
        {
            robot.rotation = targetRotation; // Forza l'angolo finale
            rotationProgress = 0f;          // Resetta per rotazioni future
                                       
            rotationActive = false;
            
        }

    }
    void IniziaMovimento(float distanza)
    {
       
        isMovingForward = true; // Attiva lo stato di movimento
        moveTarget = robot.position + robot.forward * distanza; // Calcola la destinazione

    }

    void MuoviAvanti(bool flag)
    {
        // Muove il robot gradualmente verso il target
        robot.position = Vector3.MoveTowards(robot.position, moveTarget, moveSpeed * Time.fixedDeltaTime);

        // Controlla se ha raggiunto la posizione desiderata
        if (Vector3.Distance(robot.position, moveTarget) < 0.01f) // Precisione
        {
            if (flag)
            {
                isMovingForward = false; // Ferma il movimento
            }
            else
            {
                isMovingBack = false;
            }
        }
    }


    void RilasciaOggetto()
    {
        if (oggettoPreso != null) // Controlla se c'� un oggetto agganciato
        {
            oggettoPreso.SetParent(null); // Scollega l'oggetto dalla pinza
            Rigidbody rb = oggettoPreso.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false; // Riabilita la fisica
            }
            oggettoPreso = null; // Resetta l'oggetto preso
            relase = false;
        }
    }
}