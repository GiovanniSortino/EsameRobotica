using UnityEngine;

public class RobotMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;

    private Rigidbody rb;

    void Start()
    {
        // Ottieni il componente Rigidbody
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Ricevi input dell'utente
        float moveInput = Input.GetAxis("Vertical"); 
        float turnInput = Input.GetAxis("Horizontal");
        // Movimento avanti/indietro
        Vector3 moveDirection = transform.forward * moveInput * moveSpeed;
        rb.MovePosition(rb.position + moveDirection * Time.fixedDeltaTime);

        // Rotazione del robot
        float rotation = turnInput * rotationSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0, rotation, 0);
        rb.MoveRotation(rb.rotation * turnRotation);
    }
}
