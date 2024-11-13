import agentpy as ap
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
import random
from ambiente import IntersectionModel

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