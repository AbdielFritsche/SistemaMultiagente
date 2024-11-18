import agentpy as ap

class TrafficLight(ap.Agent):
    def setup(self):
        self.state = "red"  # Estados posibles: 'red', 'green'
        self.timer = 0

    def step(self):
        # Alternar el estado del semáforo cada 10 pasos
        self.timer += 1
        if self.timer > 10:
            self.state = "green" if self.state == "red" else "red"
            self.timer = 0