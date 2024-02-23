using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Linq;
using MotionMatch;

public class DReConDirectionObservation : ObservationSource
{
    public override int Size => 2;

    [SerializeField]
    MMController motionSynthesizer;

    [SerializeField]
    private GameObject inputObject;
    private IMMInput inputTrajectorySource;

    public override void FeedObservationsToSensor(VectorSensor sensor)
    {
        RootProjection curRefFrame = motionSynthesizer.CurrentReferenceFrame;
        var inputFeatures = curRefFrame.InverseTransform(inputTrajectorySource.AnalogueDirection);
        sensor.AddObservation(inputFeatures);

        
    }

    public override void OnAgentStart()
    {
        inputTrajectorySource = inputObject.GetComponent<IMMInput>();
    }


}
