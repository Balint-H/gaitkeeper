using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mujoco;

namespace GaitLab
{

    public class GraphOSL : MonoBehaviour
    {
        [SerializeField]
        ActuatorOSL osl;

        float ankleAngle;

        float desiredTheta;

        float torque;

        float phase;

        private void FixedUpdate()
        {
            ankleAngle = (float)osl.AnkleAngle;
            desiredTheta = (float)osl.ActiveBehaviour.theta;
            torque = (float)osl.Torque;
            phase = ((float)osl.phase-2) * 15;
        }
    }
}