'''
Ambiente:
Una habitación es un rectángulo, ordenado en celdas, con filas y columnas.
Una celda puede contener una recompensa (valor negativo) o un castigo (valor positivo).
Un obstáculo estará codificado con el valor -100.
En cuanto el robot pasa sobre la celda, recolecta y remueve el elemento que contiene.
'''
import random
import agentpy as ap
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.colors as mcolors
import json
from robot import Robot

class Habitacion:
    def __init__(self, model, filas, columnas):
        self.model = model
        self.filas = filas
        self.columnas = columnas
        self.environment = ap.Grid(self.model, (self.filas, self.columnas), track_empty=True, torus=False)
        self.total_recompensas = 0
        self.total_castigos = 0
        self.celda_valores = {}
        self.configurar_entorno()
        self.estaciones_ocupadas = {
            (0, 0): False,
            (0, columnas - 1): False,
            (filas - 1, 0): False,
            (filas - 1, columnas - 1): False
        }        
        self.movimientos = {
            "BFS": lambda agent: agent.use_BFS(),
            "DFS": lambda agent: agent.use_DFS(),
            "A*": lambda agent: agent.use_A_star()
        }

    def estacion_esta_ocupada(self, posicion):
        return self.estaciones_ocupadas.get(posicion, False)

    def marcar_estacion_ocupada(self, posicion):
        if posicion in self.estaciones_ocupadas:
            self.estaciones_ocupadas[posicion] = True

    def liberar_estacion(self, posicion):
        if posicion in self.estaciones_ocupadas:
            self.estaciones_ocupadas[posicion] = False


    def configurar_entorno(self):
        esquinas = [(0, 0), (0, self.columnas - 1), (self.filas - 1, 0), (self.filas - 1, self.columnas - 1)]
        for fila in range(self.filas):
            for columna in range(self.columnas):
                if (fila, columna) in esquinas:
                    self.celda_valores[(fila, columna)] = 100  
                else:
                    self.celda_valores[(fila, columna)] = self.generar_valor(self.model.p.probabilidad_no_valor)

    def colocar_robots(self, robots):
        posiciones_libres = [pos for pos, valor in self.celda_valores.items() if valor != -1000]
        for robot in robots:
            posicion_inicial = random.choice(posiciones_libres)
            posiciones_libres.remove(posicion_inicial)
            robot.posicion = posicion_inicial
            self.environment.add_agents([robot], positions=[robot.posicion])

    def generar_valor(self, probabilidad_no_valor):
        if random.random() < probabilidad_no_valor:
            return 0
        else:
            return 10 if random.uniform(-1, 1) > 0 else -1000

    def actualizar_grafica(self):
        plt.clf()

        color_map = {
            100: 1,         # Centro de carga (azul)
            10: 2,          # Recompensa (verde)
            -1000: 3,       # Castigo (rojo)
            0: 0            # Sin valor (gris)
        }

        environment_array = np.zeros((self.filas, self.columnas))
        for fila in range(self.filas):
            for columna in range(self.columnas):
                valor_celda = self.celda_valores.get((fila, columna), 0)
                environment_array[fila, columna] = color_map.get(valor_celda, 0)  
        cmap = mcolors.ListedColormap(["gray", "blue", "green", "red"])
        plt.imshow(environment_array, cmap=cmap, interpolation='nearest')

        for robot in self.model.robots:
            fila, columna = robot.posicion
            plt.scatter(columna, fila, color='yellow', edgecolor='black', s=100)  

        plt.title("Ejecucion de Agentpy")
        plt.pause(0.001)


    