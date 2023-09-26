using ModularAgents.Kinematic;
using ModularAgents.Kinematic.Mujoco;
using Mujoco;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace GaitLab
{
    public class WalkingSpeedSampler : TrainingEventHandler
    {
        public override EventHandler Handler => CollectSpeed;

        [SerializeField]
        List<Transform> trackedTransforms;

        IReadOnlyList<IKinematic> trackedKinematics;

        [SerializeField]
        ValueRecorder recorder;


        unsafe void CollectSpeed(object sender, EventArgs e)
        {
            if (!recorder)
            {
                foreach (var k in trackedKinematics)
                {
                    Debug.Log($"{k.Name}_Speed: {k.Velocity.magnitude}");
                }
                return;
            }
            foreach (var k in trackedKinematics)
            {
                recorder.Record(k.Velocity.magnitude, $"{k.Name}_Speed");
            }
        }

        private void MjInitialize()
        {
            trackedKinematics = trackedTransforms.Select(t => MjKinematicExtensions.GetIKinematic(t)).ToList();
        }

        private unsafe void Start()
        {
            MjInitialize();
        }
    }
}