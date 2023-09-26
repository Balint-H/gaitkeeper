using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using MathNet.Numerics.LinearAlgebra;
using ModularAgents.DReCon;
using Mujoco.Extensions;
using Mujoco;

namespace ModularAgents.MotorControl
{
    public class AdaptiveProsthesisActuator : DReConActuator
    {
        [SerializeField]
        protected Transform root;

        [SerializeField, Tooltip("No action assigned to these joints, copy from reference if available")]
        protected List<MjBaseJoint> softExcludeList;

        [SerializeField, Tooltip("No action assigned to these joints")]
        protected List<MjBaseJoint> hardExcludeList;

        [SerializeField]
        double posGain;

        float springConstant;

        [SerializeField]
        double velGain;

        float damping;

        [SerializeField]
        double maxForce;

        protected IReadOnlyList<IMjJointState> jointStates;
        protected IReadOnlyList<IMjJointState> activeReferenceStates;
        protected int[] dofAddresses;
        protected int[] activeDofLocalIndices; // Maps actions of agent to the array of DoFs actuated by this component
        protected Dictionary<(int, int), int> inertiaSubMatrixMap;

        protected Matrix<double> posGainMatrix;
        protected Matrix<double> velGainMatrix;
        public virtual IEnumerable<MjBaseJoint> Joints { get => root.GetComponentsInChildren<MjBaseJoint>().Where(j => j is not MjFreeJoint); }
        public virtual IEnumerable<MjBaseJoint> ActiveJoints
        {
            get => IsExcludeDefined ? Joints.Where(j => !softExcludeList.Contains(j) && !hardExcludeList.Contains(j))
                                    : Joints;
        }

        [SerializeField]
        Transform kinematicRef;

        [SerializeField]
        bool trackState;

        [SerializeField]
        bool updateAlone;

        [SerializeField]
        double angleOffset;

        [SerializeField]
        double modulationScale;

        double posGainDefault;
        double velGainDefault;

        double dt;

        private bool IsExcludeDefined { get => (softExcludeList != null && hardExcludeList != null && softExcludeList.Count + hardExcludeList.Count > 0); }
        private int ExcludedDofCount => IsExcludeDefined ? softExcludeList.DofSum() + hardExcludeList.DofSum() : 0;

        public override int ActionSpaceSize => 2;

        public double PosGain { get => posGain;}
        public double VelGain { get => velGain;}

        unsafe private void UpdateTorque(object sender, MjStepArgs e)
        {
            var posError = IMjJointState.GetStablePosErrorVector(jointStates, dt) + Vector<double>.Build.DenseOfArray(new[] { angleOffset});
            var velError = IMjJointState.GetVelErrorVector(jointStates);

            posGainMatrix = Matrix<double>.Build.Diagonal(dofAddresses.Length, dofAddresses.Length, posGain);
            velGainMatrix = Matrix<double>.Build.Diagonal(dofAddresses.Length, dofAddresses.Length, velGain);
            

            //Vector<double> biasVector = (Vector<double>.Build.DenseOfArray(MjState.GetSubBias(dofAddresses, e)) + Vector<double>.Build.DenseOfArray(MjState.GetSubPassive(dofAddresses, e)));
            //Matrix<double> inertiaMatrix = MjState.GetSubInertiaArray(inertiaSubMatrixMap, dofAddresses.Length, e).ToSquareMatrix(dofAddresses.Length);

            var generalizedForces = ComputePD(posError, velError, posGainMatrix, velGainMatrix);

            foreach ((var dofIdx, var force) in dofAddresses.Zip(generalizedForces, Tuple.Create))
            {
                if (double.IsNaN(force))
                {
                    Debug.Log("Nan!");
                }

                e.data->qfrc_applied[dofIdx] = Math.Clamp(force, -maxForce, maxForce);
            }
        }

        unsafe public override void ApplyActions(float[] actions, float actionTimeDelta)
        {
            posGain = posGainDefault + modulationScale * actions[0];
            velGain = velGainDefault + modulationScale * actions[1];

            springConstant = (float)posGain;
            damping = (float)velGain;

        }

        public override float[] GetActionsFromState()
        {
            return new[] {0f, 0f};
        }

        private static Vector<double> ActionsToVector(float[] actions, int[] indices, int dofCount)
        {
            double[] castExpandedActions = new double[dofCount];
            for (int i = 0; i < actions.Length; i++)
            {
                castExpandedActions[indices[i]] = actions[i];
            }

            return Vector<double>.Build.DenseOfArray(castExpandedActions);
        }

        public void SetGains(double kp, double kd)
        {
            posGain = kp;
            velGain = kd;
        }

        public unsafe override void OnAgentInitialize(DReConAgent agent)
        {
            MjScene.Instance.ctrlCallback += UpdateTorque;
            var actuatedJoints = hardExcludeList != null ? Joints.Where(j => !hardExcludeList.Contains(j)) : Joints;
            var activeJoints = ActiveJoints;

            Func<MjBaseJoint, bool> IsActive = (MjBaseJoint j) => !IsExcludeDefined || activeJoints.Contains(j);
            Func<IEnumerable<MjBaseJoint>, IEnumerable<bool>> GetDofActivity = (IEnumerable<MjBaseJoint> js) => js.Select(j => Enumerable.Repeat(IsActive(j), j.DofCount())).SelectMany(x => x);
            Func<IEnumerable<bool>, IEnumerable<int>> GetIndicesOfTrue = (IEnumerable<bool> bs) => bs.Select((b, i) => (b, i)).Where(enumerated => enumerated.b).Select(enumerated => enumerated.i);
            Func<IEnumerable<MjBaseJoint>, IEnumerable<int>> GetActiveDofIndices = (IEnumerable<MjBaseJoint> js) => GetIndicesOfTrue(GetDofActivity(js));

            if (kinematicRef && trackState)
            {
                jointStates = actuatedJoints.Select(j => IMjJointState.GetJointState(j, FindReference(j))).ToList();
                activeReferenceStates = jointStates.Where(js => IsActive(js.Joint)).Select(js => js.ReferenceState).ToList();
            }
            else
            {
                jointStates = actuatedJoints.Select(j => IMjJointState.GetJointState(j)).ToList();

                activeReferenceStates = jointStates.Where(js => IsActive(js.Joint)).Select(js => kinematicRef? IMjJointState.GetJointState(FindReference(js.Joint)) : IMjJointState.GetZeroJointStateLike(js.Joint)).ToList();
            }

            activeDofLocalIndices = GetActiveDofIndices(actuatedJoints).ToArray();
            dofAddresses = IMjJointState.GetDofAddresses(jointStates);

            posGainMatrix = Matrix<double>.Build.Diagonal(dofAddresses.Length, dofAddresses.Length, posGain);
            velGainMatrix = Matrix<double>.Build.Diagonal(dofAddresses.Length, dofAddresses.Length, velGain);

            //inertiaSubMatrixMap = MjState.GetInertiaSubMatrixIndexMap(dofAddresses.ToList(), new MjStepArgs(MjScene.Instance.Model, MjScene.Instance.Data));
            dt = Time.fixedDeltaTime;
            posGainDefault = posGain;
            velGainDefault = velGain;
        }

        private void OnDisable()
        {
            if (MjScene.InstanceExists) MjScene.Instance.ctrlCallback -= UpdateTorque;
        }

        private MjBaseJoint FindReference(MjBaseJoint joint)
        {
            return kinematicRef ? kinematicRef.GetComponentsInChildren<MjBaseJoint>().First(rj => rj.name.Contains(joint.name)) : null;
        }

        private void Start()
        {
            if (updateAlone)
            {
                OnAgentInitialize(null);
            }
        }

        /// <summary>
        /// Calculate Stable PD forces as per Tan et al.
        /// </summary>
        /// <param name="posErrors">Should include first order Taylor expansion</param>
        /// <param name="velErrors"></param>
        /// <param name="biasForces">Should include passive joint forces as well</param>
        /// <param name="dt"></param>
        /// <returns></returns>
        private static Vector<double> ComputePD(Vector<double> posErrors, Vector<double> velErrors, Matrix<double> Kp, Matrix<double> Kd)
        {
            var pTerm = Kp * posErrors;
            var dTerm = Kd * velErrors;
            //var spdQacc = (M + Kd * dt).Solve(pTerm + dTerm - biasForces);
            var tau = pTerm + dTerm; // - Kd * spdQacc * dt;
            return tau;
        }


    }
}
