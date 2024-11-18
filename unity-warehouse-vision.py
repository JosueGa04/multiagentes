import numpy as np
from ultralytics import YOLO
import zmq
import json
from enum import Enum
import time
from dataclasses import dataclass, asdict
import cv2
from typing import List, Tuple, Dict, Optional

class ItemType(Enum):
    ELECTRONICS = "electronics"
    CLOTHING = "clothing"
    FOOD = "food"
    TOOLS = "tools"

    @classmethod
    def from_yolo_class(cls, yolo_class: str) -> Optional['ItemType']:
        # Mapeo de clases YOLO a tipos de items
        mapping = {
            "laptop": ItemType.ELECTRONICS,
            "cell phone": ItemType.ELECTRONICS,
            "keyboard": ItemType.ELECTRONICS,
            "clothes": ItemType.CLOTHING,
            "person": ItemType.CLOTHING,  # Para detectar maniquíes
            "bottle": ItemType.FOOD,
            "cup": ItemType.FOOD,
            "scissors": ItemType.TOOLS,
            "remote": ItemType.TOOLS
        }
        return mapping.get(yolo_class)

@dataclass
class RobotState:
    position: Tuple[float, float, float]  # x, y, z
    rotation: float  # rotación en el eje Y
    carrying_item: bool
    detected_item: Optional[str] = None
    detection_confidence: float = 0.0

@dataclass
class UnityMessage:
    robot_id: int
    state: RobotState
    message_type: str
    data: dict

class WarehouseVisionSystem:
    def __init__(self):
        # Inicializar YOLO
        self.model = YOLO('yolov8n.pt')  # usar modelo pre-entrenado
        
        # Configurar ZeroMQ
        self.context = zmq.Context()
        self.unity_socket = self.context.socket(zmq.PAIR)
        self.unity_socket.bind("tcp://*:5555")
        
        # Estado de los robots
        self.robots: Dict[int, RobotState] = {}
        
        # Umbrales de detección
        self.confidence_threshold = 0.6
        self.detection_range = 5.0  # metros en Unity
        
    def process_camera_frame(self, robot_id: int, frame: np.ndarray) -> Optional[ItemType]:
        """Procesa un frame de la cámara usando YOLO"""
        results = self.model(frame)
        
        # Obtener la detección con mayor confianza
        best_detection = None
        max_confidence = 0.0
        
        for result in results:
            for box in result.boxes:
                confidence = float(box.conf)
                if confidence > max_confidence and confidence > self.confidence_threshold:
                    class_id = int(box.cls)
                    class_name = self.model.names[class_id]
                    item_type = ItemType.from_yolo_class(class_name)
                    
                    if item_type is not None:
                        best_detection = (item_type, confidence, class_name)
                        max_confidence = confidence
        
        if best_detection:
            item_type, confidence, class_name = best_detection
            # Actualizar estado del robot
            if robot_id in self.robots:
                self.robots[robot_id].detected_item = class_name
                self.robots[robot_id].detection_confidence = confidence
            
            # Enviar mensaje a Unity
            self.send_detection_to_unity(robot_id, class_name, confidence)
            
            return item_type
        
        return None

    def send_detection_to_unity(self, robot_id: int, item_name: str, confidence: float):
        """Envía información de detección a Unity"""
        message = UnityMessage(
            robot_id=robot_id,
            state=self.robots[robot_id],
            message_type="DETECTION",
            data={
                "detected_item": item_name,
                "confidence": confidence,
                "timestamp": time.time()
            }
        )
        
        # Convertir a JSON y enviar
        self.unity_socket.send_json(asdict(message))

    def update_robot_state(self, robot_id: int, position: Tuple[float, float, float], 
                          rotation: float, carrying_item: bool):
        """Actualiza el estado de un robot"""
        self.robots[robot_id] = RobotState(
            position=position,
            rotation=rotation,
            carrying_item=carrying_item
        )

    def handle_unity_messages(self):
        """Maneja los mensajes entrantes de Unity"""
        try:
            while True:
                # Recibir mensaje de Unity (non-blocking)
                try:
                    message = self.unity_socket.recv_json(flags=zmq.NOBLOCK)
                except zmq.Again:
                    break
                
                # Procesar mensaje
                if message["type"] == "ROBOT_UPDATE":
                    robot_id = message["robot_id"]
                    self.update_robot_state(
                        robot_id,
                        tuple(message["position"]),
                        message["rotation"],
                        message["carrying_item"]
                    )
                elif message["type"] == "CAMERA_FRAME":
                    robot_id = message["robot_id"]
                    # Decodificar frame de la cámara
                    frame = np.frombuffer(message["frame"], dtype=np.uint8)
                    frame = cv2.imdecode(frame, cv2.IMREAD_COLOR)
                    # Procesar frame
                    self.process_camera_frame(robot_id, frame)
        
        except Exception as e:
            print(f"Error handling Unity messages: {e}")

    def run(self):
        """Bucle principal del sistema de visión"""
        print("Iniciando sistema de visión del almacén...")
        
        try:
            while True:
                # Manejar mensajes de Unity
                self.handle_unity_messages()
                
                # Pequeña pausa para no saturar el CPU
                time.sleep(0.01)
                
        except KeyboardInterrupt:
            print("Cerrando sistema de visión...")
        finally:
            self.unity_socket.close()
            self.context.term()

# Script de ejemplo para ejecutar el sistema
if __name__ == "__main__":
    vision_system = WarehouseVisionSystem()
    vision_system.run()
