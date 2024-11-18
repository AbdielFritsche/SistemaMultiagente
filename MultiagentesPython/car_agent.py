class Vehicle(ap.Agent):
    def setup(self):
        self.position = self.model.grid.find_empty()
        self.state = "waiting"  # Estados posibles: 'waiting', 'moving'
        self.grid = self.model.grid

    def step(self):
        if self.state == "waiting":
            if self.grid.is_driveable(self.position):
                self.state = "moving"
                self.move()

    def move(self):
        # Lógica para moverse hacia adelante
        neighbors = self.grid.neighbors(self.position, distance=1)
        driveable = [n for n in neighbors if self.grid.is_driveable(n)]
        if driveable:
            new_position = self.random.choice(driveable)
            self.grid.move_to(self, new_position)
    