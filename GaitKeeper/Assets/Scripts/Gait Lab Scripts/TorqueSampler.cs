using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mujoco;

namespace GaitLab
{
    public class TorqueSampler : TrainingEventHandler
    {
        public override EventHandler Handler => CollectPositions;

        [SerializeField]
        List<MjHingeJoint> trackedHingeJoints;

        [SerializeField]
        List<MjBallJoint> trackedBallJoints;

        [SerializeField]
        ValueRecorder recorder;


        unsafe void CollectPositions(object sender, EventArgs e)
        {
            var data = MjScene.Instance.Data;
            if (!recorder) return;
            foreach (var hj in trackedHingeJoints)
            {
                recorder.Record((float)data->qfrc_applied[hj.DofAddress], hj.name+"_torque");
            }

            foreach (var bj in trackedBallJoints)
            {
                recorder.Record(MjEngineTool.UnityVector3(data->qfrc_applied+ bj.DofAddress), bj.name + "_torque");
            }
        }
    }
}