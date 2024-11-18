import agentpy as ap

class Vehicle(ap.Agent):
    def setup(self):
        self.state = "waiting"  # Estados posibles: 'waiting', 'moving'
        self.grid = self.model.grid

    def step(self):
        if self.state == "waiting":
            # Buscar celdas vecinas transitables
            neighbors = self.grid.neighbors(self.position, distance=1)
            driveable = [n for n in neighbors if self.grid.is_driveable(n)]
            if driveable:
                self.state = "moving"
                new_position = self.random.choice(driveable)
                self.grid.move_to(self, new_position)