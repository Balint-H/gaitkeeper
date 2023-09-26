using System;
using ModularAgents;
using UnityEngine;
using ModularAgents.Kinematic.Mujoco;
using ModularAgents.Kinematic;
using Mujoco;
using System.Linq;
using ModularAgents.DReCon;

// Based on Watson et al. 2021; Use of the margin of stability to quantify stability in pathologic gait – a qualitative systematic review

namespace GaitLab
{
    public class StabilityMarginSampler : TrainingEventHandler
    {
        public override EventHandler Handler => CollectJointAngles;

        [SerializeField]
        private Transform root;
        private BodyChain subject;

        [SerializeField]
        MjBody rightToe;

        [SerializeField]
        MjBody leftToe;

        [SerializeField]
        ValueRecorder recorder;

        [SerializeField]
        float pendulumLength;

        [SerializeField]
        float gravity;


        private void Start()
        {
        }

        unsafe void CollectJointAngles(object sender, EventArgs e)
        {
            if (!recorder) return;
            recorder.Record(GetCurrentSM(), "MoS");

        }

        unsafe Vector2 GetCurrentSM()
        {
            if (subject == null) subject = new MjBodyChain(root);

            ModularAgents.DReCon.ReferenceFrame referenceFrame = new ModularAgents.DReCon.ReferenceFrame(subject.RootForward, subject.CenterOfMass);


            var vCom = Utils.Horizontal(subject.CenterOfMassVelocity);
            var com = Utils.Horizontal(subject.CenterOfMass);


            var rightBoS = Utils.Horizontal(rightToe.transform.position);
            var leftBoS = Utils.Horizontal(leftToe.transform.position);

            Vector2 boS;

            if (Vector2.Dot(rightBoS - com, vCom) > 0)
            {
                boS = rightBoS;
            }
            else
            {
                boS = leftBoS;
            }
            var xCom = com + vCom / Mathf.Sqrt(gravity / pendulumLength);

            return Utils.Horizontal(referenceFrame.WorldDirectionToCharacter((boS - xCom).ProjectTo3D()));


        }
    }
}