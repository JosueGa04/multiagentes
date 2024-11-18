using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

[Serializable]
public class DetectionMessage
{
    public string detected_item;
    public float confidence;
    public double timestamp;
}

[Serializable]
public class UnityMessage
{
    public int robot_id;
    public string type;
    public Dictionary<string, object> data;
}

public class RobotVisionController : MonoBehaviour
{
    private PairSocket socket;
    private Camera robotCamera;
    private int robotId;
    private TextMesh detectionText;
    private float lastDetectionTime;
    private float detectionDisplayDuration = 3f;

    void Start()
    {
        // Configurar ZeroMQ
        AsyncIO.ForceDotNet.Force();
        socket = new PairSocket();
        socket.Connect("tcp://localhost:5555");

        // Obtener componentes
        robotCamera = GetComponentInChildren<Camera>();
        robotId = GetComponent<RobotController>().RobotId;

        // Crear texto flotante para detecciones
        CreateDetectionText();

        // Iniciar envío de frames
        StartCoroutine(SendCameraFrames());
    }

    void CreateDetectionText()
    {
        GameObject textObj = new GameObject("DetectionText");
        textObj.transform.parent = transform;
        textObj.transform.localPosition = new Vector3(0, 2, 0);
        
        detectionText = textObj.AddComponent<TextMesh>();
        detectionText.alignment = TextAlignment.Center;
        detectionText.anchor = TextAnchor.MiddleCenter;
        detectionText.fontSize = 24;
        detectionText.color = Color.white;
        detectionText.gameObject.SetActive(false);
    }

    IEnumerator SendCameraFrames()
    {
        while (true)
        {
            // Capturar frame de la cámara
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = robotCamera.targetTexture;
            
            Texture2D tex = new Texture2D(robotCamera.targetTexture.width, 
                                        robotCamera.targetTexture.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            
            RenderTexture.active = currentRT;

            // Convertir a bytes y enviar
            byte[] bytes = tex.EncodeToPNG();
            Destroy(tex);

            var message = new UnityMessage
            {
                robot_id = robotId,
                type = "CAMERA_FRAME",
                data = new Dictionary<string, object>
                {
                    { "frame", Convert.ToBase64String(bytes) },
                    { "timestamp", DateTime.UtcNow.Ticks }
                }
            };

            string json = JsonConvert.SerializeObject(message);
            socket.SendFrame(json);

            yield return new WaitForSeconds(0.1f); // 10 FPS
        }
    }

    void Update()
    {
        // Recibir mensajes del sistema de visión
        if (socket.TryReceiveFrameString(out string jsonMessage))
        {
            var message = JsonConvert.DeserializeObject<UnityMessage>(jsonMessage);
            
            if (message.type == "DETECTION" && message.robot_id == robotId)
            {
                var detection = JsonConvert.DeserializeObject<DetectionMessage>(
                    JsonConvert.SerializeObject(message.data));
                
                // Mostrar detección
                ShowDetection(detection.detected_item, detection.confidence);
            }
        }

        // Ocultar texto de detección después del tiempo establecido
        if (Time.time - lastDetectionTime > detectionDisplayDuration)
        {
            detectionText.gameObject.SetActive(false);
        }
    }

    void ShowDetection(string item, float confidence)
    {
        detectionText.text = $"¡{item}!\n{confidence:P0}";
        detectionText.gameObject.SetActive(true);
        lastDetectionTime = Time.time;
    }

    void OnDestroy()
    {
        socket.Close();
        NetMQConfig.Cleanup();
    }
}
