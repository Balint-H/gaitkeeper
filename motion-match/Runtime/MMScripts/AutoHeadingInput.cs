using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MotionMatch
{
    public class AutoHeadingInput : AutoInput
    {
        [SerializeField]
        float rotationTimeScale;

        [SerializeField]
        float activationTimeScale;

        float curState;
        const float maxState = 50000;

        [SerializeField]
        float perlinThreshold;

        [SerializeField]
        Transform visualizationOrigin;

        float perlinActivation;

        protected Vector2 analogueHeading;
        protected List<Vector2> filteredHeadings;
        protected Vector2 prevAnalogueHeading;
        public override Vector2 AnalogueHeading => perlinActivation > perlinThreshold? 
            (analogueDirection.magnitude == 0? Vector2.zero : Vector2.up) : analogueDirection.normalized;

        protected override void InitializeInput()
        {
            base.InitializeInput();
            prevAnalogueHeading = analogueHeading;
            filteredHeadings = Enumerable.Repeat(prevAnalogueHeading, trajectory.TimeSamples.Count()).ToList();
        }

        protected override void UpdateDirection()
        {
            base.UpdateDirection();
            curState =( curState + Time.deltaTime ) % maxState;
            float newX = (Mathf.PerlinNoise(curState/ rotationTimeScale, 0)-0.5f)*2;
            float newY = (Mathf.PerlinNoise(0, curState/ rotationTimeScale) -0.5f)*2;

            perlinActivation = Mathf.PerlinNoise(curState / activationTimeScale, 10f);
            analogueHeading = AnalogueHeading;

            var headingVelocity = analogueHeading - prevAnalogueHeading;

            filteredHeadings = filteredHeadings.Zip(trajectory.TimeSamples,
                (h, t) => Vector2.SmoothDamp(h, analogueHeading, ref headingVelocity, 1 / Eignv, Mathf.Infinity, t)).Select(v => v.normalized).ToList();
            prevAnalogueHeading = analogueHeading;
        }
        

        public override IEnumerable<Vector2> CurrentTrajectoryAndDirection
        {
            get
            {
                if (perlinActivation< perlinThreshold)
                {

                    return trajectory.CurrentSamples.Select(v => new Vector2(v.y, -v.x));
                }
                else
                {
                    return trajectory.CurrentTrajectory.Select(v => new Vector2(v.y, -v.x)).Concat(filteredHeadings.Select(v => new Vector2(v.y, -v.x).normalized));
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!visualizationOrigin) return;
            if (!Application.isPlaying) return;

            var mmData = CurrentTrajectoryAndDirection.ToArray();
            var traj = mmData[0..3].Select(v => new Vector2(-v.y, v.x));
            var dir = mmData[3..].Select(v => new Vector2(-v.y, v.x)).ToArray();
            if (perlinActivation < perlinThreshold) Gizmos.color = Color.blue;
            else Gizmos.color = Color.green;
            Gizmos.DrawRay(visualizationOrigin.position + Vector3.up * 0.2f, AnalogueHeading.ProjectTo3D());

            Gizmos.color = new Color(1, 0.45f, 0);
            var starts = traj.Prepend(Vector2.zero);
            foreach ((var start, var finish) in starts.Zip(traj, Tuple.Create))
            {
                Gizmos.DrawLine(start.ProjectTo3D() + Vector3.up * 0.02f + MMUtility.Horizontal3D(visualizationOrigin.position),
                    finish.ProjectTo3D() + Vector3.up * 0.02f + MMUtility.Horizontal3D(visualizationOrigin.position));
            }

        }

    }
}
