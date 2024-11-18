class Pedestrian(ap.Agent):
    def setup(self):
        self.position = self.model.grid.find_empty()  # Encuentra una posición inicial vacía
        self.state = "waiting"  # Estados posibles: 'waiting', 'crossing'
        self.grid = self.model.grid

    def step(self):
        if self.state == "waiting":
            if self.grid.is_walkable(self.position):
                self.state = "crossing"
                self.move()

    def move(self):
        # Lógica para moverse a una posición adyacente
        neighbors = self.grid.neighbors(self.position, distance=1)
        walkable = [n for n in neighbors if self.grid.is_walkable(n)]
        if walkable:
            new_position = self.random.choice(walkable)
            self.grid.move_to(self, new_position)