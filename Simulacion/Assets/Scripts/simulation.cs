using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using SimpleJSON;

public class simulation : MonoBehaviour
{

    List<List<Vector3>> positions;
    public GameObject Agent1;
    public GameObject Agent2;
    public GameObject Agent3;
    public GameObject Agent4;
    public int car_num;
    public int step;
    public int stepcount;
    public GameObject instance;
    public string json;
    public float delay;
    public float timer;
    List<GameObject> agents = new List<GameObject>();
    [System.Serializable]
    public class Car
    {
        public int id;
        public string model;
        public bool horizontal;
        public int[] pos;
    }

    public List<Car> cars = new List<Car>();
    // Start is called before the first frame update
    void Start()
    {
        json = File.ReadAllText(Application.dataPath + "/scripts/data.json");
        var data = JSON.Parse(json);
        car_num = data["metadata"]["cars"];
        delay = 0.1f;
        stepcount = data["metadata"]["steps"];
        var manifest = data["manifest"];
        var records = data["records"];
        timer = delay;
        step = 0;
        // Take positions of first step to instantiate cars
        var initial = records[0];
        for (int i = 0; i < car_num; i++)
        {
            int id = manifest[i]["id"];
            String model = manifest[i]["model"];
            bool horizontal = manifest[i]["horizontal"];
            //    Assign default as failsafe
            GameObject prefab = Agent1;
            switch (model)
            {
                case "bus":
                    prefab = Agent1;
                    break;
                case "car":
                    prefab = Agent2;
                    break;
                case "truck":
                    prefab = Agent3;
                    break;
                default:
                    Debug.Log("ERROR; Bad model string");
                    prefab = Agent1;
                    break;
            }
            int x = initial[i]["pos"][1]*2;
            int z = initial[i]["pos"][0]*2;
            Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            Debug.Log(horizontal);
           
            instance = Instantiate(prefab, new Vector3(x, 0.0f, z), Quaternion.identity);
            if (horizontal) 
              instance.transform.Rotate(0, 90, 0);
            agents.Add(instance);
        }
    }

    // Update is called once per frame
    void Update()
    {
        var data = JSON.Parse(json);
        car_num = data["metadata"]["cars"];
        var records = data["records"];
        timer -= Time.fixedDeltaTime;
        if (timer < 0)
        {
            timer = delay;
            if (step < stepcount - 1)
            {
                step += 1;
                int x, z;
                // Iterate over every agent both in json data and unity counterpart
                for (int i = 0; i < car_num; i++)
                {
                    int speed = 1;
                    x = records[step][i]["pos"][1]*2;
                    z = records[step][i]["pos"][0]*2;
                    Vector3 newpos = new Vector3(x, 0.0f, z);
                    Vector3 pos = agents[i].transform.position;
                    agents[i].transform.position = Vector3.MoveTowards(pos, newpos, speed);
                    //agents[i].transform.position = newpos;
                    Debug.Log("Pos:" + agents[i].transform.position + " id: " + records[step][i]["id"]);
                }
            }
        }


    }

}

