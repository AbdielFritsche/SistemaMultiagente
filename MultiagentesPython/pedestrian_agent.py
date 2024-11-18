import agentpy as ap

class Pedestrian(ap.Agent):
    def setup(self):
        self.state = "waiting"  # Estados posibles: 'waiting', 'crossing'
        self.grid = self.model.grid

    def step(self):
        if self.state == "waiting":
            # Buscar celdas vecinas transitables
            neighbors = self.grid.neighbors(self.position, distance=1)
            walkable = [n for n in neighbors if self.grid.is_walkable(n)]
            if walkable:
                self.state = "crossing"
                new_position = self.random.choice(walkable)
                self.grid.move_to(self, new_position)