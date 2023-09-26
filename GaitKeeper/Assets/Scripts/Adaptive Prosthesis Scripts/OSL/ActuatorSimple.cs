using Mujoco.Extensions;
using System;
using UnityEngine;
using Mujoco;

namespace GaitLab
{
    public class ActuatorSimple : MonoBehaviour
    {

        [Serializable]
        public enum GaitPhase
        {
            Static
        }

        [SerializeField]
        private GaitPhase phase;

        public GaitPhaseBehaviour ActiveBehaviour
        {
            get
            {
                switch (phase)
                {
                    case GaitPhase.Static:
                        return staticBehaviour;

                    default:
                        return staticBehaviour;
                }
            }

        }


        public double AnkleAngle { get; private set; }

        public double Torque { get; private set; }
        private double AnkleVelocity { get; set; }

        [SerializeField]
        private double bodyMass;

        [SerializeField]
        private double ankleAngleOffset;

        private int inertiaMatrixIndex;


        [SerializeField]
        private StaticBehaviour staticBehaviour;

        [SerializeField]
        double maxForce;

        [SerializeField]
        protected MjHingeJoint ankleJoint;
        [SerializeField]
        protected MjHingeJoint kneeJoint;
        private readonly double kneeVelocity;

        [SerializeField]
        private MjContactForceAggregator forceSensor;

        unsafe private void Awake()
        {
            MjScene.Instance.ctrlCallback += UpdateTorque;
            inertiaMatrixIndex = -1;

            staticBehaviour.device = this;

        }

        unsafe private void UpdateTorque(object sender, MjStepArgs e)
        {

            GaitPhaseBehaviour curBehaviour = ActiveBehaviour;

            if (inertiaMatrixIndex == -1) inertiaMatrixIndex = MjState.GetInertiaMatrixIndex(ankleJoint.DofAddress, e);




            AnkleAngle =  ((e.data->qpos[ankleJoint.QposAddress]) * Mathf.Rad2Deg + ankleAngleOffset);
            AnkleVelocity =  (e.data->qvel[ankleJoint.DofAddress]) * Mathf.Rad2Deg;



            /*            var force = Math.Clamp(curBehaviour.k*posError - curBehaviour.b*AnkleVelocity, -maxForce, maxForce) * Mathf.Deg2Rad;
                        Torque = force;*/

            var posError =(curBehaviour.theta - AnkleAngle - Time.fixedDeltaTime * AnkleVelocity) * Mathf.Deg2Rad;
            var velError = (-curBehaviour.b * AnkleVelocity) * Mathf.Deg2Rad;

            double M = MjState.GetInertiaWithMatrixIndex(inertiaMatrixIndex, e);
            double biasForce = e.data->qfrc_bias[ankleJoint.DofAddress];
            double passiveForce = e.data->qfrc_passive[ankleJoint.DofAddress];

            double force = Math.Clamp(SingleSPD(posError, velError, curBehaviour.k, curBehaviour.b, biasForce, M, Time.fixedDeltaTime), -maxForce, maxForce);
            Torque =  force;

            //print($"behav: {curBehaviour.phase}, b:{curBehaviour.b}, theta:{curBehaviour.theta}, k:{curBehaviour.k}");

            e.data->qfrc_applied[ankleJoint.DofAddress] += force;
        }

     
        public abstract class GaitPhaseBehaviour
        {
            public abstract GaitPhase phase { get; }
            public abstract double k { get; }
            public abstract double b { get; }
            public abstract double theta { get; }

            [HideInInspector]
            public ActuatorSimple device; //for sensing


            public GaitPhaseBehaviour(ActuatorSimple device)
            {
                this.device = device;
            }

            public abstract void OnEnterState();

            public abstract bool ShouldTransition(); //use device state to determine if condition is satisfied
        }

        [Serializable]
        private class StaticBehaviour : GaitPhaseBehaviour
        {
            public override GaitPhase phase => GaitPhase.Static;

            [SerializeField]
            private double tunedK;
            [SerializeField]
            private double tunedB;
            [SerializeField]
            private double tunedTheta;

            public override double k => tunedK;

            public override double b => tunedB;

            public override double theta => tunedTheta;

            public StaticBehaviour(ActuatorSimple device) : base(device)
            {
                this.device = device;
            }

            public override void OnEnterState()
            {

            }

            public override bool ShouldTransition()
            {
                return false;
            }
        }

        private static double SingleSPD(double posError, double velError, double Kp, double Kd, double biasForces, double M, double dt)
        {
            var pTerm = Kp * posError;
            var dTerm = Kd * velError;
            var spdQacc = (pTerm + dTerm - biasForces) / (M + Kd * dt);
            var tau = pTerm + dTerm - Kd * spdQacc * dt;
            return tau;
        }

    }
}