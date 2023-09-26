using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ModularAgents.MotorControl;

namespace GaitLab
{
    public class MultiDofDeviceGainSampler : TrainingEventHandler
    {
        [SerializeField]
        MultiDofProsthesisActuator adaptiveProsthesis;

        [SerializeField]
        ValueRecorder recorder;

        public override EventHandler Handler => (object sender, EventArgs e) =>
        {
            recorder.Record((float)adaptiveProsthesis.theta, "theta");
            recorder.Record((float)adaptiveProsthesis.stiffness, "posGain");
            recorder.Record((float)adaptiveProsthesis.damping, "velGain");
        };
        
    }
}