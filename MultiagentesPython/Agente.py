import agentpy as ap
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import random
import heapq
import os
import json
# Parámetros de Q-Learning
alpha = 0.1
gamma = 0.9
epsilon = 0.2

# Estados y acciones
states_car = [
    "green_light_clear",
    "green_light_car_ahead",
    "green_light_car_crossing",
    "yellow_light_far",
    "yellow_light_near",
    "red_light"
]
actions_car = ["continue", "slow_down", "stop", "turn_left", "turn_right"]

states_person = ["red_light_car_far", "red_light_car_near", "green_light"]
actions_person = ["wait", "cross", "walk_left", "walk_right"]

# Inicialización de Q-tables
q_table_car = np.random.uniform(low=-0.1, high=0.1, size=(len(states_car), len(actions_car)))
q_table_person = np.random.uniform(low=-0.1, high=0.1, size=(len(states_person), len(actions_person)))

class TrafficLight(ap.Agent):
    light_counter = 0
    def setup(self):
        self.light_id = TrafficLight.light_counter
        TrafficLight.light_counter += 1
        self.semaforo_coordinates = {
            "UP": {"pos": (7, 3), "id": 0},
            "RIGHT": {"pos": (3, 3), "id": 1},
            "DOWN": {"pos": (3, 7), "id": 2},
            "LEFT": {"pos": (7, 7), "id": 3}
        }
        self.timer = 0
        self.green_duration = 20
        self.yellow_duration = 5
        self.directions = ["UP", "RIGHT", "DOWN", "LEFT"]
        self.current_direction_index = 0
        self.states = {direction: "red" for direction in self.directions}
        self.states[self.directions[0]] = "green"
        self.state = "green"

    def update_state(self):
        self.timer += 1
        total_cycle_time = self.green_duration + self.yellow_duration

        if self.timer > total_cycle_time:
            self.timer = 1
            self.current_direction_index = (self.current_direction_index + 1) % len(self.directions)
            self.states = {direction: "red" for direction in self.directions}
            current_direction = self.directions[self.current_direction_index]
            self.states[current_direction] = "green"
            self.state = "green"
        elif self.timer > self.green_duration:
            current_direction = self.directions[self.current_direction_index]
            self.states[current_direction] = "yellow"
            self.state = "yellow"

    def get_state_for_direction(self, direction):
        return self.states[direction]

    def get_current_direction(self):
        return self.directions[self.current_direction_index]

class CarAgent(ap.Agent):
    def setup(self, start_pos, direction, lane, traffic_light_id):
        self.id = random.randint(1000, 9999)
        self.x, self.y = start_pos
        self.direction = direction
        self.lane = lane
        self.traffic_light_id = traffic_light_id
        self.speed = 1.0
        self.waiting = False
        self.path = [(self.x, self.y)]
        self.color = np.random.rand(3)
        self.turning = False
        self.turn_direction = None
        self.turn_progress = 0
        self.current_angle = 0
        self.target_angle = 0
        self.step = 0
    def has_completed_route(self):
        grid_size = 10
        if self.direction == 'right' and self.x >= grid_size:
            return True
        if self.direction == 'left' and self.x <= 0:
            return True
        if self.direction == 'up' and self.y >= grid_size:
            return True
        if self.direction == 'down' and self.y <= 0:
            return True
        return False

    def get_state(self, traffic_light, cars, pedestrians):
        direction_to_traffic = {
            'right': 'RIGHT',
            'left': 'LEFT',
            'up': 'UP',
            'down': 'DOWN'
        }
        traffic_direction = direction_to_traffic[self.direction]
        traffic_state = traffic_light.get_state_for_direction(traffic_direction)
        
        if traffic_state == "red":
            return "red_light"
        elif traffic_state == "yellow":
            dist_to_intersection = self.distance_to_intersection()
            return "yellow_light_near" if dist_to_intersection < 3 else "yellow_light_far"
        else:  # Si es verde, verificar otras condiciones
            for car in cars:
                if car is not self and self.is_car_ahead(car):
                    if self.distance_to_car(car) < 2:
                        return "green_light_car_ahead"
                    elif self.is_car_crossing(car):
                        return "green_light_car_crossing"
            
            # Verificar si hay peatones cruzando
            for pedestrian in pedestrians:
                if pedestrian.crossing and self.is_pedestrian_in_path(pedestrian):
                    return "green_light_car_ahead"
            
            return "green_light_clear"

    def is_pedestrian_in_path(self, pedestrian):
        if self.direction in ['right', 'left']:
            return (abs(pedestrian.y - self.y) < 1 and 
                    ((self.direction == 'right' and pedestrian.x > self.x) or
                     (self.direction == 'left' and pedestrian.x < self.x)))
        else:  # up or down
            return (abs(pedestrian.x - self.x) < 1 and 
                    ((self.direction == 'up' and pedestrian.y > self.y) or
                     (self.direction == 'down' and pedestrian.y < self.y)))

    def distance_to_intersection(self):
        return abs(5 - (self.x if self.direction in ['right', 'left'] else self.y))

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

    def is_car_crossing(self, other_car):
        intersection_box = {'x': (4, 6), 'y': (4, 6)}
        return (intersection_box['x'][0] <= other_car.x <= intersection_box['x'][1] and
                intersection_box['y'][0] <= other_car.y <= intersection_box['y'][1])

    def move(self, traffic_light, cars, pedestrians):
        state = self.get_state(traffic_light, cars, pedestrians)
        state_index = states_car.index(state)

        pedestrians_crossing = any(p.crossing for p in pedestrians)

        if traffic_light.state == "red" or pedestrians_crossing:
            self.speed = 0
            self.turning = False
            return

        action_index = np.argmax(q_table_car[state_index]) if random.random() > epsilon else random.randint(0, len(actions_car)-1)
        action = actions_car[action_index]

        if action in ["turn_left", "turn_right"] and 4 <= self.x <= 6 and 4 <= self.y <= 6:
            self.speed = 0.2
            self.turning = True
            self.turn_direction = action == "turn_left"

            if self.direction == 'right':
                if self.turn_direction:
                    if self.y < 6:
                        self.y += self.speed
                    else:
                        self.direction = 'up'
                        self.turning = False
                else:
                    if self.y > 4:
                        self.y -= self.speed
                    else:
                        self.direction = 'down'
                        self.turning = False

            elif self.direction == 'left':
                if self.turn_direction:
                    if self.y > 4:
                        self.y -= self.speed
                    else:
                        self.direction = 'down'
                        self.turning = False
                else:
                    if self.y < 6:
                        self.y += self.speed
                    else:
                        self.direction = 'up'
                        self.turning = False

            elif self.direction == 'up':
                if self.turn_direction:
                    if self.x > 4:
                        self.x -= self.speed
                    else:
                        self.direction = 'left'
                        self.turning = False
                else:
                    if self.x < 6:
                        self.x += self.speed
                    else:
                        self.direction = 'right'
                        self.turning = False

            else:  # direction == 'down'
                if self.turn_direction:
                    if self.x < 6:
                        self.x += self.speed
                    else:
                        self.direction = 'right'
                        self.turning = False
                else:
                    if self.x > 4:
                        self.x -= self.speed
                    else:
                        self.direction = 'left'
                        self.turning = False

        else:
            self.turning = False
            if action == "continue":
                self.speed = 1.0
            elif action == "slow_down":
                self.speed = 0.5
            elif action == "stop":
                self.speed = 0.0

            if not self.turning:
                if self.direction == 'right':
                    self.x += self.speed
                elif self.direction == 'left':
                    self.x -= self.speed
                elif self.direction == 'up':
                    self.y += self.speed
                else:
                    self.y -= self.speed

                self.step +=1

        log_entry = {
            "X": self.x,
            "Y": self.y,
            "EstadoSemaforo": state,
            "Accion": action
        }
        print(log_entry)  # Si deseas verlo en la consola también

        # Verificar si el archivo existe; si no, crearlo vacío
        if not os.path.exists("simulacion.json"):
            with open("simulacion.json", "w") as file:
                json.dump({}, file)

        # Cargar el archivo JSON (manejar archivo vacío o formato incorrecto)
        try:
            with open("simulacion.json", "r") as file:
                content = file.read().strip()
                movement_logs = json.loads(content) if content else {}
        except (json.JSONDecodeError, TypeError):
            movement_logs = {}  # Si hay un error, inicializar como diccionario vacío

        # Asegurar que cada ID tenga la estructura correcta
        car_id = str(self.id)
        if car_id not in movement_logs:
            # Si el agente no existe, crearlo con su estructura completa
            movement_logs[car_id] = {
                "Agente": {
                    "ID": car_id,
                    "Tipo": "Carro",
                    "Movimientos": []
                }
            }
        elif "Agente" not in movement_logs[car_id] or "Movimientos" not in movement_logs[car_id]["Agente"]:
            # Si la estructura es incorrecta, restablecerla
            movement_logs[car_id] = {
                "Agente": {
                    "ID": car_id,
                    "Tipo": "Carro",
                    "Movimientos": []
                }
            }

        # Agregar el nuevo log_entry a la lista de movimientos del agente
        movement_logs[car_id]["Agente"]["Movimientos"].append(log_entry)

        # Guardar los cambios en el archivo JSON
        with open("simulacion.json", "w") as file:
            json.dump(movement_logs, file, indent=4)

        self.path.append((self.x, self.y))
        reward = self.calculate_reward(state, action)

        next_state = self.get_state(traffic_light, cars, pedestrians)
        next_state_index = states_car.index(next_state)
        best_next_action = np.argmax(q_table_car[next_state_index])

        td_target = reward + gamma * q_table_car[next_state_index, best_next_action]
        td_error = td_target - q_table_car[state_index, action_index]
        q_table_car[state_index, action_index] += alpha * td_error

    def calculate_reward(self, state, action):
        reward = 0
        if state == "red_light":
            reward = 5 if action == "stop" else -10
        elif state == "yellow_light_near":
            reward = 3 if action == "slow_down" else -5
        elif state == "green_light_clear":
            reward = 5 if action == "continue" else -2
        elif state == "green_light_car_ahead":
            reward = 3 if action == "slow_down" else -5
        return reward

class PedestrianAgent(ap.Agent):
    def setup(self, start_pos, target_pos):
        self.x, self.y = start_pos
        self.target_x, self.target_y = target_pos
        self.path_points = []
        self.crossing = False
        self.color = 'blue'
        self.safe_distance = 2.0
        self.speed = 0.2
        self.my_traffic_light = None
        self.step = 0
    def get_my_traffic_light(self, traffic_light):
        if 2 <= self.x <= 3:  # Peatón en el cruce oeste
            return traffic_light.get_state_for_direction("LEFT")
        elif 7 <= self.x <= 8:  # Peatón en el cruce este
            return traffic_light.get_state_for_direction("RIGHT")
        elif 2 <= self.y <= 3:  # Peatón en el cruce sur
            return traffic_light.get_state_for_direction("DOWN")
        elif 7 <= self.y <= 8:  # Peatón en el cruce norte
            return traffic_light.get_state_for_direction("UP")
        return None

    def is_crossing_safe(self, traffic_light, cars):
        self.my_traffic_light = self.get_my_traffic_light(traffic_light)
        
        if self.my_traffic_light != "red":
            return False
        
        for car in cars:
            if self.distance_to_car(car) < self.safe_distance:
                return False
                
        is_at_horizontal_crossing = (2 <= self.y <= 3 or 7 <= self.y <= 8) and (2 <= self.x <= 8)
        is_at_vertical_crossing = (2 <= self.x <= 3 or 7 <= self.x <= 8) and (2 <= self.y <= 8)
        
        return is_at_horizontal_crossing or is_at_vertical_crossing

    def distance_to_car(self, car):
        return np.sqrt((self.x - car.x)**2 + (self.y - car.y)**2)

    def a_star_path(self, start, goal, cars):
        if not (0 <= start[0] <= 10 and 0 <= start[1] <= 10 and
                0 <= goal[0] <= 10 and 0 <= goal[1] <= 10):
            return [start]

        frontier = []
        heapq.heappush(frontier, (0, start))
        came_from = {start: None}
        cost_so_far = {start: 0}
        max_iterations = 1000

        iterations = 0
        while frontier and iterations < max_iterations:
            iterations += 1
            current_cost, current = heapq.heappop(frontier)

            if current == goal:
                break

            possible_moves = [
                (current[0], current[1] + 1),
                (current[0], current[1] - 1),
                (current[0] + 1, current[1]),
                (current[0] - 1, current[1])
            ]

            for next_pos in possible_moves:
                if not (0 <= next_pos[0] <= 10 and 0 <= next_pos[1] <= 10):
                    continue

                if self.is_road_or_intersection(next_pos[0], next_pos[1]):
                    continue

                new_cost = cost_so_far[current] + 1
                if next_pos not in cost_so_far or new_cost < cost_so_far[next_pos]:
                    cost_so_far[next_pos] = new_cost
                    priority = new_cost + self.heuristic(goal, next_pos)
                    heapq.heappush(frontier, (priority, next_pos))
                    came_from[next_pos] = current

        if goal not in came_from:
            return self.fallback_path(start, goal)

        path = []
        current = goal
        while current != start:
            path.append(current)
            current = came_from[current]
        path.append(start)
        return list(reversed(path))

    def heuristic(self, a, b):
        return abs(a[0] - b[0]) + abs(a[1] - b[1])

    def is_road_or_intersection(self, x, y):
        is_vertical_road = 3 <= x <= 7 and not (2 <= y <= 3 or 7 <= y <= 8)
        is_horizontal_road = 3 <= y <= 7 and not (2 <= x <= 3 or 7 <= x <= 8)
        
        is_crosswalk = ((2 <= y <= 3 or 7 <= y <= 8) and (2 <= x <= 8)) or \
                      ((2 <= x <= 3 or 7 <= x <= 8) and (2 <= y <= 8))
        
        return (is_vertical_road or is_horizontal_road) and not is_crosswalk

    def fallback_path(self, start, goal):
        path = [start]
        current = list(start)

        while current[0] != goal[0]:
            next_x = current[0] + (1 if goal[0] > current[0] else -1)
            if not self.is_road_or_intersection(next_x, current[1]):
                current[0] = next_x
                path.append(tuple(current))

        while current[1] != goal[1]:
            next_y = current[1] + (1 if goal[1] > current[1] else -1)
            if not self.is_road_or_intersection(current[0], next_y):
                current[1] = next_y
                path.append(tuple(current))

        return path

    def move(self, traffic_light, cars):
        state = self.get_state(traffic_light, cars)
        state_index = states_person.index(state)
        
        if not self.path_points:
            self.path_points = self.a_star_path((self.x, self.y), (self.target_x, self.target_y), cars)
        
        can_cross = self.is_crossing_safe(traffic_light, cars)
        
        if can_cross and self.path_points:
            next_pos = self.path_points[0]
            dx = next_pos[0] - self.x
            dy = next_pos[1] - self.y
            
            if abs(dx) > self.speed:
                self.x += self.speed if dx > 0 else -self.speed
            else:
                self.x = next_pos[0]
                
            if abs(dy) > self.speed:
                self.y += self.speed if dy > 0 else -self.speed
            else:
                self.y = next_pos[1]
                
            if self.x == next_pos[0] and self.y == next_pos[1]:
                self.path_points.pop(0)
            
            self.crossing = True
        else:
            self.crossing = False
            action_index = np.argmax(q_table_person[state_index])
            if random.random() < epsilon:
                action_index = random.randint(0, len(actions_person) - 1)
            
            action = actions_person[action_index]
            if action == "walk_left":
                self.x -= self.speed
            elif action == "walk_right":
                self.x += self.speed

        reward = self.calculate_reward(state, can_cross)
        next_state = self.get_state(traffic_light, cars)
        next_state_index = states_person.index(next_state)
        best_next_action = np.argmax(q_table_person[next_state_index])
        
        td_target = reward + gamma * q_table_person[next_state_index, best_next_action]
        td_error = td_target - q_table_person[state_index, action_index]
        q_table_person[state_index, action_index] += alpha * td_error

        self.step +=1

        log_entry = {
            "X": self.x,
            "Y": self.y,
            "EstadoSemaforo": state,
            "Accion": action
        }
        print(log_entry)  # Si deseas verlo en la consola también

        # Verificar si el archivo existe; si no, crearlo vacío
        if not os.path.exists("simulacion.json"):
            with open("simulacion.json", "w") as file:
                json.dump({}, file)

        # Cargar el archivo JSON (manejar archivo vacío o formato incorrecto)
        try:
            with open("simulacion.json", "r") as file:
                content = file.read().strip()
                movement_logs = json.loads(content) if content else {}
        except (json.JSONDecodeError, TypeError):
            movement_logs = {}  # Si hay un error, inicializar como diccionario vacío

        # Asegurar que cada ID tenga la estructura correcta
        car_id = str(self.id)
        if car_id not in movement_logs:
            # Si el agente no existe, crearlo con su estructura completa
            movement_logs[car_id] = {
                "Agente": {
                    "ID": car_id,
                    "Tipo": "Persona",
                    "Movimientos": []
                }
            }
        elif "Agente" not in movement_logs[car_id] or "Movimientos" not in movement_logs[car_id]["Agente"]:
            # Si la estructura es incorrecta, restablecerla
            movement_logs[car_id] = {
                "Agente": {
                    "ID": car_id,
                    "Tipo": "Persona",
                    "Movimientos": []
                }
            }

        # Agregar el nuevo log_entry a la lista de movimientos del agente
        movement_logs[car_id]["Agente"]["Movimientos"].append(log_entry)

        # Guardar los cambios en el archivo JSON
        with open("simulacion.json", "w") as file:
            json.dump(movement_logs, file, indent=4)

    def calculate_reward(self, state, did_cross):
        if state.startswith("red_light"):
            if did_cross:
                return 10
            return 1
        return -5

    def get_state(self, traffic_light, cars):
        self.my_traffic_light = self.get_my_traffic_light(traffic_light)
        
        car_proximity = "far"
        for car in cars:
            if self.distance_to_car(car) < 2:
                car_proximity = "near"
                break
        
        if self.my_traffic_light == "red":
            return "red_light_car_near" if car_proximity == "near" else "red_light_car_far"
        else:
            return "green_light"

class IntersectionModel(ap.Model):
    def setup(self):
        self.traffic_light = TrafficLight(self)
        self.cars = ap.AgentList(self, 0)
        self.pedestrians = ap.AgentList(self, 0)

        self.spawn_info = {
            'right': {
                'points': [(0, 3.5), (0, 4.5)],
                'traffic_light_id': 1,
                'direction': 'right'
            },
            'left': {
                'points': [(10, 5.5), (10, 6.5)],
                'traffic_light_id': 3,
                'direction': 'left'
            },
            'up': {
                'points': [(5.5, 0), (6.5, 0)],
                'traffic_light_id': 0,
                'direction': 'up'
            },
            'down': {
                'points': [(3.5, 10), (4.5, 10)],
                'traffic_light_id': 2,
                'direction': 'down'
            }
        }

        self.spawn_points_cars = self.spawn_info
        self.num_initial_cars = 5
        self.num_initial_pedestrians = 4
        self.car_spawn_interval = 15
        self.spawn_timer = 0

        for _ in range(self.num_initial_cars):
            spawn_direction = random.choice(list(self.spawn_info.keys()))
            info = self.spawn_info[spawn_direction]
            spawn_point = random.choice(info['points'])
            
            self.cars.append(CarAgent(self, 
                                    start_pos=spawn_point,
                                    direction=info['direction'],
                                    lane=0,
                                    traffic_light_id=info['traffic_light_id']))

        self.pedestrian_pairs = [
            ((2, 0), (2, 10)),
            ((8, 0), (8, 10)),
            ((2, 10), (2, 0)),
            ((8, 10), (8, 0)),
            ((0, 2), (10, 2)),
            ((0, 8), (10, 8))
        ]

        for start, end in self.pedestrian_pairs[:self.num_initial_pedestrians]:
            self.pedestrians.append(PedestrianAgent(self, start_pos=start, target_pos=end))

    def step(self):
        self.traffic_light.update_state()

        active_cars = []
        for car in self.cars:
            if not car.has_completed_route():
                car.move(self.traffic_light, self.cars, self.pedestrians)
                active_cars.append(car)

        self.cars = ap.AgentList(self, active_cars)

        self.spawn_timer += 1
        if self.spawn_timer >= self.car_spawn_interval:
            spawn_direction = random.choice(list(self.spawn_points_cars.keys()))
            info = self.spawn_info[spawn_direction]
            point = random.choice(info['points'])
            can_spawn = True
            for car in self.cars:
                if np.sqrt((car.x - point[0])**2 + (car.y - point[1])**2) < 1:
                    can_spawn = False
                    break
            if can_spawn:
                self.cars.append(CarAgent(self, start_pos=point, direction=info['direction'], lane=0,
                                        traffic_light_id=info['traffic_light_id']))
                self.spawn_timer = 0

        active_pedestrians = []
        for pedestrian in self.pedestrians:
            if (abs(pedestrian.x - pedestrian.target_x) > 0.1 or
                abs(pedestrian.y - pedestrian.target_y) > 0.1):
                pedestrian.move(self.traffic_light, self.cars)
                active_pedestrians.append(pedestrian)

        while len(active_pedestrians) < self.num_initial_pedestrians:
            start, end = random.choice(self.pedestrian_pairs)
            active_pedestrians.append(PedestrianAgent(self, start_pos=start, target_pos=end))

        self.pedestrians = ap.AgentList(self, active_pedestrians)

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

    sidewalk_color = '#C0C0C0'
    ax.fill_between([0, 3], 2, 3, color=sidewalk_color)
    ax.fill_between([7, 10], 2, 3, color=sidewalk_color)
    ax.fill_between([0, 3], 7, 8, color=sidewalk_color)
    ax.fill_between([7, 10], 7, 8, color=sidewalk_color)
    ax.fill_between([2, 3], 0, 2, color=sidewalk_color)
    ax.fill_between([2, 3], 8, 10, color=sidewalk_color)
    ax.fill_between([7, 8], 0, 2, color=sidewalk_color)
    ax.fill_between([7, 8], 8, 10, color=sidewalk_color)

    model.step()

    for direction, coord_info in model.traffic_light.semaforo_coordinates.items():
        light_color = model.traffic_light.get_state_for_direction(direction)
        coord = coord_info["pos"]
        ax.plot(coord[0], coord[1], 'o', color=light_color, markersize=10, markeredgecolor='black')

    for car in model.cars:
        ax.plot(car.x, car.y, 'o', color=car.color, markersize=10)
        if car.turning:
            if car.turn_direction:  # left turn
                dx, dy = -0.3, 0.3
            else:  # right turn
                dx, dy = 0.3, -0.3
        else:
            dx, dy = (0.3, 0) if car.direction == 'right' else (-0.3, 0) if car.direction == 'left' else (0, 0.3) if car.direction == 'up' else (0, -0.3)
        ax.arrow(car.x, car.y, dx, dy, head_width=0.2, head_length=0.2, fc=car.color, ec=car.color)

    for pedestrian in model.pedestrians:
        ax.add_patch(plt.Rectangle((pedestrian.x - 0.15, pedestrian.y - 0.15), 0.3, 0.3, color=pedestrian.color))
        if pedestrian.crossing:
            ax.arrow(pedestrian.x, pedestrian.y, 0, 0.3, head_width=0.1, head_length=0.1, fc=pedestrian.color, ec=pedestrian.color)

    ax.set_xlim(-0.5, 10.5)
    ax.set_ylim(-0.5, 10.5)
    ax.set_title(f'Traffic Simulation - Step {frame}\nTraffic Light: {model.traffic_light.state}')

model = IntersectionModel()
model.setup()
fig, ax = plt.subplots(figsize=(10, 10))
ani = FuncAnimation(fig, update, frames=200, repeat=True, interval=100)

from IPython.display import HTML
HTML(ani.to_jshtml())