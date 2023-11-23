using System;
using UnityEngine;
using Mujoco;
using Mujoco.Extensions;
using ModularAgents;

namespace GaitLab
{
    public class ActuatorOSL : MonoBehaviour
    {
        private enum ActivityMode
        {
            LGW
        }


        [Serializable]
        public enum GaitPhase
        {
            EarlyStance,
            LateStance,
            SwingFlexion,
            SwingExtension,
            Static
        }

        private ActivityMode mode;

        [SerializeField]
        public GaitPhase phase;

        public GaitPhaseBehaviour ActiveBehaviour
        {
            get
            {
                switch (phase)
                {
                    case GaitPhase.EarlyStance:
                        return earlyStance;

                    case GaitPhase.LateStance:
                        return lateStance;

                    case GaitPhase.SwingFlexion:
                        return swingFlexion;

                    case GaitPhase.SwingExtension:
                        return swingExtension;

                    case GaitPhase.Static:
                        return staticBehaviour;

                    default:
                        return earlyStance;
                }
            }

        }

        private double lastSwitchTime;

        public double AnkleAngle { get; private set; }

        public double Torque { get; private set; }
        private double AnkleVelocity { get; set; }
        private double KneeVelocity { get; set; }
        [SerializeField]
        private bool autoTransition;

        [SerializeField]
        private double bodyMass;

        [SerializeField]
        private double ankleAngleOffset;

        private int inertiaMatrixIndex;

        private double FAxial { get; set; }

        [SerializeField]
        bool invertAnkleAngle;

        [SerializeField]
        bool invertKneeAngle;

        [SerializeField]
        int switchConfidence;

        int switchCount;

        [SerializeField]
        private double Eq4C;

        [SerializeField]
        private double minPhaseTime;

        [SerializeField]
        private double dampingOffset;

        [Serializable]
        private struct ImpedanceSettings
        {
            [SerializeField]
            public double stiffnessScale;

            [SerializeField]
            public double stiffnessOffset;

            [SerializeField]
            public double dampingScale;

            [SerializeField]
            public double dampingOffset;
        }

        [SerializeField]
        ImpedanceSettings impedanceSettings;

        public double PhaseTime { get => Time.timeAsDouble - lastSwitchTime; }


        [SerializeField]
        private EarlyStanceBehaviour earlyStance;
        [SerializeField]
        private LateStanceBehaviour lateStance;
        [SerializeField]
        private SwingFlexionBehaviour swingFlexion;
        [SerializeField]
        private SwingExtensionBehaviour swingExtension;

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
        private GRFObservationSource forceSensor;

        unsafe private void Awake()
        {
            MjScene.Instance.ctrlCallback += UpdateTorque;
            inertiaMatrixIndex = -1;

            earlyStance.device = this;
            lateStance.device = this;
            swingFlexion.device = this;
            swingExtension.device = this;
            staticBehaviour.device = this;

        }

        unsafe private void UpdateTorque(object sender, MjStepArgs e)
        {
            if (ActiveBehaviour.ShouldTransition() && PhaseTime > minPhaseTime && autoTransition)
            {
                switchCount++;

                if (switchCount >= switchConfidence)
                {
                    phase = (GaitPhase)(((int)phase + 1) % 4);
                    ActiveBehaviour.OnEnterState();
                    lastSwitchTime = Time.timeAsDouble;
                    switchCount = 0;
                }

            }
            GaitPhaseBehaviour curBehaviour = ActiveBehaviour;

            if (inertiaMatrixIndex == -1) inertiaMatrixIndex = MjState.GetInertiaMatrixIndex(ankleJoint.DofAddress, e);

            double sign = (invertAnkleAngle ? -1 : 1);


            AnkleAngle = sign * ((e.data->qpos[ankleJoint.QposAddress]) * Mathf.Rad2Deg + ankleAngleOffset);
            KneeVelocity = (invertKneeAngle ? -1 : 1) * (e.data->qvel[kneeJoint.DofAddress]) * Mathf.Rad2Deg;
            AnkleVelocity = sign * (e.data->qvel[ankleJoint.DofAddress]) * Mathf.Rad2Deg;
            (_, var sensorReading) = forceSensor.GetMeanGRF();
            FAxial = Vector3.Dot(kneeJoint.transform.rotation * Vector3.forward, sensorReading);



            /*            var force = Math.Clamp(curBehaviour.k*posError - curBehaviour.b*AnkleVelocity, -maxForce, maxForce) * Mathf.Deg2Rad;
                        Torque = force;*/

            var posError = sign * (curBehaviour.theta - AnkleAngle - Time.fixedDeltaTime * AnkleVelocity) * Mathf.Deg2Rad;
            var velError = sign * (-curBehaviour.b * AnkleVelocity) * Mathf.Deg2Rad;

            double M = MjState.GetInertiaWithMatrixIndex(inertiaMatrixIndex, e);
            double biasForce = sign * e.data->qfrc_bias[ankleJoint.DofAddress];
            double passiveForce = sign * e.data->qfrc_passive[ankleJoint.DofAddress];

            double k = curBehaviour.k * impedanceSettings.stiffnessScale + impedanceSettings.stiffnessOffset;
            double b = curBehaviour.b * impedanceSettings.dampingScale + impedanceSettings.dampingOffset;

            double force = Math.Clamp(SingleSPD(posError, velError, k, b, biasForce, M, Time.fixedDeltaTime), -maxForce, maxForce);
            Torque = sign * force;

            //print($"behav: {curBehaviour.phase}, b:{curBehaviour.b}, theta:{curBehaviour.theta}, k:{curBehaviour.k}");

            e.data->qfrc_applied[ankleJoint.DofAddress] += force;
        }

        private void OnValidate()
        {
            Eq4C = Math.Max(Eq4C, 1);
        }

        public abstract class GaitPhaseBehaviour
        {
            public abstract GaitPhase phase { get; }
            public abstract double k { get; }
            public abstract double b { get; }
            public abstract double theta { get; }

            [HideInInspector]
            public ActuatorOSL device; //for sensing


            public GaitPhaseBehaviour(ActuatorOSL device)
            {
                this.device = device;
            }

            public abstract void OnEnterState();

            public abstract bool ShouldTransition(); //use device state to determine if condition is satisfied
        }

        [Serializable]
        private class EarlyStanceBehaviour : GaitPhaseBehaviour
        {
            public override GaitPhase phase { get => GaitPhase.EarlyStance; }

            [SerializeField, Range(0.25f, 0.3f)]
            private double tunedB;

            [SerializeField, Tooltip("Transition to Late Stance when dorsiflexion exceeds this")]
            private double ankleAngleThreshold;

            [SerializeField]
            private double tunedTheta;

            public override double k { get => Math.Clamp(Eq2(device.bodyMass, device.AnkleAngle), 2.5, 7); }
            public override double b { get => tunedB; }
            public override double theta { get => tunedTheta; }

            public EarlyStanceBehaviour(ActuatorOSL device) : base(device)
            {
                this.device = device;
            }

            public override bool ShouldTransition()
            {
                return device.AnkleAngle > ankleAngleThreshold;
            }

            public override void OnEnterState() { }
        }

        [Serializable]
        private class LateStanceBehaviour : GaitPhaseBehaviour
        {
            public override GaitPhase phase
            {
                get => GaitPhase.LateStance;
            }

            [SerializeField, Range(0.25f, 0.3f)]
            private double tunedB;

            [SerializeField]
            private double loadThreshold;

            [SerializeField, Range(-24f, 24f)]
            private double tunedThetaFinal;

            public override double k { get => Math.Clamp(Eq2(device.bodyMass, device.AnkleAngle), 2.5, 7); }
            public override double b { get => tunedB; }

            private double thetaInitial;
            private double FInitial;
            public override double theta { get => Math.Clamp(Eq4(device.Eq4C, device.FAxial, FInitial, device.bodyMass * 0.981, thetaInitial, tunedThetaFinal), tunedThetaFinal, thetaInitial); }

            public LateStanceBehaviour(ActuatorOSL device) : base(device)
            {
                this.device = device;
            }

            public override void OnEnterState()
            {
                thetaInitial = device.earlyStance.theta;
                FInitial = device.FAxial;
            }

            public override bool ShouldTransition()
            {
                return device.FAxial < loadThreshold;
            }
        }

        [Serializable]
        private class SwingFlexionBehaviour : GaitPhaseBehaviour
        {
            public override GaitPhase phase { get => GaitPhase.SwingFlexion; }

            [SerializeField, Range(2.5f, 5)]
            private double tunedK;

            [SerializeField, Range(0.1f, 0.25f)]
            private double tunedB;

            [SerializeField, Range(0f, 2.5f)]
            private double tunedTheta;

            [SerializeField]
            private double velocityThreshold;

            public override double k => tunedK;

            public override double b => tunedB;

            public override double theta => tunedTheta;

            public override void OnEnterState()
            {
            }

            public override bool ShouldTransition()
            {
                return device.KneeVelocity < velocityThreshold;
            }

            public SwingFlexionBehaviour(ActuatorOSL device) : base(device)
            {
                this.device = device;
            }
        }

        [Serializable]
        private class SwingExtensionBehaviour : GaitPhaseBehaviour
        {
            public override GaitPhase phase { get => GaitPhase.SwingExtension; }

            [SerializeField, Range(2.5f, 5)]
            private double tunedK;

            [SerializeField, Range(0.05f, 0.25f)]
            private double tunedB;

            [SerializeField, Range(0f, 2.5f)]
            private double tunedTheta;

            [SerializeField]
            private double loadThreshold;

            public override double k => tunedK;

            public override double b => tunedB;

            public override double theta => tunedTheta;

            public override void OnEnterState()
            {
            }

            public override bool ShouldTransition()
            {
                return device.FAxial > loadThreshold;
            }

            public SwingExtensionBehaviour(ActuatorOSL device) : base(device)
            {
                this.device = device;
            }
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

            public StaticBehaviour(ActuatorOSL device) : base(device)
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

        private static double Eq2(double bodyMass, double ankleAngle) => bodyMass * (0.237 * ankleAngle + 0.028);

        private static double Eq4(double C, double F, double FInitial, double FFinal, double pInitial, double pFinal) =>
            C * ((F - FInitial) / (FInitial - FFinal)) * (pInitial - pFinal) + pInitial;

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