using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MotionMatch
{
    public abstract class DampedTrajectoryInput : MonoBehaviour, IMMInput
    {
        protected DampedTrajectory trajectory;

        [SerializeField]
        float defaultEigenv;

        public float Eignv { get => trajectory.Eignv; set => trajectory.Eignv = value; }
        public Vector2 AnalogueDirection => analogueDirection;
        protected Vector2 analogueDirection;

        public virtual Vector2 AnalogueHeading => analogueDirection.normalized;

        public virtual IEnumerable<Vector2> CurrentTrajectoryAndDirection
        {
            get
            {
                return trajectory.CurrentSamples.Select(v => new Vector2(v.y, -v.x));
            }
        }


        private void Awake()
        {
            trajectory = new DampedTrajectory();
            trajectory.Eignv = defaultEigenv;
            InitializeInput();
        }

        private void FixedUpdate()
        {
            UpdateDirection();
            UpdateTrajectory();
        }

        protected abstract void InitializeInput();

        protected abstract void UpdateDirection();

        protected void UpdateTrajectory()
        {
            trajectory.TargetVelocity = analogueDirection;
            trajectory.UpdateStep(Time.deltaTime);
        }


        protected class DampedTrajectory
        {

            readonly IEnumerable<float> timeSamples = MMUtility.LinSpace(1f / 3f, 1, 3);

            Vector2 localVelocity;
            Vector2 localAcceleration;

            Vector2 targetVelocity;
            public Vector2 TargetVelocity { get => targetVelocity; set => targetVelocity = value; }

            private float eignv = -3;
            public float Eignv { get => eignv; set => eignv = value; }

            Vector2 c1;
            Vector2 c2;
            Vector2 c3;

            public IEnumerable<Vector2> CurrentSamples
            {
                get
                {
                    c1 = localVelocity - targetVelocity;
                    c2 = localAcceleration - c1 * eignv;
                    c3 = c2 - c1 * eignv;
                    return TrajectoryHorizon.Concat(DirectionHorizon);
                }
            }

            public IEnumerable<Vector2> CurrentTrajectory
            {
                get
                {
                    c1 = localVelocity - targetVelocity;
                    c2 = localAcceleration - c1 * eignv;
                    c3 = c2 - c1 * eignv;
                    return TrajectoryHorizon;
                }
            }

            public void UpdateStep(float dt)
            {
                c1 = localVelocity - targetVelocity;
                c2 = localAcceleration - c1 * eignv;
                localVelocity = VelocityStep(dt);
                localAcceleration = AccelerationStep(dt);
            }

            Vector2 VelocityStep(float dt)
            {
                return targetVelocity + (c1 + c2 * dt) * Mathf.Exp(eignv * dt);
            }

            Vector2 AccelerationStep(float dt)
            {
                return (localAcceleration + c2 * eignv * dt) * Mathf.Exp((eignv * dt)); ;
            }

            Vector2 PositionStep(float dt)
            {
                return targetVelocity * dt + (c3 + (c1 * eignv + c2 * eignv * dt - c2) * Mathf.Exp((eignv * dt))) / (eignv * eignv);
            }

            IEnumerable<Vector2> VelocityHorizon
            {
                get
                {
                    return timeSamples.Select(t => VelocityStep(t));
                }
            }

            IEnumerable<Vector2> AccelerationHorizon
            {
                get
                {
                    return timeSamples.Select(t => AccelerationStep(t));
                }
            }

            IEnumerable<Vector2> TrajectoryHorizon
            {
                get
                {
                    return timeSamples.Select(t => PositionStep(t));
                }
            }

            IEnumerable<Vector2> DirectionHorizon
            {
                get
                {
                    return timeSamples.Select(t => VelocityStep(t).normalized);
                }
            }

            public IEnumerable<float> TimeSamples => timeSamples;
        }
    }
}
