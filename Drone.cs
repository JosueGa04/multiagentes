using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneAgent : MonoBehaviour
{
    public List<GameObject> waypoints; // Puntos de la ruta
    public float Speed = 2f;
    public float rotationSpeed = 2f;
    private int index = 0;
    public float yRotationOffset = 90f; // Offset de rotación en Y

    // Comunicación
    public string messageToSend = ""; // Mensaje para el guardia
    public string messageReceived = ""; // Mensaje recibido del guardia

    private bool waitingAtLanding = false;

    void Start()
    {
        if (waypoints.Count == 0) Debug.LogWarning("No waypoints assigned.");
        Speed = Random.Range(1f, 5f);
    }

    void Update()
    {
        if (waypoints.Count == 0 || waitingAtLanding) return;

        MoveTowardsWaypoint();
        ProcessMessage();
    }

    void MoveTowardsWaypoint()
    {
        Vector3 target = waypoints[index].transform.position;
        transform.position = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);

        Vector3 direction = target - transform.position;
        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, yRotationOffset, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, target) <= 0.01f)
        {
            if (index == waypoints.Count - 1)
            {
                StartCoroutine(WaitAtLanding());
            }
            else
            {
                index = (index + 1) % waypoints.Count;
            }
        }
    }

    IEnumerator WaitAtLanding()
    {
        Debug.Log("DroneAgent: Waiting at landing zone for 10 seconds...");
        waitingAtLanding = true;

        // Notifica al guardia que ha llegado a la pista de aterrizaje
        messageToSend = "Drone: Arrived at landing zone.";

        yield return new WaitForSeconds(10);

        Debug.Log("DroneAgent: Restarting patrol.");
        index = 0; // Reinicia la ruta desde el primer waypoint
        waitingAtLanding = false;
    }

    void ProcessMessage()
    {
        if (!string.IsNullOrEmpty(messageReceived))
        {
            Debug.Log($"DroneAgent: Message from Guard - {messageReceived}");
            messageReceived = ""; // Reset message
        }

        // Notifica al guardia sobre la patrulla
        if (!waitingAtLanding)
        {
            messageToSend = $"DroneStatus: Patrolling at waypoint {index}";
        }
    }
}
