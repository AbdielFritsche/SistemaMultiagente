from flask import Flask, request, jsonify

import heapq

class AStar:
    def __init__(self, grid):
        self.grid = grid  # Matriz de la cuadrícula
        self.rows = len(grid)
        self.cols = len(grid[0])

    # Método para calcular la heurística de Manhattan
    def heuristic(self, a, b):
        return abs(a[0] - b[0]) + abs(a[1] - b[1])

    # Método para buscar la ruta
    def search(self, start, goal):
        open_set = []
        heapq.heappush(open_set, (0, start))
        came_from = {}
        g_score = {start: 0}
        f_score = {start: self.heuristic(start, goal)}

        while open_set:
            _, current = heapq.heappop(open_set)

            # Si se alcanza el objetivo
            if current == goal:
                return self.reconstruct_path(came_from, current)

            neighbors = self.get_neighbors(current)
            for neighbor in neighbors:
                tentative_g_score = g_score[current] + 1

                if tentative_g_score < g_score.get(neighbor, float('inf')):
                    came_from[neighbor] = current
                    g_score[neighbor] = tentative_g_score
                    f_score[neighbor] = tentative_g_score + self.heuristic(neighbor, goal)
                    if neighbor not in [i[1] for i in open_set]:
                        heapq.heappush(open_set, (f_score[neighbor], neighbor))

        return []  # Retorna una lista vacía si no hay camino

    # Método para reconstruir el camino desde 'came_from'
    def reconstruct_path(self, came_from, current):
        path = [current]
        while current in came_from:
            current = came_from[current]
            path.append(current)
        path.reverse()
        return path

    # Método para obtener los vecinos válidos de una celda
    def get_neighbors(self, pos):
        neighbors = []
        for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:  # Movimientos: arriba, abajo, izquierda, derecha
            nx, ny = pos[0] + dx, pos[1] + dy
            if 0 <= nx < self.rows and 0 <= ny < self.cols and self.grid[nx][ny] == 0:
                neighbors.append((nx, ny))
        return neighbors

import agentpy as ap
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import random
import json
import os

# Parámetros de Q-learning
alpha = 0.1
gamma = 0.9
epsilon = 0.2

states_car = [
    "green_light_clear",
    "green_light_car_ahead",
    "green_light_car_crossing",
    "yellow_light_far",
    "yellow_light_near",
    "red_light"
]
actions_car = ["continue", "slow_down", "stop"]

states_person = ["red_light_car_far", "red_light_car_near", "green_light"]
actions_person = ["wait", "cross"]

# Inicializar tablas Q
q_table_car = np.random.uniform(low=-0.1, high=0.1, size=(len(states_car), len(actions_car)))
q_table_person = np.random.uniform(low=-0.1, high=0.1, size=(len(states_person), len(actions_person)))

movement_log = []
class TrafficLight(ap.Agent):
    def setup(self):
        self.state = "red"
        self.timer = 0
        self.yellow_duration = 3
        self.cycle_duration = 30

    def update_state(self):
        self.timer += 1
        if self.timer % self.cycle_duration == 0:
            self.state = "yellow" if self.state == "green" else "red"
            self.timer = 0
        elif self.timer % self.cycle_duration == self.yellow_duration and self.state == "yellow":
            self.state = "red"
        elif self.timer % (self.cycle_duration // 2) == 0 and self.state == "red":
            self.state = "green"
        return self.state

class CarAgent(ap.Agent):
    def setup(self, start_pos, direction, lane):
        self.x, self.y = start_pos
        self.direction = direction
        self.lane = lane
        self.speed = 1.0
        self.waiting = False
        self.path = [(self.x, self.y)]
        self.color = np.random.rand(3)

    def get_state(self, traffic_light, cars, pedestrians):
        if traffic_light.state == "red":
            return "red_light"
        elif traffic_light.state == "yellow":
            dist_to_intersection = self.distance_to_intersection()
            return "yellow_light_near" if dist_to_intersection < 3 else "yellow_light_far"
        else:
            for car in cars:
                if car is not self and self.is_car_ahead(car):
                    if self.distance_to_car(car) < 2:
                        return "green_light_car_ahead"
                    elif self.is_car_crossing(car):
                        return "green_light_car_crossing"
            return "green_light_clear"

    def distance_to_intersection(self):
        if self.direction in ['right', 'left']:
            return abs(5 - self.x)
        else:
            return abs(5 - self.y)

    def is_car_ahead(self, other_car):
        if self.direction == 'right':
            return other_car.x > self.x and abs(other_car.y - self.y) < 0.5
        elif self.direction == 'left':
            return other_car.x < self.x and abs(other_car.y - self.y) < 0.5
        elif self.direction == 'up':
            return other_car.y > self.y and abs(other_car.x - self.x) < 0.5
        else:
            return other_car.y < self.y and abs(other_car.x - self.x) < 0.5

    def distance_to_car(self, other_car):
        return np.sqrt((self.x - other_car.x)**2 + (self.y - other_car.y)**2)

    def is_car_in_intersection(self, other_car):
        intersection_box = {'x': (4, 6), 'y': (4, 6)}
        return (intersection_box['x'][0] <= other_car.x <= intersection_box['x'][1] and
                intersection_box['y'][0] <= other_car.y <= intersection_box['y'][1])

    def is_car_crossing(self, other_car):
        intersection_box = {'x': (4, 6), 'y': (4, 6)}
        return (intersection_box['x'][0] <= other_car.x <= intersection_box['x'][1] and
                intersection_box['y'][0] <= other_car.y <= intersection_box['y'][1])

    def move(self, traffic_light, cars, pedestrians):
        # Obtener el estado actual del coche
        state = self.get_state(traffic_light, cars, pedestrians)
        state_index = states_car.index(state)
        
        # Elegir la acción usando la política epsilon-greedy
        action_index = np.argmax(q_table_car[state_index]) if random.random() > epsilon else random.randint(0, len(actions_car)-1)
        action = actions_car[action_index]

        # Registrar el movimiento antes de mover
        log_entry = f"Car {self.id} at coordinates ({self.x}, {self.y}) in state '{state}' decided to '{action}'."
        log_entry = ["carro",self.x,self.y,state,action]
        print(log_entry)  # Si deseas verlo en la consola también

        #self.id = "2"  # Reemplaza con el ID específico
        #log_entry = "Car 2 at coordinates (10, 3) in state 'red_light' decided to 'stop'."  # Ejemplo de log_entry


        # Verificar si el archivo existe; si no, crearlo vacío
        if not os.path.exists("simulacion.json"):
            with open("simulacion.json", "w") as file:
                json.dump({}, file)

        # Cargar el archivo JSON (manejar archivo vacío)
        try:
            with open("simulacion.json", "r") as file:
                content = file.read().strip()
                movement_logs = json.loads(content) if content else {}
        except json.JSONDecodeError:
            movement_logs = {}  # Si hay un error, inicializar como diccionario vacío

        # Actualizar el JSON agregando el nuevo log_entry a la lista del ID correspondiente
        
        # Actualizar el JSON agregando el nuevo log_entry
        car_id = str(self.id)
        if car_id not in movement_logs:
            movement_logs[car_id] = []  # Crear una lista si no existe
        movement_logs[car_id].append(log_entry)  # Agregar el nuevo log_entry

        
        # Guardar los cambios en el archivo JSON
        with open("simulacion.json", "w") as file:
            json.dump(movement_logs, file, indent=4)
        
        print(self.id, movement_logs[str(self.id)])
        # Modificar la velocidad dependiendo de la acción
        self.speed = 1.0 if action == "continue" else 0.5 if action == "slow_down" else 0.0
        
        # Si el coche no está esperando, moverlo según su dirección
        if not self.waiting:
            # Revisar si hay otros coches en la intersección y detenerse si es necesario
            for car in cars:
                if car is not self and self.is_car_in_intersection(car):
                    self.speed = 0.0  # Si el coche está en la intersección, detenerlo
                    break

            # Actualizar la posición del coche según su dirección
            if self.direction == 'right':
                self.x += self.speed
            elif self.direction == 'left':
                self.x -= self.speed
            elif self.direction == 'up':
                self.y += self.speed
            else:
                self.y -= self.speed

        # Loguear el movimiento del coche (agente) con sus nuevas coordenadas
        print(f"Car {self} moved to new coordinates ({self.x}, {self.y}).")

        # Registrar las coordenadas actuales del coche en su ruta (path)
        self.path.append((self.x, self.y))

        # Calcular la recompensa, el estado siguiente y actualizar la tabla Q
        reward = self.calculate_reward(state, action)
        next_state = self.get_state(traffic_light, cars, pedestrians)
        next_state_index = states_car.index(next_state)
        best_next_action = np.argmax(q_table_car[next_state_index])

        # Cálculo del objetivo TD y error TD
        td_target = reward + gamma * q_table_car[next_state_index, best_next_action]
        td_error = td_target - q_table_car[state_index, action_index]

        # Actualizar la tabla Q usando el error TD
        q_table_car[state_index, action_index] += alpha * td_error


    def calculate_reward(self, state, action):
        if state == "red_light":
            return 5 if action == "stop" else -10
        elif state == "yellow_light_near":
            return 3 if action == "slow_down" else -5
        elif state == "green_light_clear":
            return 5 if action == "continue" else -2
        elif state == "green_light_car_ahead":
            return 3 if action == "slow_down" else -5
        return 0

class PedestrianAgent(ap.Agent):
    def setup(self, start_pos):
        self.x, self.y = start_pos
        self.crossing = False
        self.color = 'blue'

    def get_state(self, traffic_light, cars):
        car_proximity = "far"
        for car in cars:
            if self.distance_to_car(car) < 2:
                car_proximity = "near"
                break

        if traffic_light.state == "red":
            return "red_light_car_near" if car_proximity == "near" else "red_light_car_far"
        else:
            return "green_light"

    def distance_to_car(self, car):
        return np.sqrt((self.x - car.x)**2 + (self.y - car.y)**2)

    def move(self, traffic_light, cars):
        state = self.get_state(traffic_light, cars)
        state_index = states_person.index(state)
        action_index = np.argmax(q_table_person[state_index]) if random.random() > epsilon else random.randint(0, len(actions_person) - 1)
        action = actions_person[action_index]

        # Log de la acción del peatón
        log_entry = f"Pedestrian {self.id} at coordinates ({self.x}, {self.y}) in state '{state}' decided to '{action}'."
        log_entry = ["persona",self.x,self.y,state,action]
        print(log_entry)

        # Simular movimiento según la acción
        if action == "cross" and traffic_light.state == "green" and not self.crossing:
            self.y += 0.5  # Simula el cruce
            self.crossing = True
            print(f"Pedestrian {self.id} at coordinates ({self.x}, {self.y}) crosses the street.")
        elif action == "wait":
            self.crossing = False
            print(f"Pedestrian {self.id} at coordinates ({self.x}, {self.y}) waits at the corner.")

        # Cálculo del Q-learning
        reward = 10 if action == "cross" and state == "green_light" else -5 if action == "cross" else 2
        next_state = self.get_state(traffic_light, cars)
        next_state_index = states_person.index(next_state)
        best_next_action = np.argmax(q_table_person[next_state_index])
        td_target = reward + gamma * q_table_person[next_state_index, best_next_action]
        td_error = td_target - q_table_person[state_index, action_index]
        q_table_person[state_index, action_index] += alpha * td_error

        # Verificar si el archivo JSON existe; si no, crearlo vacío
        if not os.path.exists("simulacion.json"):
            with open("simulacion.json", "w") as file:
                json.dump({}, file)

        # Cargar el archivo JSON
        try:
            with open("simulacion.json", "r") as file:
                content = file.read().strip()
                movement_logs = json.loads(content) if content else {}
        except json.JSONDecodeError:
            movement_logs = {}  # Si hay un error, inicializar como diccionario vacío

        # Actualizar el JSON agregando el nuevo log_entry
        pedestrian_id = str(self.id)
        if pedestrian_id not in movement_logs:
            movement_logs[pedestrian_id] = []  # Crear una lista si no existe
        movement_logs[pedestrian_id].append(log_entry)  # Agregar el nuevo log_entry

        # Guardar los cambios en el archivo JSON
        with open("simulacion.json", "w") as file:
            json.dump(movement_logs, file, indent=4)
class IntersectionModel(ap.Model):
    def setup(self):
        self.traffic_light = TrafficLight(self)
        self.cars = ap.AgentList(self, 0)
        self.pedestrians = ap.AgentList(self, 0)
        self.spawn_points_cars = {
            'right': [(0, 4), (0, 6)],
            'left': [(10, 5), (10, 3)],
            'up': [(4, 0), (6, 0)],
            'down': [(5, 10), (3, 10)]
        }
        self.spawn_points_pedestrians = [(5, 0), (5, 10)]
        self.num_initial_cars = 5
        self.num_initial_pedestrians = 2
        self.car_spawn_interval = 15
        self.spawn_timer = 0

        for _ in range(self.num_initial_cars):
            direction = random.choice(list(self.spawn_points_cars.keys()))
            point = random.choice(self.spawn_points_cars[direction])
            self.cars.append(CarAgent(self, start_pos=point, direction=direction, lane=0))

        for _ in range(self.num_initial_pedestrians):
            start_pos = random.choice(self.spawn_points_pedestrians)
            self.pedestrians.append(PedestrianAgent(self, start_pos=start_pos))

    def step(self):
        self.traffic_light.update_state()
        self.spawn_timer += 1

        if self.spawn_timer >= self.car_spawn_interval:
            direction = random.choice(list(self.spawn_points_cars.keys()))
            point = random.choice(self.spawn_points_cars[direction])
            self.cars.append(CarAgent(self, start_pos=point, direction=direction, lane=0))
            self.spawn_timer = 0

        for car in self.cars:
            car.move(self.traffic_light, self.cars, self.pedestrians)

        for pedestrian in self.pedestrians:
            pedestrian.move(self.traffic_light, self.cars)

# Animation of the simulation
def update(frame):
    ax.clear()
    for i in range(11):
        ax.axhline(y=i, color='gray', linestyle=':')
        ax.axvline(x=i, color='gray', linestyle=':')
    road_color = '#404040'
    ax.fill_between([3, 7], 0, 10, color=road_color)
    ax.fill_between([0, 10], 3, 7, color=road_color)

    for i in [3, 5, 7]:
        ax.axhline(y=i, color='white', linestyle='--')
        ax.axvline(x=i, color='white', linestyle='--')

    model.step()

    light_color = {'red': 'red', 'yellow': 'yellow', 'green': 'green'}[model.traffic_light.state]
    ax.plot(5, 5, 'o', color=light_color, markersize=15)

    for car in model.cars:
        ax.plot(car.x, car.y, 'o', color=car.color, markersize=10)
        dx, dy = (0.3, 0) if car.direction == 'right' else (-0.3, 0) if car.direction == 'left' else (0, 0.3) if car.direction == 'up' else (0, -0.3)
        ax.arrow(car.x, car.y, dx, dy, head_width=0.2, head_length=0.2, fc=car.color, ec=car.color)


    for pedestrian in model.pedestrians:
        # Draw rectangles for pedestrians
        ax.add_patch(plt.Rectangle((pedestrian.x - 0.15, pedestrian.y - 0.15), 0.3, 0.3, color=pedestrian.color))
        if pedestrian.crossing:
            ax.arrow(pedestrian.x, pedestrian.y, 0, 0.3, head_width=0.1, head_length=0.1, fc=pedestrian.color, ec=pedestrian.color)

    ax.set_xlim(-0.5, 10.5)
    ax.set_ylim(-0.5, 10.5)
    ax.set_title(f'Traffic Simulation - Step {frame}\nTraffic Light: {model.traffic_light.state}')

# Setup and run the simulation
model = IntersectionModel()
model.setup()
fig, ax = plt.subplots(figsize=(10, 10))
ani = FuncAnimation(fig, update, frames=200, repeat=True, interval=100)
from IPython.display import HTML
HTML(ani.to_jshtml())
