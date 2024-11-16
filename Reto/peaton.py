import agentpy as ap
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import random

# Parámetros de Q-learning
alpha = 0.1
gamma = 0.9
epsilon = 0.2

states_person = ["red_light_car_far", "red_light_car_near", "green_light"]
actions_person = ["wait", "cross"]

# Inicializar tablas Q
q_table_person = np.random.uniform(low=-0.1, high=0.1, size=(len(states_person), len(actions_person)))

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
        action_index = np.argmax(q_table_person[state_index]) if random.random() > epsilon else random.randint(0, len(actions_person)-1)
        action = actions_person[action_index]
        
        if action == "cross" and traffic_light.state == "green" and not self.crossing:
            self.y += 0.5  # Simula el cruce
            self.crossing = True
        elif action == "wait":
            self.crossing = False

        reward = 10 if action == "cross" and state == "green_light" else -5 if action == "cross" else 2
        next_state = self.get_state(traffic_light, cars)
        next_state_index = states_person.index(next_state)
        best_next_action = np.argmax(q_table_person[next_state_index])
        td_target = reward + gamma * q_table_person[next_state_index, best_next_action]
        td_error = td_target - q_table_person[state_index, action_index]
        q_table_person[state_index, action_index] += alpha * td_error
