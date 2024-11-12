from ambiente import Habitacion, ap, Robot,plt

class Simulacion(ap.Model):
    def setup(self):
        self.habitacion = Habitacion(self, filas=self.p.filas, columnas=self.p.columnas)
        
        self.num_agentes = self.p.num_agentes
        self.robots = ap.AgentList(self, self.num_agentes, Robot)
        
        self.habitacion.colocar_robots(self.robots)
        self.tipo_movimiento = self.p.tipo_movimiento


    def step(self):
        for robot in self.robots:
            robot.step()

        self.habitacion.actualizar_grafica()

        if all(robot.estado == "detenido" for robot in self.robots):
            self.stop()

    def end(self): 
        total_recompensas = sum(robot.recompensas_recogidas for robot in self.robots)
        
        self.report("recompensas", total_recompensas)

        total_castigos = sum(robot.castigos_recogidos for robot in self.robots)
        energia_restante = sum(robot.energia for robot in self.robots)
        
        print(f"Reporte - Recompensas: {total_recompensas}, Castigos: {total_castigos}, Energía Restante: {energia_restante}")
        
        self.report("castigos", total_castigos)
        self.report("energia_restante", energia_restante)
