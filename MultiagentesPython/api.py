from flask import Flask, jsonify
import json
import os

app = Flask(__name__)

# Ruta al archivo JSON
JSON_FILE_PATH = os.path.join(os.path.dirname(__file__), '..', 'simulacion.json')

@app.route('/simulacion', methods=['GET'])
def get_simulacion_data():
    try:
        # Leer el archivo JSON
        with open(JSON_FILE_PATH, 'r') as file:
            data = json.load(file)
        
        # Retornar el JSON como respuesta
        return jsonify(data)
    except FileNotFoundError:
        return jsonify({"error": "Archivo simulacion.json no encontrado"}), 404
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=True)


##Emir te amo mucho gracias por todo
