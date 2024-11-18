class TrafficLight(ap.Agent):
    def setup(self):
        self.position = self.model.grid.find_empty()
        self.state = "red"  # Estados posibles: 'red', 'green'
        self.timer = 0

    def step(self):
        self.timer += 1
        if self.timer > 10:  # Cambiar cada 10 pasos
            self.state = "green" if self.state == "red" else "red"
            self.timer = 0