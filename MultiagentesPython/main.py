#Programa creado por Abdiel Fritsche Barajas A01234933
#Fecha de creación: 7 de Noviembre de 2024
from simulacion import Simulacion, plt

tipos_movimiento = ["BFS",  "DFS", "A*"]
resultados = []

for movimiento in tipos_movimiento:
    parametros = {
        "filas": 50,
        "columnas": 50,
        "num_agentes": 4,
        "probabilidad_no_valor": 0.85,
        "energia_inicial": 10,
        "tipo_movimiento": movimiento
    }
    
    modelo = Simulacion(parametros)
    resultado = modelo.run()
    
    recompensas = resultado.reporters["recompensas"][0]
    castigos = resultado.reporters["castigos"][0]
    energia_restante = resultado.reporters["energia_restante"][0]
    total_recompensas = modelo.habitacion.total_recompensas  
    total_castigos = modelo.habitacion.total_castigos       
    
    proporcion_recompensas = recompensas / total_recompensas if total_recompensas > 0 else 0
    proporcion_castigos = castigos / total_castigos if total_castigos > 0 else 0
    
    resultados.append({
        "tipo_movimiento": movimiento,
        "recompensas": recompensas,
        "castigos": castigos,
        "energia_restante": energia_restante,
        "proporcion_recompensas": proporcion_recompensas,
        "proporcion_castigos": proporcion_castigos
    })

print(resultados)

tipos_movimiento = [r["tipo_movimiento"] for r in resultados]
proporcion_recompensas = [r["proporcion_recompensas"] for r in resultados]
proporcion_castigos = [r["proporcion_castigos"] for r in resultados]
energia_restante = [r["energia_restante"] for r in resultados]

fig, axs = plt.subplots(3, 1, figsize=(10, 15))

axs[0].bar(tipos_movimiento, proporcion_recompensas, color='skyblue')
axs[0].set_xlabel("Tipo de Movimiento")
axs[0].set_ylabel("Proporción de Recompensas Recogidas")
axs[0].set_title("Proporción de Recompensas Recogidas por Tipo de Movimiento")

axs[1].bar(tipos_movimiento, proporcion_castigos, color='salmon')
axs[1].set_xlabel("Tipo de Movimiento")
axs[1].set_ylabel("Proporción de Castigos Recogidos")
axs[1].set_title("Proporción de Castigos Recogidos por Tipo de Movimiento")

axs[2].bar(tipos_movimiento, energia_restante, color='lightgreen')
axs[2].set_xlabel("Tipo de Movimiento")
axs[2].set_ylabel("Energía Restante")
axs[2].set_title("Energía Restante del Robot por Tipo de Movimiento")

plt.tight_layout()
plt.show()


