using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Mujoco;
using static Mujoco.MjScene;
using MathNet.Numerics.LinearAlgebra;
using ModularAgents.DReCon;
using Mujoco.Extensions;
using ModularAgents.MotorControl.Mujoco;
using MathNet.Numerics.RootFinding;

namespace ModularAgents.MotorControl
{
    public class ProsthesisActuatorComponent : ActuatorComponent
    {
        [SerializeField]
        protected Transform root;

        [SerializeField, Tooltip("No action assigned to these joints, copy from reference if available")]
        protected List<MjBaseJoint> softExcludeList;

        [SerializeField, Tooltip("No action assigned to these joints")]
        protected List<MjBaseJoint> hardExcludeList;

        [SerializeField]
        bool useHeuristic;

        public int ActionSpaceSize => 3 * ActiveJoints.DofSum();

        [SerializeField]
        List<double> activePosGains;

        Vector<double> posGains;

        public float stiffness;

        public override ActionSpec ActionSpec => new ActionSpec(ActionSpaceSize);

        [SerializeField]
        List<double> activeVelGains;

        Vector<double> velGains;

        public float damping;

        [SerializeField]
        List<double> activeAngleOffsets;

        Vector<double> angleOffsets;

        //[DebugGUIGraph(group: 1, b:0, autoScale: true)]
        public float kneeTheta;

        public float theta;

        Vector<double> angleOffsetDefaults;


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
        double modulationScale;

        [SerializeField]
        ModularAgent agent;
        [SerializeField]
        GameObject smoothingObject;

        IRememberPreviousActions prevActionSource;

        Vector<double> posGainDefaults;
        Vector<double> velGainDefaults;

        double dt;

        private bool IsExcludeDefined { get => (softExcludeList != null && hardExcludeList != null && softExcludeList.Count + hardExcludeList.Count > 0); }
        private int ExcludedDofCount => IsExcludeDefined ? softExcludeList.DofSum() + hardExcludeList.DofSum() : 0;

        public double[] PosGains { get => posGains.ToArray(); }
        public double[] VelGains { get => velGains.ToArray(); }

        [SerializeField]
        bool waitBeforeSubscribe;

        unsafe private void UpdateTorque(object sender, MjStepArgs e)
        {
            var posError = IMjJointState.GetStablePosErrorVector(jointStates, dt) + angleOffsets;
            var velError = IMjJointState.GetVelErrorVector(jointStates);

            posGainMatrix = Matrix<double>.Build.DiagonalOfDiagonalVector(posGains);
            velGainMatrix = Matrix<double>.Build.DiagonalOfDiagonalVector(velGains);


            //Vector<double> biasVector = (Vector<double>.Build.DenseOfArray(MjState.GetSubBias(dofAddresses, e))*0 + Vector<double>.Build.DenseOfArray(MjState.GetSubPassive(dofAddresses, e)))*0;
            //Matrix<double> inertiaMatrix = MjState.GetSubInertiaArray(inertiaSubMatrixMap, dofAddresses.Length, e).ToSquareMatrix(dofAddresses.Length);

            var generalizedForces = ComputePD(posError, velError, posGainMatrix, velGainMatrix);

            foreach ((var dofIdx, var force) in dofAddresses.Zip(generalizedForces, Tuple.Create))
            {
                if (double.IsNaN(force))
                {
                    Debug.Log("Nan!");
                }

                e.data->qfrc_applied[dofIdx] = Math.Clamp(force, -maxForce, maxForce);
                foreach (var exc in hardExcludeList)
                {
                    e.data->qfrc_applied[exc.DofAddress] = 0;
                }
            }
            Debug.Log("Prosthesis Torque");
        }

        unsafe public void ApplyActions(float[] actions)
        {
#if UNITY_EDITOR
            posGainDefaults = ArrayToActiveAndSoftVector(activePosGains.ToArray());
            velGainDefaults = ArrayToActiveAndSoftVector(activeVelGains.ToArray());
            angleOffsetDefaults = ArrayToActiveAndSoftVector(activeAngleOffsets.ToArray());
#endif
            posGains = posGainDefaults + modulationScale * ArrayToActiveAndSoftVector(actions[..(actions.Length / 3)]);
            velGains = velGainDefaults + modulationScale * ArrayToActiveAndSoftVector(actions[(actions.Length / 3)..(2 * actions.Length / 3)]);
            angleOffsets = angleOffsetDefaults + ArrayToActiveAndSoftVector(actions[(2 * actions.Length / 3)..(3 * actions.Length / 3)]);

            theta = (float)angleOffsets[0];
            stiffness = (float)posGains[0];
            damping = (float)velGains[0];
            Debug.Log("Prosthesis Actions");
        }

        private Vector<double> ArrayToActiveAndSoftVector(float[] arr)
        {
            var vecOut = Vector<double>.Build.SparseOfIndexed(dofAddresses.Length, activeDofLocalIndices.Zip(arr, (i, a) => Tuple.Create(i, (double)a)));
            return vecOut;
        }

        private Vector<double> ArrayToActiveAndSoftVector(double[] arr)
        {
            var vecOut = Vector<double>.Build.SparseOfIndexed(dofAddresses.Length, activeDofLocalIndices.Zip(arr, Tuple.Create));
            return vecOut;
        }

        public float[] GetActionsFromState()
        {
            return new float[ActionSpaceSize];
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

        public void SetGains(double[] kp, double[] kd)
        {
            posGains = ArrayToActiveAndSoftVector(kp);
            velGains = ArrayToActiveAndSoftVector(kd);
        }

        public unsafe void OnAgentInitialize(DReConAgent agent)
        {
            if (waitBeforeSubscribe)
            {
                StartCoroutine(ExecuteAfterFrames(1, () => MjScene.Instance.ctrlCallback += UpdateTorque));
            }
            else
            {
                MjScene.Instance.ctrlCallback += UpdateTorque;
            }

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

                activeReferenceStates = jointStates.Where(js => IsActive(js.Joint)).Select(js => kinematicRef ? IMjJointState.GetJointState(FindReference(js.Joint)) : IMjJointState.GetZeroJointStateLike(js.Joint)).ToList();
            }

            activeDofLocalIndices = GetActiveDofIndices(actuatedJoints).ToArray();
            dofAddresses = IMjJointState.GetDofAddresses(jointStates);

            posGains = ArrayToActiveAndSoftVector(activePosGains.ToArray());
            velGains = ArrayToActiveAndSoftVector(activeVelGains.ToArray());
            angleOffsets = ArrayToActiveAndSoftVector(activeAngleOffsets.ToArray());


            posGainMatrix = Matrix<double>.Build.DiagonalOfDiagonalVector(posGains);
            velGainMatrix = Matrix<double>.Build.DiagonalOfDiagonalVector(velGains);

            inertiaSubMatrixMap = MjState.GetInertiaSubMatrixIndexMap(dofAddresses.ToList(), new MjStepArgs(MjScene.Instance.Model, MjScene.Instance.Data));
            dt = Time.fixedDeltaTime;
            posGainDefaults = Vector<double>.Build.SparseOfVector(posGains);
            velGainDefaults = Vector<double>.Build.SparseOfVector(velGains);
            angleOffsetDefaults = Vector<double>.Build.SparseOfVector(angleOffsets);
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

        private unsafe void Awake()
        {
            if (agent)
            {

                if (smoothingObject)
                {
                    prevActionSource = smoothingObject.GetComponent<IRememberPreviousActions>();
                    agent.OnBegin += (object sender, EventArgs e) => prevActionSource.SetPreviousActions(GetActionsFromState());
                }
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

        private static Vector<double> ComputeSPD(Vector<double> posErrors, Vector<double> velErrors, Matrix<double> Kp, Matrix<double> Kd, Vector<double> biasForces, Matrix<double> M, double dt)
        {
            var pTerm = Kp * posErrors;
            var dTerm = Kd * velErrors;
            var spdQacc = (M + Kd * dt).Solve(pTerm + dTerm - biasForces);
            var tau = pTerm + dTerm - Kd * spdQacc * dt;
            return tau;
        }

        public static IEnumerator ExecuteAfterFrames(int frameCount, Action act)
        {
            if (frameCount <= 0)
            {
                throw new ArgumentOutOfRangeException("frameCount", "Cannot wait for less that 1 frame");
            }

            while (frameCount > 0)
            {
                frameCount--;
                yield return null;
            }
            act.Invoke();
        }

        public IActuator actuator;
        public override IActuator[] CreateActuators()
        {
            if (actuator == null)
            {
                actuator = new ProsthesisActuator(this);
            }
            return new[] { actuator };
        }

        class ProsthesisActuator : IActuator
        {
            ProsthesisActuatorComponent component;

            public ProsthesisActuator(ProsthesisActuatorComponent component)
            {
                this.component = component;
                actionSpec = new ActionSpec(component.ActionSpaceSize);
            }

            ActionSpec actionSpec;

            public string Name => component.name + "_SPD_Actuator";

            ActionSpec IActuator.ActionSpec => actionSpec;

            public void Heuristic(in ActionBuffers actionBuffersOut)
            {
                if (!component.useHeuristic) return;
                var actions = actionBuffersOut.ContinuousActions;
                var actionsFromState = component.GetActionsFromState();
                for (var actionIndex = 0; actionIndex < actions.Length; actionIndex++)
                {
                    actions[actionIndex] = actionsFromState[actionIndex];
                }
            }

            public void OnActionReceived(ActionBuffers actionBuffers)
            {
                var actions = actionBuffers.ContinuousActions;
                component.ApplyActions(actions.Array[actionBuffers.ContinuousActions.Offset..(actionBuffers.ContinuousActions.Offset + actionBuffers.ContinuousActions.Length)]);
            }

            public void ResetData()
            {

            }

            public void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
            {

            }
        }
    }
}