using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Mujoco;

namespace GaitLab
{
    public class MjContactForceAggregator : TrainingEventHandler
    {
        public override EventHandler Handler => CollectContactForces;

        [SerializeField]
        List<MjSiteScalarSensor> TrackedSensor;

        float normalForce;

        [SerializeField]
        ValueRecorder recorder;

        public EventHandler FootFall;
        public EventHandler LiftOff;

        public double SensorReading { get=> TrackedSensor.Sum(s => (float)s.SensorReading); }

        [SerializeField]
        float contactForceLimit = 0.01f;


        // Start is called before the first frame update
        void Start()
        {
            if (recorder) recorder.AddColumn(name);
        }

        // Update is called once per frame
        unsafe void CollectContactForces(object sender, EventArgs e)
        {
            var newNormalForce = TrackedSensor.Sum(s => (float) s.SensorReading);
            if (normalForce < contactForceLimit && newNormalForce >= contactForceLimit) FootFall?.Invoke(this, EventArgs.Empty);
            if (normalForce >= contactForceLimit && newNormalForce < contactForceLimit) LiftOff?.Invoke(this, EventArgs.Empty);

            normalForce = newNormalForce;
            if (recorder) recorder.Record(normalForce, name);
            
        }

        unsafe private double[] GetContactVector(double* cfrc_ext, int id)
        {
            return Enumerable.Range(id * 6, 1).Select(i => cfrc_ext[i]).ToArray();
        }
    }
}