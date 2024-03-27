using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;

using UnityEngine.Animations;

namespace MotionMatch
{
    public class LegIK : MonoBehaviour
    {
        [SerializeField]
        MMController controller;

        public bool syncGoal = true;

        [Range(0.0f, 1.5f)]
        public float stiffness = 1.0f;

        [Range(1, 50)]
        public int maxPullIteration = 5;

        [Range(0, 1)]
        public float defaultEffectorPositionWeight = 1.0f;
        [Range(0, 1)]
        public float defaultEffectorRotationWeight = 1.0f;
        [Range(0, 1)]
        public float defaultEffectorPullWeight = 1.0f;
        [Range(0, 1)]
        public float defaultHintWeight = 0.0f;

        public GameObject LeftFootEffector;
        public GameObject RightFootEffector;

        private GameObject m_LeftFootEffector;
        private GameObject m_RightFootEffector;

        private GameObject m_LeftKneeHintEffector;
        private GameObject m_RightKneeHintEffector;

        private Animator m_Animator;
        private PlayableGraph m_Graph;
        private AnimationScriptPlayable m_IKPlayable;

        [SerializeField]


        private static GameObject CreateEffector(string name)
        {
            var go = IKUtility.CreateEffector(name, Vector3.zero, Quaternion.identity);
            return go;
        }

        private static GameObject CreateBodyEffector(string name)
        {
            var go = IKUtility.CreateBodyEffector(name, Vector3.zero, Quaternion.identity);
            return go;
        }

        private GameObject SetupEffector(ref LegIKJob.EffectorHandle handle, GameObject go)
        {
            if (go != null)
            {
                handle.effector = m_Animator.BindSceneTransform(go.transform);
                handle.positionWeight = m_Animator.BindSceneProperty(go.transform, typeof(Effector), "positionWeight");
                handle.rotationWeight = m_Animator.BindSceneProperty(go.transform, typeof(Effector), "rotationWeight");
                handle.pullWeight = m_Animator.BindSceneProperty(go.transform, typeof(Effector), "pullWeight");
            }
            return go;
        }

        private GameObject SetupHintEffector(ref LegIKJob.HintEffectorHandle handle, string name)
        {
            var go = CreateEffector(name);
            if (go != null)
            {
                go.AddComponent<HintEffector>();
                handle.hint = m_Animator.BindSceneTransform(go.transform);
                handle.weight = m_Animator.BindSceneProperty(go.transform, typeof(HintEffector), "weight");
            }
            return go;
        }


        private void SetupIKLimbHandle(ref LegIKJob.IKLimbHandle handle, HumanBodyBones top, HumanBodyBones middle, HumanBodyBones end)
        {
            handle.top = m_Animator.BindStreamTransform(m_Animator.GetBoneTransform(top));
            handle.middle = m_Animator.BindStreamTransform(m_Animator.GetBoneTransform(middle));
            handle.end = m_Animator.BindStreamTransform(m_Animator.GetBoneTransform(end));
        }

        private void ResetIKWeight()
        {
            m_LeftFootEffector.GetComponent<Effector>().positionWeight = defaultEffectorPositionWeight;
            m_LeftFootEffector.GetComponent<Effector>().rotationWeight = defaultEffectorRotationWeight;
            m_LeftFootEffector.GetComponent<Effector>().pullWeight = defaultEffectorPullWeight;
            m_RightFootEffector.GetComponent<Effector>().positionWeight = defaultEffectorPositionWeight;
            m_RightFootEffector.GetComponent<Effector>().rotationWeight = defaultEffectorRotationWeight;
            m_RightFootEffector.GetComponent<Effector>().pullWeight = defaultEffectorPullWeight;

            m_LeftKneeHintEffector.GetComponent<HintEffector>().weight = defaultHintWeight;
            m_RightKneeHintEffector.GetComponent<HintEffector>().weight = defaultHintWeight;
        }

        private void SyncIKFromPose()
        {
            var selectedTransform = Selection.transforms;

            var stream = new AnimationStream();
            if (m_Animator.OpenAnimationStream(ref stream))
            {
                AnimationHumanStream humanStream = stream.AsHuman();

                // don't sync if transform is currently selected
                if (!Array.Exists(selectedTransform, tr => tr == m_LeftFootEffector.transform))
                {
                    m_LeftFootEffector.transform.position = humanStream.GetGoalPositionFromPose(AvatarIKGoal.LeftFoot);
                    m_LeftFootEffector.transform.rotation = humanStream.GetGoalRotationFromPose(AvatarIKGoal.LeftFoot);
                }

                if (!Array.Exists(selectedTransform, tr => tr == m_RightFootEffector.transform))
                {
                    m_RightFootEffector.transform.position = humanStream.GetGoalPositionFromPose(AvatarIKGoal.RightFoot);
                    m_RightFootEffector.transform.rotation = humanStream.GetGoalRotationFromPose(AvatarIKGoal.RightFoot);
                }

                if (!Array.Exists(selectedTransform, tr => tr == m_LeftKneeHintEffector.transform))
                {
                    m_LeftKneeHintEffector.transform.position = humanStream.GetHintPosition(AvatarIKHint.LeftKnee);
                }

                if (!Array.Exists(selectedTransform, tr => tr == m_RightKneeHintEffector.transform))
                {
                    m_RightKneeHintEffector.transform.position = humanStream.GetHintPosition(AvatarIKHint.RightKnee);
                }

                m_Animator.CloseAnimationStream(ref stream);
            }
        }

        void Start()
        {
            var mixer = controller.Animator.Mixer;

            var ikPlayable = ConnectIK(mixer, controller.Animator.GetGraph());
            controller.Animator.Output.SetSourcePlayable(ikPlayable);
            

            m_Graph.Play();
            m_Graph.Evaluate(0);
            SyncIKFromPose();

            ResetIKWeight();

        }

        AnimationScriptPlayable ConnectIK<V>(V sourcePlayable, PlayableGraph graph) where V : struct, IPlayable
        {
            m_Animator = GetComponent<Animator>();

            // Setting to Always animate because on the first frame the renderer can be not visible which break syncGoal on start up
            m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (!m_Animator.avatar.isHuman)
                throw new InvalidOperationException("Avatar must be a humanoid.");

            m_Graph = graph;

            var job = new LegIKJob();
            job.stiffness = stiffness;
            job.maxPullIteration = maxPullIteration;

            SetupIKLimbHandle(ref job.leftLeg, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
            SetupIKLimbHandle(ref job.rightLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);

            m_LeftFootEffector = SetupEffector(ref job.leftFootEffector, LeftFootEffector);
            m_RightFootEffector = SetupEffector(ref job.rightFootEffector, RightFootEffector);

            m_LeftKneeHintEffector = SetupHintEffector(ref job.leftKneeHintEffector, "LeftKneeHintEffector");
            m_RightKneeHintEffector = SetupHintEffector(ref job.rightKneeHintEffector, "RightKneeHintEffector");



            m_IKPlayable = AnimationScriptPlayable.Create(m_Graph, job, 1);
            m_IKPlayable.ConnectInput(0, sourcePlayable, 0, 1.0f);
            return m_IKPlayable;
        }

        void OnDisable()
        {
            GameObject.DestroyImmediate(m_LeftFootEffector);
            GameObject.DestroyImmediate(m_RightFootEffector);
            GameObject.DestroyImmediate(m_LeftKneeHintEffector);
            GameObject.DestroyImmediate(m_RightKneeHintEffector);

            if (m_Graph.IsValid())
                m_Graph.Destroy();
        }

        void UpdateData()
        {
            var job = m_IKPlayable.GetJobData<LegIKJob>();
            job.stiffness = stiffness;
            job.maxPullIteration = maxPullIteration;
            m_IKPlayable.SetJobData(job);
        }

        void FixedUpdate()
        {
            UpdateData();
            // Synchronize on LateUpdate to sync goal on current frame
            if (syncGoal)
            {
                SyncIKFromPose();
                syncGoal = false;
            }

        }
    }
}