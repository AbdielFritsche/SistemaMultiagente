'''
Agente: Robot con capacidades de movimiento de acuerdo a las siguientes reglas:

Puede obtener la información de cada celda en la habitación utilizando un sistema de coordenadas (fila, columna).
Tarda 1 unidad de tiempo para viajar de una celda a cualquier celda vecina (arriba, abajo, derecha o izquierda).
Gasta 1 unidad de energía para viajar de una celda a cualquier celda vecina.
Su energía se modifica de acuerdo a lo valores dados en cada celda.
Detiene su ejecución si ya no hay celdas con recompensas o si se agota su energía.
Evita obstáculos  (al colisionar con un obstáculo, automicamente debería detener su marcha por falta de energía).
Si concluye la recolección de recompensas viaja a una celda objetivo (e.g., estación de carga) predeterminada.
'''

import agentpy as ap
from collections import deque
import heapq

class Robot(ap.Agent):

    def setup(self):
        self.energia = self.p.energia_inicial
        self.intentos_de_regreso = 0
        self.estado = "activo"  

        print(f"El robot tiene energía {self.energia} y está listo para comenzar.")

        self.recompensas_recogidas = 0  
        self.castigos_recogidos = 0 
 
    def step(self):
        if self.energia < 1:
            self.estado = "detenido"
            return  

        hay_recompensas = any((valor > 0 and valor != 100) for valor in self.model.habitacion.celda_valores.values())

        if not hay_recompensas:
            if self.posicion != (0, 0):
                _, ruta = self.movimiento_A_star((0, 0))
                self.mover_hacia_objetivo(ruta)
            else:
                self.estado = "detenido"  
            return

        movimiento_func = self.model.habitacion.movimientos.get(self.model.tipo_movimiento)
        if movimiento_func:
            destino, ruta = movimiento_func(self)
            if destino:
                self.mover_hacia_objetivo(ruta)

    def celda_esta_ocupada(self, posicion):
        """ Verifica si la posición está ocupada por otro robot. """
        for otro_robot in self.model.robots:
            if otro_robot is not self and otro_robot.posicion == posicion:
                return True
        return False

    def movimiento_BFS(self):
        filas, columnas = self.model.habitacion.filas, self.model.habitacion.columnas
        start = self.posicion
        queue = deque([(start, [])]) 
        visitados = set([start])  
        esquinas = [(0, 0), (0, columnas - 1), (filas - 1, 0), (filas - 1, columnas - 1)]

        while queue:
            (fila, columna), ruta = queue.popleft()

            # Verificar si la posición actual es una recompensa y no está en una esquina
            if (fila, columna) not in esquinas and self.model.habitacion.celda_valores.get((fila, columna), 0) > 0:
                print(f"Ruta encontrada a recompensa en {fila, columna} con ruta: {ruta + [(fila, columna)]}")
                return (fila, columna), ruta + [(fila, columna)]

            for delta_fila, delta_columna in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                nueva_fila, nueva_columna = fila + delta_fila, columna + delta_columna
                nueva_posicion = (nueva_fila, nueva_columna)
                valor_celda = self.model.habitacion.celda_valores.get(nueva_posicion, 0)
                
                # Si la celda es un castigo, registrar y continuar sin incluirla en la ruta
                if valor_celda == -1000:
                    print(f"Celda de castigo detectada en {nueva_posicion}, evitando esta ruta.")
                    continue

                # Verificar celdas válidas, libres de ocupación y sin castigos para la búsqueda
                if (0 <= nueva_fila < filas and 0 <= nueva_columna < columnas and
                    nueva_posicion not in visitados and valor_celda >= 0 and
                    not self.celda_esta_ocupada(nueva_posicion)):

                    visitados.add(nueva_posicion)
                    queue.append((nueva_posicion, ruta + [(fila, columna)]))

        print("No se encontró una recompensa accesible sin castigos.")
        return None, []
   

    def movimiento_DFS(self):
        filas, columnas = self.model.habitacion.filas, self.model.habitacion.columnas
        start = self.posicion
        stack = [(start, [])]  
        visitados = set([start])  
        esquinas = [(0, 0), (0, columnas - 1), (filas - 1, 0), (filas - 1, columnas - 1)]

        while stack:
            (fila, columna), ruta = stack.pop()

            # Verificar si la posición actual es una recompensa y no está en una esquina
            if (fila, columna) not in esquinas and self.model.habitacion.celda_valores.get((fila, columna), 0) > 0:
                print(f"Ruta encontrada a recompensa en {fila, columna} con ruta: {ruta + [(fila, columna)]}")
                return (fila, columna), ruta + [(fila, columna)]

            for delta_fila, delta_columna in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                nueva_fila, nueva_columna = fila + delta_fila, columna + delta_columna
                nueva_posicion = (nueva_fila, nueva_columna)
                valor_celda = self.model.habitacion.celda_valores.get(nueva_posicion, 0)

                # Ignorar celdas con valor de castigo y registrar en el log
                if valor_celda == -1000:
                    print(f"Celda de castigo detectada en {nueva_posicion}, evitando esta ruta.")
                    continue

                # Verificar celdas válidas, libres de ocupación y sin castigos para la búsqueda
                if (0 <= nueva_fila < filas and 0 <= nueva_columna < columnas and
                    nueva_posicion not in visitados and valor_celda >= 0 and
                    not self.celda_esta_ocupada(nueva_posicion)):
                    
                    visitados.add(nueva_posicion)
                    stack.append((nueva_posicion, ruta + [(fila, columna)]))

        print("No se encontró una recompensa accesible sin castigos.")
        return None, []

    def movimiento_A_star(self, objetivo=None, hacia_estacion=False):
        filas, columnas = self.model.habitacion.filas, self.model.habitacion.columnas
        start = self.posicion
        queue = [(0, start, [])]  
        visitados = {start: 0}  

        esquinas = [(0, 0), (0, columnas - 1), (filas - 1, 0), (filas - 1, columnas - 1)]

        def heuristica(pos):
            fila, columna = pos
            if objetivo:
                return abs(fila - objetivo[0]) + abs(columna - objetivo[1])
            else:
                min_distancia = float('inf')
                for (f, c), valor in self.model.habitacion.celda_valores.items():
                    if valor > 0 and (f, c) not in esquinas:  
                        distancia = abs(fila - f) + abs(columna - c)
                        min_distancia = min(min_distancia, distancia)
                return min_distancia

        while queue:
            costo_actual, (fila, columna), ruta = heapq.heappop(queue)

            # Condición de éxito: alcanzar el objetivo o una recompensa (si no va a una estación)
            if objetivo:
                if (fila, columna) == objetivo:
                    return (fila, columna), ruta + [(fila, columna)]
            else:
                if not hacia_estacion and self.model.habitacion.celda_valores.get((fila, columna), 0) > 0 and (fila, columna) not in esquinas:
                    return (fila, columna), ruta + [(fila, columna)]

            for delta_fila, delta_columna in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                nueva_fila, nueva_columna = fila + delta_fila, columna + delta_columna
                nueva_posicion = (nueva_fila, nueva_columna)
                nuevo_costo = costo_actual + 1

                if 0 <= nueva_fila < filas and 0 <= nueva_columna < columnas:
                    valor_celda = self.model.habitacion.celda_valores.get(nueva_posicion, 0)

                    # Ignorar celdas de castigo sólo si no está en modo hacia_estacion
                    if valor_celda == -1000 and not hacia_estacion:
                        print(f"Celda de castigo detectada en {nueva_posicion}, evitando esta ruta.")
                        continue

                    # Agregar sólo celdas válidas y no ocupadas al camino
                    if (valor_celda >= 0 or hacia_estacion) and not self.celda_esta_ocupada(nueva_posicion) and (
                        nueva_posicion not in visitados or nuevo_costo < visitados[nueva_posicion]
                    ):
                        visitados[nueva_posicion] = nuevo_costo
                        prioridad = nuevo_costo + heuristica(nueva_posicion)
                        heapq.heappush(queue, (prioridad, nueva_posicion, ruta + [(fila, columna)]))

        print("No se encontró una ruta accesible.")
        return None, []

    def use_BFS(self):
        if self.estado == "detenido" or not self.verificar_energia_y_regresar_estacion():  
            return None, []  
            
        destino, ruta = self.movimiento_BFS()
        return destino, ruta 

    def use_DFS(self):
        if self.estado == "detenido" or not self.verificar_energia_y_regresar_estacion():  
            return None, []  
        
        destino, ruta = self.movimiento_DFS()
        return destino, ruta 

    def use_A_star(self):
        if self.estado == "detenido" or not self.verificar_energia_y_regresar_estacion():  
            return None, []  
        
        destino, ruta = self.movimiento_A_star()
        return destino, ruta 


    def mover_hacia_objetivo(self, ruta):
        """Mueve el robot a lo largo de la ruta hacia el objetivo sin detenerse en cada paso."""
        for paso in ruta:
            if self.energia < 1:
                print("Energía agotada antes de llegar al objetivo.")
                return False 
            self.actualizar_posicion(paso)
        return True  

    def verificar_energia_y_regresar_estacion(self):
        # Lista de estaciones de carga (esquinas)
        estaciones = [(0, 0), (0, self.model.habitacion.columnas - 1), 
                    (self.model.habitacion.filas - 1, 0), 
                    (self.model.habitacion.filas - 1, self.model.habitacion.columnas - 1)]
        
        # Encuentra la estación más cercana que no esté ocupada
        estacion_disponible = None
        ruta_estacion_mas_cercana = []
        distancia_estacion_mas_cercana = float('inf')

        for estacion in estaciones:
            if not self.model.habitacion.estacion_esta_ocupada(estacion):
                destino_estacion, ruta_estacion = self.movimiento_A_star(estacion, hacia_estacion=True)
                distancia_estacion = len(ruta_estacion) if destino_estacion else float('inf')
                
                if distancia_estacion < distancia_estacion_mas_cercana:
                    distancia_estacion_mas_cercana = distancia_estacion
                    ruta_estacion_mas_cercana = ruta_estacion
                    estacion_disponible = estacion

        # Verificar el destino de la recompensa y calcular la ruta
        destino_recompensa, ruta_recompensa = self.movimiento_A_star()
        distancia_recompensa = len(ruta_recompensa) if destino_recompensa else float('inf')

        # Control de la energía restante y la ruta a seguir
        if estacion_disponible and self.energia >= distancia_estacion_mas_cercana:
            print("Regresando a la estación de carga más cercana disponible.")
            self.model.habitacion.marcar_estacion_ocupada(estacion_disponible)  # Marcar como ocupada
            if self.mover_hacia_objetivo(ruta_estacion_mas_cercana):
                return True

        elif destino_recompensa and self.energia >= distancia_recompensa:
            print("Dirigiéndose a la recompensa más cercana.")
            if self.mover_hacia_objetivo(ruta_recompensa):
                return True

        print("No se encontró una ruta accesible. Deteniendo el robot.")
        self.estado = "detenido"
        return False


    def actualizar_posicion(self, nueva_posicion):
        fila, columna = nueva_posicion
        valor = self.model.habitacion.celda_valores.get((fila, columna), 0)

        if self.celda_esta_ocupada(nueva_posicion):
            print(f"El robot en {self.posicion} espera porque {nueva_posicion} está ocupada.")
            return False  # No se mueve y espera

        self.energia -= 1

        # Lista de posiciones de estaciones de carga (esquinas)
        estaciones = [(0, 0), (0, self.model.habitacion.columnas - 1), 
                    (self.model.habitacion.filas - 1, 0), 
                    (self.model.habitacion.filas - 1, self.model.habitacion.columnas - 1)]
        
        # Manejar la recarga en las estaciones de carga y la ocupación
        if nueva_posicion in estaciones:
            self.model.habitacion.marcar_estacion_ocupada(nueva_posicion)  # Marcar como ocupada al llegar
            energia_recarga = self.p.energia_inicial * 2
            self.energia = min(self.energia + energia_recarga, self.p.energia_inicial * 2)
            print(f"Robot ha recargado energía en la estación a {self.energia}.")

            # Liberar la estación anterior si el robot está en una estación
            if self.posicion in estaciones:
                self.model.habitacion.liberar_estacion(self.posicion)

        # Si no es una estación, procede con el movimiento y ajustes de energía
        elif valor is not None and valor != 100:
            self.energia += valor
            if valor < 0:
                self.castigos_recogidos += 1
            elif valor > 0:
                self.recompensas_recogidas += 1

            self.model.habitacion.celda_valores[(fila, columna)] = 0
            print(f"Robot se mueve a {nueva_posicion}, recoge valor {valor}, energía restante: {self.energia}")
            print(f"Recompensas recogidas: {self.recompensas_recogidas}, Castigos recogidos: {self.castigos_recogidos}")
        
        # Actualizar el entorno y posición
        self.model.habitacion.environment.move_to(self, nueva_posicion)
        self.posicion = nueva_posicion
        self.model.habitacion.actualizar_grafica()
        return True  # Movimiento exitoso




