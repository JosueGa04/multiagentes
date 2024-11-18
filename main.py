import numpy as np
import pygame
from enum import Enum
import math
from typing import List, Tuple, Dict
import random
from dataclasses import dataclass
import time

# Definición de la ontología básica
class ItemType(Enum):
    ELECTRONICS = "electronics"
    CLOTHING = "clothing"
    FOOD = "food"
    TOOLS = "tools"

@dataclass
class Item:
    item_type: ItemType
    id: int
    position: Tuple[int, int]

@dataclass
class Stack:
    position: Tuple[int, int]
    items: List[Item]
    item_type: ItemType
    
    def is_full(self):
        return len(self.items) >= 5

class Direction(Enum):
    NORTH = (0, -1)
    SOUTH = (0, 1)
    EAST = (1, 0)
    WEST = (-1, 0)

class RobotState(Enum):
    IDLE = "idle"
    SEEKING = "seeking"
    CARRYING = "carrying"
    STACKING = "stacking"

class Robot:
    def __init__(self, x: int, y: int, robot_id: int):
        self.position = np.array([x, y], dtype=float)
        self.direction = Direction.NORTH
        self.state = RobotState.IDLE
        self.carried_item = None
        self.target = None
        self.id = robot_id
        self.radius = 20
        self.speed = 2.0
        self.rotation_speed = math.pi / 16
        self.angle = 0
        self.detection_radius = 60
        # Agregar contador de movimientos
        self.movement_count = 0
        self.last_position = np.array([x, y], dtype=float)

    def get_steering_force(self, warehouse):
        if self.target is None:
            return np.array([0.0, 0.0])
        
        # Fuerza base hacia el objetivo
        desired_velocity = np.array(self.target) - self.position
        distance = np.linalg.norm(desired_velocity)
        if distance > 0:
            desired_velocity = (desired_velocity / distance) * self.speed
            
        # Calcular fuerzas de separación de otros robots
        separation = np.array([0.0, 0.0])
        neighbors = 0
        
        for robot in warehouse.robots:
            if robot.id != self.id:
                distance = np.linalg.norm(self.position - robot.position)
                if distance < self.detection_radius:
                    # Vector de repulsión
                    repulsion = self.position - robot.position
                    if distance > 0:
                        # La fuerza es más fuerte cuando más cerca están
                        repulsion = repulsion / (distance * distance)
                        separation += repulsion
                        neighbors += 1
        
        # Normalizar y aplicar la fuerza de separación
        if neighbors > 0:
            separation = separation / neighbors
            mag = np.linalg.norm(separation)
            if mag > 0:
                separation = (separation / mag) * self.speed
        
        # Combinar fuerzas
        steering_force = desired_velocity
        if neighbors > 0:
            steering_force = 0.6 * desired_velocity + 0.4 * separation
            
            # Normalizar la fuerza resultante
            mag = np.linalg.norm(steering_force)
            if mag > 0:
                steering_force = (steering_force / mag) * self.speed
                
        return steering_force

    def move(self, warehouse):
        if self.target is None:
            return False
            
        # Obtener la fuerza de dirección
        steering = self.get_steering_force(warehouse)
        
        # Actualizar posición
        self.position += steering
        
        # Verificar si hubo movimiento significativo
        if np.linalg.norm(self.position - self.last_position) > 0.1:  # Umbral de movimiento
            self.movement_count += 1
            self.last_position = self.position.copy()
        
        # Mantener dentro de los límites del almacén
        self.position[0] = np.clip(self.position[0], self.radius, warehouse.width - self.radius)
        self.position[1] = np.clip(self.position[1], self.radius, warehouse.height - self.radius)
        
        # Actualizar ángulo de orientación
        if np.linalg.norm(steering) > 0:
            target_angle = math.atan2(steering[1], steering[0])
            angle_diff = (target_angle - self.angle + math.pi) % (2 * math.pi) - math.pi
            self.angle += np.sign(angle_diff) * min(self.rotation_speed, abs(angle_diff))
        
        # Comprobar si ha llegado al objetivo
        distance_to_target = np.linalg.norm(np.array(self.target) - self.position)
        return distance_to_target < self.radius

class Warehouse:
    def __init__(self, width: int, height: int, num_robots: int):
        self.width = width
        self.height = height
        self.robots = [Robot(random.randint(0, width), random.randint(0, height), i) 
                      for i in range(num_robots)]
        self.items = []
        self.stacks = {}
        self.initialize_items()
        self.initialize_stacks()
        # Agregar tiempo inicial
        self.start_time = time.time()
        self.end_time = None
        self.task_completed = False
        
    def initialize_items(self):
        items_per_type = 5  # 5 items por cada tipo
        item_id = 0
        
        # Crear items para cada tipo
        for item_type in ItemType:
            for _ in range(items_per_type):
                # Asegurarse que los items no aparezcan muy cerca de los bordes
                margin = 50
                x = random.randint(margin, self.width - margin)
                y = random.randint(margin, self.height - margin)
                
                self.items.append(Item(item_type, item_id, (x, y)))
                item_id += 1
            
    def initialize_stacks(self):
        margin = 70
        stack_positions = [
            (margin, margin),
            (self.width - margin, margin),
            (margin, self.height - margin),
            (self.width - margin, self.height - margin)
        ]
        for i, item_type in enumerate(ItemType):
            self.stacks[item_type] = Stack(stack_positions[i], [], item_type)
    
    def is_task_completed(self):
        return len(self.items) == 0 and not any(robot.carried_item for robot in self.robots)
    
    def update(self):
        for robot in self.robots:
            if robot.state == RobotState.IDLE:
                available_items = [item for item in self.items 
                                if not any(r.carried_item == item for r in self.robots)]
                if available_items:
                    target_item = min(available_items, 
                                    key=lambda x: np.linalg.norm(np.array(x.position) - robot.position))
                    robot.target = target_item.position
                    robot.state = RobotState.SEEKING
                    
            elif robot.state == RobotState.SEEKING:
                reached = robot.move(self)
                if reached:
                    target_items = [item for item in self.items 
                                  if item.position == robot.target and 
                                  not any(r.carried_item == item for r in self.robots)]
                    if target_items:
                        robot.carried_item = target_items[0]
                        robot.state = RobotState.CARRYING
                        robot.target = self.stacks[robot.carried_item.item_type].position
                    else:
                        robot.state = RobotState.IDLE
                        robot.target = None
                    
            elif robot.state == RobotState.CARRYING:
                reached = robot.move(self)
                if reached:
                    stack = self.stacks[robot.carried_item.item_type]
                    if not stack.is_full():
                        stack.items.append(robot.carried_item)
                        self.items.remove(robot.carried_item)
                        robot.carried_item = None
                        robot.target = None
                        robot.state = RobotState.IDLE
                    else:
                        robot.state = RobotState.IDLE
                        robot.target = None
        
        # Verificar si la tarea está completa
        if not self.task_completed and self.is_task_completed():
            self.task_completed = True
            self.end_time = time.time()

class WarehouseSimulation:
    def __init__(self, width=800, height=600):
        pygame.init()
        self.width = width
        self.height = height
        self.screen = pygame.display.set_mode((width, height))
        self.clock = pygame.time.Clock()
        self.warehouse = Warehouse(width, height, 5)
        self.colors = {
            ItemType.ELECTRONICS: (255, 0, 0),
            ItemType.CLOTHING: (0, 255, 0),
            ItemType.FOOD: (0, 0, 255),
            ItemType.TOOLS: (255, 255, 0)
        }
        self.font = pygame.font.Font(None, 36)
        
    def draw(self):
        self.screen.fill((255, 255, 255))
        
        # Dibujar elementos existentes
        for stack in self.warehouse.stacks.values():
            pygame.draw.rect(self.screen, self.colors[stack.item_type],
                           (stack.position[0]-25, stack.position[1]-25, 50, 50), 2)
            
        for item in self.warehouse.items:
            pygame.draw.circle(self.screen, self.colors[item.item_type],
                             (int(item.position[0]), int(item.position[1])), 5)
            
        for robot in self.warehouse.robots:
            pygame.draw.circle(self.screen, (100, 100, 100),
                             (int(robot.position[0]), int(robot.position[1])), 
                             robot.radius)
            
            direction_end = robot.position + np.array([
                math.cos(robot.angle),
                math.sin(robot.angle)
            ]) * robot.radius
            pygame.draw.line(self.screen, (0, 0, 0),
                           (int(robot.position[0]), int(robot.position[1])),
                           (int(direction_end[0]), int(direction_end[1])), 2)
            
            if robot.carried_item:
                pygame.draw.circle(self.screen, 
                                 self.colors[robot.carried_item.item_type],
                                 (int(robot.position[0]), int(robot.position[1])-5), 5)
        
        # Mostrar tiempo transcurrido y movimientos
        current_time = time.time() - self.warehouse.start_time
        if self.warehouse.task_completed:
            current_time = self.warehouse.end_time - self.warehouse.start_time
            
        time_text = f"Tiempo: {current_time:.1f}s"
        time_surface = self.font.render(time_text, True, (0, 0, 0))
        self.screen.blit(time_surface, (10, 10))
        
        # Mostrar movimientos de cada robot
        for i, robot in enumerate(self.warehouse.robots):
            moves_text = f"Robot {robot.id}: {robot.movement_count} movimientos"
            moves_surface = self.font.render(moves_text, True, (0, 0, 0))
            self.screen.blit(moves_surface, (10, 50 + i * 30))
        
        pygame.display.flip()
        
    def run(self):
        running = True
        while running:
            for event in pygame.event.get():
                if event.type == pygame.QUIT:
                    running = False
                    
            self.warehouse.update()
            self.draw()
            self.clock.tick(60)
            
        pygame.quit()

if __name__ == "__main__":
    simulation = WarehouseSimulation()
    simulation.run()