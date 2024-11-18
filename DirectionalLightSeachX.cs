using UnityEngine;


public class DirectionalLightSearchX : MonoBehaviour
{
    public float searchSpeed = 1f; // Velocidad del movimiento de búsqueda
    public float searchAngle = 10f; // Ángulo de oscilación del movimiento de búsqueda en el eje X


    private float initialXRotation; // Rotación inicial en el eje X
    private float initialYRotation; // Rotación inicial en el eje Y
    private float initialZRotation; // Rotación inicial en el eje Z


    void Start()
    {
        // Guardar la rotación inicial en X, Y y Z
        initialXRotation = transform.localEulerAngles.x;
        initialYRotation = transform.localEulerAngles.y;
        initialZRotation = transform.localEulerAngles.z;
    }


    void Update()
    {
        PerformSearchMovement();
    }


    // Función para realizar el movimiento de búsqueda en el eje X alrededor de la rotación inicial
    private void PerformSearchMovement()
    {
        // Calcular el ángulo de oscilación en el eje X usando el tiempo para un movimiento de búsqueda
        float angleX = Mathf.Sin(Time.time * searchSpeed) * searchAngle;


        // Aplicar la rotación oscilante en el eje X, manteniendo las rotaciones iniciales en Y y Z
        transform.localRotation = Quaternion.Euler(initialXRotation + angleX, initialYRotation, initialZRotation);
    }
}
