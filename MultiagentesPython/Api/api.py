from flask import Flask, request, jsonify

app = Flask(__name__)

# Ruta para recibir actualizaciones de Unity.
@app.route('/update', methods=['POST'])
def update_simulation():
    data = request.json
    # Procesar datos enviados por Unity.
    return jsonify({"status": "updated"})

# Ruta para enviar estado de agentes a Unity.
@app.route('/state', methods=['GET'])
def send_state():
    # Simulación de datos.
    agents_state = [
        {"id": 1, "type": "car", "position": [1, 2]},
        {"id": 2, "type": "pedestrian", "position": [3, 4]},
    ]
    return jsonify(agents_state)

if __name__ == '__main__':
    app.run(port=5000)