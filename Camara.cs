using UnityEngine;

public class CameraAgent : MonoBehaviour
{
    public float rotationAmplitude = 45f; // Amplitud de rotación
    public float rotationSpeed = 2f;

    public string messageToSend = ""; // Mensaje para el guardia
    public string messageReceived = ""; // Mensaje recibido del guardia

    private float initialYRotation;

    void Start()
    {
        initialYRotation = transform.rotation.eulerAngles.y;
    }

    void Update()
    {
        PatrolArea();
        ProcessMessage();
    }

    void PatrolArea()
    {
        float offsetY = Mathf.Sin(Time.time * rotationSpeed) * rotationAmplitude;
        transform.rotation = Quaternion.Euler(0f, initialYRotation + offsetY, 0f);

        // Simular detección de evento
        if (Random.Range(0, 100) < 1) // 1% de probabilidad por frame
        {
            messageToSend = "CameraAlert: Suspicious activity detected.";
        }
    }

    void ProcessMessage()
    {
        if (!string.IsNullOrEmpty(messageReceived))
        {
            Debug.Log($"CameraAgent: Message from Guard - {messageReceived}");
            messageReceived = ""; // Reset message
        }
    }
}
