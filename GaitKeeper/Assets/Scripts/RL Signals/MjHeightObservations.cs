using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;
using ModularAgents.Kinematic;
using ModularAgents.Kinematic.Mujoco;
using Mujoco;
using Mujoco.Extensions;
using System.Linq;
using System;
using MotionMatch;

namespace ModularAgents
{

    public class MjHeightObservations : ObservationSource
    {
        [SerializeField]
        List<MjBody> feet;

        [SerializeField]
        List<MjBody> toes;

        [SerializeField]
        float timeHorizon;

        [SerializeField]
        MjBody forwardReference;


        [SerializeField]
        float maxDist = 0.75f;

        [SerializeField]
        GameObject IntentObject;
        IMMInput intent;

        IKinematic referenceKinematics;

        public override int Size => 5*feet.Count;

        private static unsafe Ray GetDownRay(MujocoLib.mjModel_* model, MujocoLib.mjData_* data, MjBody body, float timeHorizon, float maxDist)
        {
            Vector3 bodyPos = MjEngineTool.UnityVector3(data->xpos + 3 * body.MujocoId);
            Vector3 bodyVel = body.GlobalVelocity();

            var dist = Mathf.Clamp(bodyVel.magnitude * timeHorizon, 0, maxDist);

            Vector3 origin = bodyPos + (bodyVel * timeHorizon).normalized*dist + 3 * Vector3.up;
            Vector3 direction = Vector3.down;  // Assume convex surface? Assume no overhangs
            return new Ray(origin, direction);
        }

        private static unsafe Ray GetForwardRay(MujocoLib.mjModel_* model, MujocoLib.mjData_* data, MjBody body, Vector3 futurePos)
        {
            Vector3 bodyPos = MjEngineTool.UnityVector3(data->xpos + 3 * body.MujocoId);
            Vector3 bodyPath = futurePos - bodyPos;

            Vector3 origin = bodyPos;
            Vector3 direction = bodyPath;
            return new Ray(origin, direction.magnitude>0.0001?  direction: Vector3.up);
        }


        private void Start()
        {
            //OnAgentInitialize();
            //MjScene.Instance.ctrlCallback += TestUpdate;
            intent = IntentObject.GetComponent<IMMInput>();
            referenceKinematics = forwardReference.transform.GetIKinematic();
        }

        public unsafe override void FeedObservationsToSensor(VectorSensor sensor)
        {
            MujocoLib.mjModel_* model = MjScene.Instance.Model;
            MujocoLib.mjData_* data = MjScene.Instance.Data;

            

            foreach ((MjBody foot, MjBody toe) in feet.Zip(toes, Tuple.Create))
            {
                Vector3 footPos = MjEngineTool.UnityVector3(data->xpos + 3 * foot.MujocoId);
                ReferenceFrame frame = new ReferenceFrame(referenceKinematics.Forward, footPos);

                var downRay = GetDownRay(model, data, foot, timeHorizon, maxDist);
                (Vector3 futurePos, double footClearance, _) = MjState.MjGroundRayCast(model, data, downRay);
                sensor.AddObservation((float)footClearance - 3f);
                

                var forwardRay = GetForwardRay(model, data, toe, futurePos);
                (Vector3 intersectPos, double intersectDist, _) = MjState.MjGroundRayCast(model, data, forwardRay);

                if (intersectDist == -1) Debug.LogWarning("No intersect!");

                sensor.AddObservation(frame.WorldToCharacter(intersectPos));

                var rootRay = new Ray(MjEngineTool.UnityVector3(data->xpos + 3 * forwardReference.MujocoId) + intent.AnalogueDirection.ProjectTo3D().normalized * maxDist, Vector3.down);
                (_, double rootHeight, _) = MjState.MjGroundRayCast(model, data, rootRay);
                sensor.AddObservation((float)rootHeight);
            }
        }

        private void TestUpdate(object sender, MjStepArgs args)
        {
            var sns = new VectorSensor(8);
            FeedObservationsToSensor(sns);
        }


        public override void OnAgentStart()
        {
            
        }

        private unsafe void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            MujocoLib.mjModel_* model = MjScene.Instance.Model;
            MujocoLib.mjData_* data = MjScene.Instance.Data;

            foreach ((MjBody foot, MjBody toe) in feet.Zip(toes, Tuple.Create))
            {
                Vector3 footPos = MjEngineTool.UnityVector3(data->xpos + 3 * foot.MujocoId);
                ReferenceFrame frame = new ReferenceFrame(referenceKinematics.Forward, footPos);

                var downRay = GetDownRay(model, data, foot, timeHorizon, maxDist);
                (Vector3 futurePos, double footClearance, _) = MjState.MjGroundRayCast(model, data, downRay);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(futurePos, 0.03f);
                Gizmos.DrawRay(futurePos, Vector3.up * ((float)footClearance - 3));

                var forwardRay = GetForwardRay(model, data, toe, futurePos);
                (Vector3 intersectPos, double intersectDist, _) = MjState.MjGroundRayCast(model, data, forwardRay);
                
                frame.Draw();

                Gizmos.color = Color.red;
                if (intersectDist == -1) Debug.LogWarning("No intersect!");
                Gizmos.DrawSphere(intersectPos, 0.02f);

                var rootRay = new Ray(MjEngineTool.UnityVector3(data->xpos + 3 * forwardReference.MujocoId) + intent.AnalogueDirection.ProjectTo3D().normalized * maxDist, Vector3.down);
                (var rootRayEnd, double rootHeight, _) = MjState.MjGroundRayCast(model, data, rootRay);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(rootRayEnd, 0.02f);
                Gizmos.DrawRay(rootRayEnd, Vector3.up * (float)rootHeight);

            }
        }
    }
}