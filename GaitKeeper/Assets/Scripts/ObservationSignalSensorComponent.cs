using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Linq;
using Unity.Barracuda;
using Mujoco;

/// <summary>
/// Deprecated. A class for grouping multiple the observations from observation sources into one sensor. 
/// This class will be removed in a future release, used only for backwards compatibility with existing policies.
/// This will be deprecated because it hides the names of the individual observation sources, making debugging/analysis harder on the python side
/// </summary>
public class ObservationSignalSensorComponent : SensorComponent
{
    [SerializeField]
    private List<ObservationSource> observationSources;

    [SerializeField, Range(1, 100)]
    private int numStackedObservations;

    ISensor sensor;

    public override ISensor[] CreateSensors()
    {
        foreach (var observation in observationSources) 
        { 
            observation.OnAgentStart();
        }
        sensor = new ObservationSignalSensor(observationSources, name + "_VectorSensor");
        return new[] { new StackingSensor(sensor, numStackedObservations) };
    }

    public void Initialize()
    {
        foreach (ObservationSource observationSource in observationSources.Where(obs => obs != null))
        {
            observationSource.OnAgentStart();
        }
    }

    private unsafe void Start()
    {
        if(!MjScene.InstanceExists || MjScene.Instance.Data == null) 
        {
            MjScene.Instance.postInitEvent += (_, _) => Initialize();
        }
        else
        {
            Initialize();
        }
    }
}

