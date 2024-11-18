import agentpy as ap
from environment.grid import Grid
from agents.pedestrian import Pedestrian
from agents.vehicle import Vehicle
from agents.traffic_light import TrafficLight
from api.server import update_model_data

class TrafficModel(ap.Model):
    def setup(self):
        self.grid = Grid(self, width=10, height=10)

        # Configurar banquetas, calles e intersecciones
        sidewalk_positions = [(x, 0) for x in range(10)] + [(x, 9) for x in range(10)]
        obstacle_positions = [(5, 2), (5, 7)]
        intersection_positions = [(4, 4), (5, 4), (4, 5), (5, 5)]

        self.grid.set_sidewalk(sidewalk_positions)
        self.grid.set_obstacles(obstacle_positions)
        self.grid.set_intersections(intersection_positions)

        # Crear agentes
        self.pedestrians = ap.AgentList(self, 10, Pedestrian)
        self.vehicles = ap.AgentList(self, 5, Vehicle)
        self.traffic_lights = ap.AgentList(self, 4, TrafficLight)

        # Ubicar agentes en el grid
        assign_agents_to_grid(self.grid, self.pedestrians, "sidewalk")
        assign_agents_to_grid(self.grid, self.vehicles, "road")
        assign_agents_to_grid(self.grid, self.traffic_lights, "intersection")

    def step(self):
        # Actualizar agentes
        self.traffic_lights.step()
        self.pedestrians.step()
        self.vehicles.step()

        # Actualizar datos del modelo para la API
        positions = self.get_agent_positions()
        update_model_data(positions)

    def get_agent_positions(self):
        """Obtiene las posiciones de los agentes."""
        data = {
            "pedestrians": [{"id": p.id, "position": p.position, "state": p.state} for p in self.pedestrians],
            "vehicles": [{"id": v.id, "position": v.position, "state": v.state} for v in self.vehicles],
            "traffic_lights": [{"id": t.id, "position": t.position, "state": t.state} for t in self.traffic_lights]
        }
        return data