import random
import heapq
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import agentpy as ap
import os
import json

# Q-Learning Parameters
alpha = 0.1
gamma = 0.9
epsilon = 0.2

# States and Actions
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

# Initialize Q-tables
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
        self.all_red_duration = 60
        self.directions = ["UP", "RIGHT", "DOWN", "LEFT",  "ALL_RED"]
        self.current_direction_index = 0
        self.states = {direction: "red" for direction in self.directions[:-1]}
        self.states[self.directions[0]] = "green"
        self.state = "green"
        self.is_all_red = False

    def update_state(self):
        self.timer += 1
        total_cycle_time = self.green_duration + self.yellow_duration

        if self.current_direction_index == len(self.directions) - 1:
            if self.timer > self.all_red_duration:
                self.timer = 1
                self.current_direction_index = 0
                self.states = {direction: "red" for direction in self.directions[:-1]}
                self.states[self.directions[0]] = "green"
                self.state = "green"
                self.is_all_red = False
            else:
                self.states = {direction: "red" for direction in self.directions[:-1]}
                self.state = "red"
                self.is_all_red = True
        else:
            if self.timer > total_cycle_time:
                self.timer = 1
                self.current_direction_index = (self.current_direction_index + 1) % len(self.directions)
                if self.current_direction_index == len(self.directions) - 1:
                    self.states = {direction: "red" for direction in self.directions[:-1]}
                    self.state = "red"
                    self.is_all_red = True
                else:
                    self.states = {direction: "red" for direction in self.directions[:-1]}
                    current_direction = self.directions[self.current_direction_index]
                    self.states[current_direction] = "green"
                    self.state = "green"
                    self.is_all_red = False
            elif self.timer > self.green_duration:
                current_direction = self.directions[self.current_direction_index]
                self.states[current_direction] = "yellow"
                self.state = "yellow"

    def get_state_for_direction(self, direction):
        return self.states[direction]

    def get_current_direction(self):
        return self.directions[self.current_direction_index]
    

import random
import heapq
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import agentpy as ap
import os
import json


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
        else:  # Green light
            for car in cars:
                if car is not self and self.is_car_ahead(car):
                    if self.distance_to_car(car) < 2:
                        return "green_light_car_ahead"
                    elif self.is_car_crossing(car):
                        return "green_light_car_crossing"

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

        log_entry = ["Carro", self.x, self.y, state, self.id, self.step]
        print(log_entry)

        # Handle JSON logging
        if not os.path.exists("simulacion.json"):
            with open("simulacion.json", "w") as file:
                json.dump({}, file)

        try:
            with open("simulacion.json", "r") as file:
                content = file.read().strip()
                movement_logs = json.loads(content) if content else {}
        except json.JSONDecodeError:
            movement_logs = {}

        car_id = str(self.id)
        if car_id not in movement_logs or not isinstance(movement_logs[car_id], list):
            movement_logs[car_id] = []
        movement_logs[car_id].append(log_entry)

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
        self.stuck_counter = 0
        self.last_position = start_pos
        self.last_recalculation = 0
        self.recalculation_cooldown = 15
        self.path_cache = {}
        self.waiting_time = 0
        self.current_sidewalk = self.get_current_sidewalk(start_pos)

    def get_my_traffic_light(self, traffic_light):
        if 2 <= self.x <= 3:
            return traffic_light.get_state_for_direction("LEFT")
        elif 7 <= self.x <= 8:
            return traffic_light.get_state_for_direction("RIGHT")
        elif 2 <= self.y <= 3:
            return traffic_light.get_state_for_direction("DOWN")
        elif 7 <= self.y <= 8:
            return traffic_light.get_state_for_direction("UP")
        return None

    def get_current_sidewalk(self, pos):
        x, y = pos
        if 2 <= x <= 3 and (y < 3 or y > 7):
            return "left_sidewalk"
        elif 7 <= x <= 8 and (y < 3 or y > 7):
            return "right_sidewalk"
        elif 2 <= y <= 3 and (x < 3 or x > 7):
            return "bottom_sidewalk"
        elif 7 <= y <= 8 and (x < 3 or x > 7):
            return "top_sidewalk"
        return None

    def is_crossing_safe(self, traffic_light, cars):
        is_at_horizontal_crossing = (2 <= self.y <= 3 or 7 <= self.y <= 8) and (2 <= self.x <= 8)
        is_at_vertical_crossing = (2 <= self.x <= 3 or 7 <= self.x <= 8) and (2 <= self.y <= 8)

        if not (is_at_horizontal_crossing or is_at_vertical_crossing):
            return True

        # Check if it's ALL_RED state
        if traffic_light.is_all_red:
            # Check for nearby cars
            for car in cars:
                if self.distance_to_car(car) < self.safe_distance:
                    self.waiting_time += 1
                    return False
            # If no cars are nearby during ALL_RED, allow crossing
            return True

        # If not ALL_RED, increment waiting time and don't allow crossing
        self.waiting_time += 1
        return False

    def distance_to_car(self, car):
        return np.sqrt((self.x - car.x)**2 + (self.y - car.y)**2)

    def a_star_path(self, start, goal, obstacles):
        # Verificar si ya tenemos este camino en caché
        cache_key = (start, goal)
        if cache_key in self.path_cache:
            return self.path_cache[cache_key].copy()

        if not (0 <= start[0] <= 10 and 0 <= start[1] <= 10 and
                0 <= goal[0] <= 10 and 0 <= goal[1] <= 10):
            return [start]

        # Crear grid de obstáculos de manera más eficiente
        obstacle_grid = set()
        for obstacle in obstacles:
            for coord in obstacle.coordinates:
                obstacle_grid.add(coord)

        frontier = []
        frontier_set = set()
        heapq.heappush(frontier, (0, start))
        frontier_set.add(start)

        came_from = {start: None}
        cost_so_far = {start: 0}
        max_iterations = 200
        iterations = 0

        def is_valid_position(pos):
            x, y = pos
            if not (0 <= x <= 10 and 0 <= y <= 10):
                return False
            if (int(x), int(y)) in obstacle_grid:
                return False
            # Evitar las carreteras excepto en los cruces
            is_road = (3 <= x <= 7 and 3 <= y <= 7)
            is_crossing = ((2 <= y <= 3 or 7 <= y <= 8) and (2 <= x <= 8)) or \
                         ((2 <= x <= 3 or 7 <= x <= 8) and (2 <= y <= 8))
            return not (is_road and not is_crossing)

        possible_moves = [
            (0, 1), (0, -1), (1, 0), (-1, 0),
            (1, 1), (1, -1), (-1, 1), (-1, -1)
        ]

        while frontier and iterations < max_iterations:
            iterations += 1
            current_cost, current = heapq.heappop(frontier)
            frontier_set.remove(current)

            if current == goal:
                break

            for dx, dy in possible_moves:
                next_pos = (current[0] + dx, current[1] + dy)

                if next_pos in frontier_set or not is_valid_position(next_pos):
                    continue

                new_cost = cost_so_far[current] + (1.4 if dx != 0 and dy != 0 else 1.0)

                if next_pos not in cost_so_far or new_cost < cost_so_far[next_pos]:
                    cost_so_far[next_pos] = new_cost
                    priority = new_cost + abs(goal[0] - next_pos[0]) + abs(goal[1] - next_pos[1])
                    heapq.heappush(frontier, (priority, next_pos))
                    frontier_set.add(next_pos)
                    came_from[next_pos] = current

        if goal not in came_from:
            return self.get_simple_path(start, goal, obstacles)

        path = []
        current = goal
        while current != start:
            path.append(current)
            current = came_from[current]
        path.append(start)
        path = list(reversed(path))

        if len(path) > 1:
            self.path_cache[cache_key] = path

        return path

    def get_simple_path(self, start, goal, obstacles):
        path = [start]
        current = list(start)

        obstacle_set = set()
        for obstacle in obstacles:
            for coord in obstacle.coordinates:
                obstacle_set.add(coord)

        def is_valid_move(x, y):
            if not (0 <= x <= 10 and 0 <= y <= 10):
                return False
            if (int(x), int(y)) in obstacle_set:
                return False
            is_road = (3 <= x <= 7 and 3 <= y <= 7)
            is_crossing = ((2 <= y <= 3 or 7 <= y <= 8) and (2 <= x <= 8)) or \
                         ((2 <= x <= 3 or 7 <= x <= 8) and (2 <= y <= 8))
            return not (is_road and not is_crossing)

        # Intentar moverse en X primero
        while current[0] != goal[0]:
            next_x = current[0] + (1 if goal[0] > current[0] else -1)
            if is_valid_move(next_x, current[1]):
                current[0] = next_x
                path.append(tuple(current))
            else:
                break

        # Luego moverse en Y
        while current[1] != goal[1]:
            next_y = current[1] + (1 if goal[1] > current[1] else -1)
            if is_valid_move(current[0], next_y):
                current[1] = next_y
                path.append(tuple(current))
            else:
                break

        return path

    def move(self, traffic_light, cars, obstacles):
        state = self.get_state(traffic_light, cars)
        state_index = states_person.index(state)

        current_sidewalk = self.get_current_sidewalk((self.x, self.y))
        need_recalculation = (
            not self.path_points or
            self.stuck_counter > 10 or
            (self.crossing and not self.is_crossing_safe(traffic_light, cars))
        )

        if need_recalculation and self.step - self.last_recalculation > self.recalculation_cooldown:
            self.path_points = self.a_star_path(
                (int(self.x), int(self.y)),
                (self.target_x, self.target_y),
                obstacles
            )
            self.last_recalculation = self.step
            self.stuck_counter = 0
            self.current_sidewalk = current_sidewalk

        can_cross = self.is_crossing_safe(traffic_light, cars)

        if can_cross and self.path_points:
            next_pos = self.path_points[0]

            current_pos = (self.x, self.y)
            if abs(current_pos[0] - self.last_position[0]) < 0.01 and \
               abs(current_pos[1] - self.last_position[1]) < 0.01:
                self.stuck_counter += 1
            else:
                self.stuck_counter = 0
            self.last_position = current_pos

            dx = next_pos[0] - self.x
            dy = next_pos[1] - self.y

            next_is_crossing = (
                (2 <= next_pos[1] <= 3 or 7 <= next_pos[1] <= 8) and (2 <= next_pos[0] <= 8) or
                (2 <= next_pos[0] <= 3 or 7 <= next_pos[0] <= 8) and (2 <= next_pos[1] <= 8)
            )

            if next_is_crossing and traffic_light.is_all_red:
                current_speed = self.speed * 1.5  # Increase speed by 50% during crossing
            else:
                current_speed = self.speed

            self.crossing = next_is_crossing

            if abs(dx) > self.speed:
                self.x += self.speed if dx > 0 else -self.speed
            else:
                self.x = next_pos[0]

            if abs(dy) > self.speed:
                self.y += self.speed if dy > 0 else -self.speed
            else:
                self.y = next_pos[1]

            if abs(self.x - next_pos[0]) < 0.01 and abs(self.y - next_pos[1]) < 0.01:
                self.path_points.pop(0)

            action_index = None
        else:
            self.crossing = False
            action_index = np.argmax(q_table_person[state_index]) if random.random() > epsilon else random.randint(0, len(actions_person)-1)

            action = actions_person[action_index]
            if action == "walk_left":
                self.x -= self.speed
            elif action == "walk_right":
                self.x += self.speed

        reward = self.calculate_reward(state, can_cross)
        next_state = self.get_state(traffic_light, cars)
        next_state_index = states_person.index(next_state)

        if action_index is not None:
            best_next_action = np.argmax(q_table_person[next_state_index])
            td_target = reward + gamma * q_table_person[next_state_index, best_next_action]
            td_error = td_target - q_table_person[state_index, action_index]
            q_table_person[state_index, action_index] += alpha * td_error

        self.step += 1


        log_entry = ["Persona", self.x, self.y, state, self.id, self.step]
        print(log_entry)

        # Handle JSON logging
        if not os.path.exists("simulacion.json"):
            with open("simulacion.json", "w") as file:
                json.dump({}, file)

        try:
            with open("simulacion.json", "r") as file:
                content = file.read().strip()
                movement_logs = json.loads(content) if content else {}
        except json.JSONDecodeError:
            movement_logs = {}

        pedestrian_id = str(self.id)
        if pedestrian_id not in movement_logs or not isinstance(movement_logs[pedestrian_id], list):
            movement_logs[pedestrian_id] = []
        movement_logs[pedestrian_id].append(log_entry)

        with open("simulacion.json", "w") as file:
            json.dump(movement_logs, file, indent=4)

    def calculate_reward(self, state, did_cross):
        rewards = {
            'crossing_safely': 150,
            'making_progress': 50, 
            'waiting_correctly': 2,
            'dangerous_crossing': -100,
            'collision_with_car': -200,
            'stuck_penalty': -30,
            'wrong_path': -60
        }

        current_sidewalk = self.get_current_sidewalk((self.x, self.y))

        if did_cross and self.crossing and self.model.traffic_light.is_all_red:
            return rewards['crossing_safely']

        for car in self.model.cars:
          if self.distance_to_car(car) < 1.5:  
            return rewards['collision_with_car']

        # if not did_cross and state.startswith("red_light"):
        #     return rewards['waiting_correctly']

        if self.crossing and not did_cross:
            return rewards['dangerous_crossing']

        if self.stuck_counter > 10:
            return rewards['stuck_penalty']

        if current_sidewalk is None and not self.crossing:
            return rewards['wrong_path']


        return -1

    def get_state(self, traffic_light, cars):
        self.my_traffic_light = self.get_my_traffic_light(traffic_light)

        car_proximity = "far"
        for car in cars:
            if self.distance_to_car(car) < 2:
                car_proximity = "near"
                break

        if traffic_light.is_all_red:
            return "red_light_car_near" if car_proximity == "near" else "red_light_car_far"
        else:
            return "green_light"

class IntersectionModel(ap.Model):
    def setup(self):
        self.traffic_light = TrafficLight(self)
        self.cars = ap.AgentList(self, 0)
        self.pedestrians = ap.AgentList(self, 0)
        self.obstacles = ap.AgentList(self,0)

        self.spawn_initial_obstacles()

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

    def add_obstacles(self,coordinates):
      self.obstacles.append(Obstacle(self,coordinates=coordinates))

    def spawn_initial_obstacles(self):
      fixed_obstacle_positions = [
          # coordenadas contorno
          [(0, 0), (1, 0)],
          [(9, 0), (9, 1)],
          [(0, 9), (1, 9)],
          [(9, 9), (9, 8)],

          # coordenadas de donde va el obsaculo
          [(0, 8)],  # Arriba a la izquierda
          [(0, 9)],
          [(1, 8)],
          [(1, 9)],
          [(1,7)],

          [(8, 8)],  # Arriba a la Derecha
          [(8, 9)],
          [(9, 8)],
          [(9, 8)],
          [(8,7)], # obstaculo

          [(0, 0)],  # abajo a la izquierda
          [(0, 1)],
          [(1, 0)],
          [(1, 1)],
          [(2,1)],

          [(8, 0)],  # abajo a la derecha
          [(8, 1)],
          [(9, 0)],
          [(9, 1)]

      ]
      for coordinates in fixed_obstacle_positions:
          self.add_obstacles(coordinates)

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
                pedestrian.move(self.traffic_light, self.cars, self.obstacles)
                active_pedestrians.append(pedestrian)

        while len(active_pedestrians) < self.num_initial_pedestrians:
            start, end = random.choice(self.pedestrian_pairs)
            active_pedestrians.append(PedestrianAgent(self, start_pos=start, target_pos=end))

        self.pedestrians = ap.AgentList(self, active_pedestrians)

class Obstacle(ap.Agent):
  def setup(self, coordinates):
    self.coordinates = coordinates
    self.color = "#8B4513"

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
            if car.turn_direction:
                dx, dy = -0.3, 0.3
            else:
                dx, dy = 0.3, -0.3
        else:
            dx, dy = (0.3, 0) if car.direction == 'right' else (-0.3, 0) if car.direction == 'left' else (0, 0.3) if car.direction == 'up' else (0, -0.3)
        ax.arrow(car.x, car.y, dx, dy, head_width=0.2, head_length=0.2, fc=car.color, ec=car.color)

    for pedestrian in model.pedestrians:
        ax.add_patch(plt.Rectangle((pedestrian.x - 0.15, pedestrian.y - 0.15), 0.3, 0.3, color=pedestrian.color))
        if pedestrian.crossing:
            ax.arrow(pedestrian.x, pedestrian.y, 0, 0.3, head_width=0.1, head_length=0.1, fc=pedestrian.color, ec=pedestrian.color)

    for obstacle in model.obstacles:
        for coord in obstacle.coordinates:
            ax.add_patch(plt.Rectangle((coord[0], coord[1]), 1, 1,
                                     facecolor=obstacle.color,
                                     edgecolor='black',
                                     linewidth=2))

    ax.set_xlim(-0.5, 10.5)
    ax.set_ylim(-0.5, 10.5)
    ax.set_title(f'Traffic Simulation - Step {frame}\nTraffic Light: {model.traffic_light.state}')

model = IntersectionModel()
model.setup()
fig, ax = plt.subplots(figsize=(10, 10))
ani = FuncAnimation(fig, update, frames=500, repeat=True, interval=200)

from IPython.display import HTML
HTML(ani.to_jshtml())