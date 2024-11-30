# SistemaMultiagente  
**Proyecto de Simulación de Movilidad Urbana: Calle en Cruz**

## Descripción

Este proyecto tiene como objetivo modelar y optimizar el flujo de tráfico y de peatones en una calle en forma de cruz, diseñada para simular intersecciones similares de diferentes ciudades. Utilizando simulación multiagente, buscamos analizar y proponer mejoras en la infraestructura y en la gestión de tráfico para reducir los riesgos de accidentes, mejorar la accesibilidad y optimizar la fluidez del tránsito vehicular y peatonal.

La simulación proporciona un entorno interactivo donde los usuarios pueden observar en tiempo real el comportamiento y la interacción de los agentes, incluidos semáforos, coches y peatones. 

## Tecnologías Utilizadas

- **Unity**: Plataforma de desarrollo de simulaciones 3D utilizada para modelar el entorno y generar el entorno visual.
- **Python**: Lenguaje principal para la implementación de la lógica de simulación multiagente.
- **AgentPy**: Librería de Python para modelar y simular sistemas multiagente.
- **Flask**: Framework utilizado para desarrollar la API que conecta los módulos del sistema.
- **Git**: Control de versiones para la colaboración y manejo de cambios en el código.

## Archivos Ejecutables

- **`final.py`**: Archivo principal que ejecuta la simulación de movilidad urbana. Proporciona un entorno en vivo donde los agentes (semaforización, vehículos y peatones) interactúan de manera autónoma según los parámetros establecidos.

  **Instrucciones para la ejecución**:
  1. Ejecuta el archivo `api.py` para iniciar la API, que conecta los módulos del sistema:
     ```bash
     python api.py
     ```
  2. Luego, ejecuta el archivo `final.py` para iniciar la simulación de los agentes:
     ```bash
     python final.py
     ```
  3. Una vez iniciados `api.py` y `final.py`, abre Unity y carga el entorno de simulación para visualizar el tráfico y la interacción de los agentes en tiempo real.

- **`api.py`**: Archivo que ejecuta la API desarrollada con Flask, permitiendo que los datos y configuraciones sean accesibles para la interacción del sistema.

  Ejecución:
  ```bash
  python api.py
