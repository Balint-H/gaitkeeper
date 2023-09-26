using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotionMatch
{
    public class ControllerInput : DampedTrajectoryInput, IMMInput
    {
        [SerializeField]
        InputActionAsset inputActions;


        protected Vector2 analogueHeading;
        protected List<Vector2> filteredHeadings;
        protected Vector2 prevAnalogueHeading;
        public override Vector2 AnalogueHeading => analogueHeading;

        [SerializeField]
        Transform visualizationOrigin;

        public override IEnumerable<Vector2> CurrentTrajectoryAndDirection
        {
            get
            {
                if (analogueHeading.magnitude < 0.01)
                {

                    return trajectory.CurrentSamples.Select(v => new Vector2(v.y, -v.x));
                }
                else
                {
                    return trajectory.CurrentTrajectory.Select(v => new Vector2(v.y, -v.x)).Concat(filteredHeadings.Select(v => new Vector2(v.y, -v.x).normalized));
                }
            }
        }

        private void OnVelocity(InputValue gamepadVelocity)
        {
            analogueDirection = gamepadVelocity.Get<Vector2>();
        }

        private void OnHeading(InputValue gamepadHeading)
        {
            analogueHeading = gamepadHeading.Get<Vector2>().normalized;
        }


        protected override void InitializeInput()
        {
            prevAnalogueHeading = analogueHeading;
            filteredHeadings = Enumerable.Repeat(prevAnalogueHeading, trajectory.TimeSamples.Count()).ToList();
        }

        protected override void UpdateDirection()
        {
            var headingVelocity = analogueHeading.normalized - prevAnalogueHeading;
            
            filteredHeadings = filteredHeadings.Zip(trajectory.TimeSamples,
                (h, t) => Vector2.SmoothDamp(h, analogueHeading.normalized, ref headingVelocity, 1 / Eignv,  Mathf.Infinity, t)).Select(v => v.normalized).ToList();
            prevAnalogueHeading = analogueHeading.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (!visualizationOrigin) return;
            if (!Application.isPlaying) return;

            var mmData = CurrentTrajectoryAndDirection.ToArray();
            var traj = mmData[0..3].Select(v => new Vector2(-v.y, v.x));
            var dir = mmData[3..].Select(v => new Vector2(-v.y, v.x)).ToArray();
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(visualizationOrigin.position + Vector3.up*0.2f, new Vector3(dir[0].x, 0, dir[0].y));

            Gizmos.color = Color.green;
            Gizmos.DrawRay(visualizationOrigin.position + Vector3.up * 0.2f, new Vector3(filteredHeadings[0].x, 0, filteredHeadings[0].y));

            Gizmos.color = new Color(1, 0.45f, 0);
            var starts = traj.Prepend(Vector2.zero);
            foreach((var start, var finish) in starts.Zip(traj, Tuple.Create))
            {
                Gizmos.DrawLine(start.ProjectTo3D() + Vector3.up * 0.02f + MMUtility.Horizontal3D(visualizationOrigin.position), 
                    finish.ProjectTo3D() + Vector3.up * 0.02f + MMUtility.Horizontal3D(visualizationOrigin.position));
            }
            
        }
    }
}
