import agentpy as ap
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import random

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