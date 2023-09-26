using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Linq;

public class DReConIntentObservation : ObservationSource
{
    public override int Size => 12;

    [SerializeField]
    MMController motionSynthesizer;

    [SerializeField]
    private GameObject inputObject;
    private IMMInput inputTrajectorySource;

    public override void FeedObservationsToSensor(VectorSensor sensor)
    {
        RootProjection curRefFrame = motionSynthesizer.CurrentReferenceFrame;
        var inputFeatures = inputTrajectorySource.CurrentTrajectoryAndDirection.Select(v => curRefFrame.InverseTransform(v));
        foreach (var vec in inputFeatures)
        {
            sensor.AddObservation(vec);
        }
        
    }

    public override void OnAgentStart()
    {
        inputTrajectorySource = inputObject.GetComponent<IMMInput>();
    }


}
