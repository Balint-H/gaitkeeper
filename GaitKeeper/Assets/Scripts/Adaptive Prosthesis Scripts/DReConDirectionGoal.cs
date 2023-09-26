using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class DReConDirectionGoal : SensorComponent
{
    // Start is called before the first frame update
    [SerializeField]
    GameObject inputObject;

    [SerializeField]
    MMController motionMatchingController;

    ISensor sensorObject;


    private void Awake()
    {
        
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override ISensor[] CreateSensors()
    {
        sensorObject = new DirectionSensor(inputObject.GetComponent<IMMInput>(), motionMatchingController);
        return new[] { sensorObject };
    }

    class DirectionSensor : ISensor
    {
        IMMInput inputComponent;
        MMController motionMatchingController;
        ObservationSpec observationSpec;

        public DirectionSensor(IMMInput input, MMController controller)
        {
            inputComponent = input;
            motionMatchingController = controller;
            observationSpec = new ObservationSpec(shape: new InplaceArray<int>(2),  dimensionProperties: new InplaceArray<DimensionProperty>(DimensionProperty.None), observationType: ObservationType.GoalSignal);
        }

        public byte[] GetCompressedObservation()
        {
            throw new NotImplementedException();
        }

        public CompressionSpec GetCompressionSpec()
        {
            return CompressionSpec.Default();
        }

        public string GetName()
        {
            return motionMatchingController.name;
        }

        public ObservationSpec GetObservationSpec()
        {
            return observationSpec;
        }

        public void Reset()
        {
        }

        public void Update()
        {
        }

        public int Write(ObservationWriter writer)
        {
            var observationVector = motionMatchingController.CurrentReferenceFrame.InverseTransform(inputComponent.AnalogueDirection);
            writer[0] = observationVector.x;
            writer[1] = observationVector.y;
            return 2;
        }
    }
}
