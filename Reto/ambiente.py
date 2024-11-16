import agentpy as ap
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import random
from semaforo import TrafficLight
from vehicle import CarAgent
from peaton import PedestrianAgent

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