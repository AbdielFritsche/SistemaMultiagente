import agentpy as ap
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import random


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

# Inicializar tablas Q
q_table_car = np.random.uniform(low=-0.1, high=0.1, size=(len(states_car), len(actions_car)))

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
        state = self.get_state(traffic_light, cars, pedestrians)
        state_index = states_car.index(state)
        action_index = np.argmax(q_table_car[state_index]) if random.random() > epsilon else random.randint(0, len(actions_car)-1)
        action = actions_car[action_index]
        
        self.speed = 1.0 if action == "continue" else 0.5 if action == "slow_down" else 0.0
        if not self.waiting:
            for car in cars:
                if car is not self and self.is_car_in_intersection(car):
                    self.speed = 0.0
                    break

            if self.direction == 'right':
                self.x += self.speed
            elif self.direction == 'left':
                self.x -= self.speed
            elif self.direction == 'up':
                self.y += self.speed
            else:
                self.y -= self.speed
        self.path.append((self.x, self.y))
        
        reward = self.calculate_reward(state, action)
        next_state = self.get_state(traffic_light, cars, pedestrians)
        next_state_index = states_car.index(next_state)
        best_next_action = np.argmax(q_table_car[next_state_index])
        td_target = reward + gamma * q_table_car[next_state_index, best_next_action]
        td_error = td_target - q_table_car[state_index, action_index]
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