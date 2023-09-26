using System;
using UnityEngine;
using Mujoco;

namespace GaitLab
{
    public class OSLPhaseSampler : TrainingEventHandler
    {
        public override EventHandler Handler => CollectPhase;

        [SerializeField]
        ActuatorOSL sampledDevice;

        [SerializeField]
        ValueRecorder recorder;

        unsafe void CollectPhase(object sender, EventArgs e)
        {
            if (!recorder) return;
            recorder.Record((int)sampledDevice.phase, sampledDevice.name + "_phase");
            
        }
    }
}