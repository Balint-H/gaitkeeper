using GaitLab;
using ModularAgents;
using ModularAgents.MotorControl;
using Mujoco;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GRFSampler : TrainingEventHandler
{
    public override EventHandler Handler => CollectGrf;

    [SerializeField]
    GRFObservationSource grf;

    [SerializeField]
    ValueRecorder recorder;

    unsafe void CollectGrf(object sender, EventArgs e)
    {
        if (!recorder) return;
        (_, var vec) = grf.GetMeanGRF();
        recorder.Record(vec.magnitude, grf.name);
    }

}

