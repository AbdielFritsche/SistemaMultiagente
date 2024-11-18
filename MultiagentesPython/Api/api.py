from flask import Flask, jsonify
from threading import Thread

app = Flask(__name__)
current_model_data = {}

@app.route('/positions', methods=['GET'])
def send_positions():
    """Envía las posiciones actuales del modelo a Unity."""
    return jsonify(current_model_data)

def run_server():
    """Ejecuta el servidor Flask."""
    app.run(host='0.0.0.0', port=5000)

def update_model_data(data):
    """Actualiza los datos actuales del modelo para la API."""
    global current_model_data
    current_model_data = data