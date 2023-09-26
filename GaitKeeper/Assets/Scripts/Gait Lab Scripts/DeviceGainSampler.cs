using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mujoco;
using ModularAgents.MotorControl;

namespace GaitLab
{
    public class DeviceGainSampler : TrainingEventHandler
    {
        [SerializeField]
        AdaptiveProsthesisActuator adaptiveProsthesis;

        [SerializeField]
        ValueRecorder recorder;

        public override EventHandler Handler => (object sender, EventArgs e) =>
        {
            recorder.Record((float)adaptiveProsthesis.PosGain, "posGain");
            recorder.Record((float)adaptiveProsthesis.VelGain, "velGain");
        };
        
    }
}