using Mujoco;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorPrinter : MonoBehaviour
{

    [SerializeField]
    MjSiteVectorSensor sensor;
    // Start is called before the first frame update
    void FixedUpdate()
    {
        Debug.Log(sensor.SensorReading);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
