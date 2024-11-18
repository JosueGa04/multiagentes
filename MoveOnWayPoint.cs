using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MoveOnWayPoint : MonoBehaviour
{
    public List<GameObject> waypoints; // Lista de puntos de la ruta
    public GameObject itemPrefab; // Prefab del objeto a soltar
    public float Speed = 2;
    public float rotationSpeed = 2f; // Velocidad de rotación para el drone en todos los ejes
    public Camera fixedCamera; // Cámara que sigue al drone y siempre ve hacia abajo con movimiento de búsqueda
    public Camera dynamicCamera; // Cámara que observa el siguiente waypoint
    public float cameraSmoothSpeed = 2f; // Velocidad de suavizado de la cámara dinámica
    public float yRotationOffset = 0f; // Ajuste de rotación en Y para el frente del drone
    public Light directionalLight; // Luz direccional que también realizará el movimiento de búsqueda


    // Parámetros para el movimiento de búsqueda
    public float searchSpeed = 1f; // Velocidad del movimiento de búsqueda
    public float searchAngle = 30f; // Ángulo de oscilación del movimiento de búsqueda


    private int index = 0;
    private bool droppingItems = false; // Flag para saber si está en proceso de soltar objetos
    private int itemsCollected = 0; // Contador de objetos recogidos
    private bool returning = false; // Flag para saber si el drone está regresando a través de la ruta


    private HashSet<GameObject> collectedWaypoints = new HashSet<GameObject>(); // Conjunto de waypoints recogidos


    void Start()
    {
        // Asignar una velocidad aleatoria entre 1 y 5 al inicio
        Speed = Random.Range(1f, 5f);


        // Asegurar que el drone y la cámara inician con rotación cero
        transform.rotation = Quaternion.identity;


        if (dynamicCamera != null)
        {
            dynamicCamera.transform.localRotation = Quaternion.identity;
        }
    }


    void Update()
    {
        if (waypoints.Count == 0) return;


        // Actualizar la cámara dinámica y el drone para observar el siguiente waypoint con suavidad
        UpdateRotationTowardsNextWaypoint();


        // Movimiento de búsqueda para la cámara fija y la luz direccional
        PerformSearchMovement();


        // Detener el movimiento cuando esté en el último waypoint y esté soltando los objetos
        if (index == waypoints.Count - 1 && droppingItems) return;


        // Verificar si el waypoint actual no es nulo y no ha sido recogido
        if (waypoints[index] == null || collectedWaypoints.Contains(waypoints[index]))
        {
            if (returning && index > 0)
            {
                index--; // Retroceder al waypoint anterior si está regresando
            }
            else
            {
                index++; // Avanzar al siguiente waypoint si está yendo hacia adelante
            }
            return;
        }


        Vector3 destination = waypoints[index].transform.position;


        // Movimiento hacia el siguiente waypoint
        Vector3 newPos = Vector3.MoveTowards(transform.position, destination, Speed * Time.deltaTime);
        transform.position = newPos;


        // Verificar si llegó al siguiente waypoint
        float distance = Vector3.Distance(transform.position, destination);
        if (distance <= 0.01f)
        {
            if (index == waypoints.Count - 1 && !returning)
            {
                // Cuando llegue al último waypoint por primera vez, comenzar a soltar objetos
                Debug.Log("Llegó al último waypoint. Comenzando a soltar objetos.");
                droppingItems = true;
                StartCoroutine(DropItemsOneByOne());
            }
            else if (returning && index == 0)
            {
                // Cuando regrese al primer waypoint, detenerse
                Debug.Log("Regresó al primer waypoint.");
                returning = false;
            }
            else
            {
                // Cambiar el índice en función de si está regresando o avanzando
                if (returning)
                {
                    index--;
                }
                else
                {
                    index++;
                }
            }
        }
    }


    // Detecta cuando el objeto entra en contacto con el objeto a recoger
    private void OnTriggerEnter(Collider other)
    {
        PickableObject pickable = other.GetComponent<PickableObject>();
        if (pickable != null)
        {
            PickUpItem(pickable);
        }
    }


    // Función para recoger el objeto
    void PickUpItem(PickableObject pickable)
    {
        // Incrementa el contador de objetos recogidos
        itemsCollected++;


        // Marcar el objeto como recogido en lugar de eliminarlo de la lista
        collectedWaypoints.Add(pickable.gameObject);


        // Destruir el objeto después de recogerlo
        Destroy(pickable.gameObject);
        Debug.Log("Objeto recogido y destruido.");
    }


    // Coroutine para soltar los objetos en el último waypoint
    IEnumerator DropItemsOneByOne()
    {
        for (int i = 0; i < itemsCollected; i++)
        {
            // Crear una instancia del objeto a soltar en la posición actual
            GameObject item = Instantiate(itemPrefab, transform.position + Vector3.down * 1.0f, Quaternion.identity);


            // Añadir un Rigidbody para aplicar gravedad
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = item.AddComponent<Rigidbody>();
            }
            rb.useGravity = true;


            Debug.Log("Objeto soltado en posición: " + item.transform.position);


            // Espera un momento antes de soltar el siguiente objeto
            yield return new WaitForSeconds(0.5f);
        }


        // Restablecer el flag y contador después de soltar los objetos
        droppingItems = false;
        itemsCollected = 0;


        // Iniciar el regreso al primer waypoint
        returning = true;
    }


    // Actualizar la rotación del drone y la cámara dinámica para observar el siguiente waypoint con suavidad
    private void UpdateRotationTowardsNextWaypoint()
    {
        if (index < waypoints.Count && waypoints[index] != null)
        {
            // Calcular la dirección al siguiente waypoint
            Vector3 targetDirection = waypoints[index].transform.position - transform.position;


            // Rotación para el drone con el offset en el eje Y
            Quaternion droneTargetRotation = Quaternion.LookRotation(targetDirection) * Quaternion.Euler(0, yRotationOffset, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, droneTargetRotation, rotationSpeed * Time.deltaTime);


            // Rotación para la cámara dinámica sin el offset
            Quaternion cameraTargetRotation = Quaternion.LookRotation(targetDirection);
            dynamicCamera.transform.rotation = Quaternion.Slerp(dynamicCamera.transform.rotation, cameraTargetRotation, cameraSmoothSpeed * Time.deltaTime);
        }
    }


    // Función para realizar el movimiento de búsqueda en la cámara fija y la luz direccional
    private void PerformSearchMovement()
    {
        // Calcular el ángulo de oscilación usando el tiempo para crear un movimiento de "búsqueda"
        float angle = Mathf.Sin(Time.time * searchSpeed) * searchAngle;


        // Aplicar el ángulo de oscilación en el eje Y mientras mantiene la inclinación hacia abajo en el eje X
        if (fixedCamera != null)
        {
            fixedCamera.transform.localRotation = Quaternion.Euler(89, angle, 0); // Mantiene 89 en el eje X
        }


        if (directionalLight != null)
        {
            directionalLight.transform.localRotation = Quaternion.Euler(89, angle, 0); // Mantiene 89 en el eje X
        }
    }
}
