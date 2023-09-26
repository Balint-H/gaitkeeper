using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Mujoco;
using System.Linq;

public class AdaptiveProsthesisObservations : ObservationSource
{
    public override int Size => 3 * vectorSensors.Count + jointSensors.Count;

    [SerializeField]
    List<MjSiteVectorSensor> vectorSensors;

    [SerializeField]
    List<MjJointScalarSensor> jointSensors;

    public override void FeedObservationsToSensor(VectorSensor sensor)
    {
        foreach(var s in vectorSensors)
        {
            sensor.AddObservation(s.SensorReading);
        }

        foreach (var s in jointSensors)
        {
            sensor.AddObservation((float)s.SensorReading);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnAgentStart()
    {

    }
}
