using Mujoco;
using System;
using ModularAgents.MotorControl;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GaitLab
{
    public class JointAngleSampler : TrainingEventHandler
    {
        public override EventHandler Handler => CollectJointAngles;

        [SerializeField]
        List<MjBaseJoint> trackedJoints;
        List<AngleJoint> exposedAngles;

        [SerializeField]
        ValueRecorder recorder;

        void Awake()
        {
            exposedAngles = trackedJoints.Select(j => new AngleJoint(j)).ToList();
        }

        unsafe void CollectJointAngles(object sender, EventArgs e)
        {
            if (!recorder) return;
            foreach(var aj in exposedAngles)
            {
                recorder.Record(aj.Angle, aj.Name);
            }
        }

        class AngleJoint
        {
            IMjJointState jointState;

            public string Name => jointState.Name;

            public AngleJoint(MjBaseJoint joint)
            {
                this.jointState = IMjJointState.GetJointState(joint);
            }

            public float Angle
            {
                get
                {
                    double[] position = jointState.Positions;
                    switch(jointState)
                    {
                        case HingeState:
                            return (float)position[0];
                            
                        case BallState:
                            return (float)(2 * System.Math.Acos(position[0]));
                            
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }
    }
}