using UnityEngine;

public class GuardAgent : MonoBehaviour
{
    public string droneMessage = ""; // Mensaje recibido del drone
    public string cameraMessage = ""; // Mensaje recibido de las cámaras

    public string messageToDrone = ""; // Respuesta al drone
    public string messageToCamera = ""; // Respuesta a las cámaras

    void Update()
    {
        ProcessMessages();
    }

    void ProcessMessages()
    {
        if (!string.IsNullOrEmpty(droneMessage))
        {
            Debug.Log($"GuardAgent: Received from Drone - {droneMessage}");
            // Responde al drone dependiendo de su estado
            if (droneMessage.Contains("landing zone"))
            {
                messageToDrone = "Guard: Confirmed landing.";
            }
            else
            {
                messageToDrone = "Guard: Continue patrol.";
            }
            droneMessage = ""; // Reset message
        }

        if (!string.IsNullOrEmpty(cameraMessage))
        {
            Debug.Log($"GuardAgent: Received from Camera - {cameraMessage}");
            // Responde a las cámaras en función de la alerta
            messageToCamera = "Guard: Investigating camera alert.";
            cameraMessage = ""; // Reset message
        }
    }
}
