using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;


namespace MotionMatch
{
    public enum TransitionState
    {
        QueueTransition,
        RootShift,
        Blend,
        Playing,
        Skip
    }

    public readonly struct RootProjection
    {

        public RootProjection(Transform hipIn)
        {
            position = new Vector3(hipIn.position.x, 0, hipIn.position.z);
            forward = new Vector3(hipIn.forward.x, 0, hipIn.forward.z);
            right = new Vector3(hipIn.right.x, 0, hipIn.right.z);
        }

        public readonly Vector3 position;
        public readonly Vector3 forward;
        public readonly Vector3 right;

        /// <summary>
        /// In Unity Notation (left handed)
        /// </summary>
        public float YEuler { get => -Mathf.Atan2(right.z, right.x) * Mathf.Rad2Deg; }

        public Vector2 InverseTransform(Vector2 vec)
        {
            Vector3 r = right.normalized;
            return new Vector2(r.x * vec.x + r.z * vec.y, -r.z * vec.x + r.x * vec.y);
        }

    }

    public class MMController : MonoBehaviour
    {
        MMAnimator animator;
        [SerializeField]
        Transform model = default;
        [SerializeField]
        Avatar avatar;

        [SerializeField]
        MMDataset matchingDataset;
        MotionMatcher motionMatcher;
        MotionMatcher.MMFrame foundFrame;

        [SerializeField]
        Transform fauxRoot;
        RootProjection oldHip;

        TransitionState transitionState;

        [SerializeField]
        GameObject inputObject;
        IMMInput input;

        [SerializeField]
        ControllerSettings settings;

        [SerializeField]
        Inertializer inertializer;

        [SerializeField]
        bool removeIdleIK;

        double Speed
        {
            get => animator.Speed;
            set => animator.Speed = value;
        }

        //[SerializeField]
        //MjKinematicRig rig;

        [SerializeField]
        bool updateAlone;

        public EventHandler frameReadyHandler;

        public EventHandler Handler { get => StepOnTrigger; }

        public RootProjection CurrentReferenceFrame
        {
            get
            {
                return new RootProjection(fauxRoot);
            }
        }

        int matchDelayCountdown;

        public MotionMatcher.MMFrame CurFrame
        {
            get
            {
                return new MotionMatcher.MMFrame(clipIdx: animator.CurrentClip, timeInClip: animator.CurrentTime);
            }
        }

        public float[] CurFeatures
        {
            get
            {
                return motionMatcher.GetClipFeaturesAtFrame(CurFrame);
            }
        }

        public float blendTime;

        public AnimationClip idleAnim;
        public float idleLimit;

        void Awake()
        {
            input = inputObject.GetComponent<IMMInput>();
            //Don't need to instantiate as m_anim is just a struct
            animator.Configure(model.GetChild(0).gameObject.AddComponent<UnityEngine.Animator>(), matchingDataset.motionList.Select(x => x.clip).Concat(new[] { idleAnim }).ToList(), avatar, removeIdleIK: removeIdleIK);
            motionMatcher = new MotionMatcher(matchingDataset, settings.weights, settings.maxVelocity);
            if (inertializer) inertializer.Initialize(fauxRoot, blendTime);
            matchDelayCountdown = 0;
        }


        void Start()
        {
            animator.Play();
            input.Eignv = settings.dampingEigenv;
            animator.PlayFromFrame(settings.StartingFrame);
            transitionState = TransitionState.Skip;
        }


        void OnEnable()
        {

        }

        void OnDisable()
        {
            animator.Destroy();
        }

        public void StepOnTrigger(object sender, EventArgs args)
        {
            Evaluate(Time.fixedDeltaTime);
        }

        private void Evaluate(float dT)
        {
            animator.Evaluate(dT);

            switch (transitionState)
            {
                case TransitionState.QueueTransition:
                    {
                        oldHip = CurrentReferenceFrame;
                        animator.PlayFromFrame(foundFrame);
                        transitionState = TransitionState.RootShift;
                        break;
                    }

                case TransitionState.RootShift:
                    {
                        RootProjection newHip = CurrentReferenceFrame;
                        Quaternion rotDiff = Quaternion.Euler(0, oldHip.YEuler - newHip.YEuler, 0);
                        model.rotation = rotDiff * model.rotation;
                        model.position += oldHip.position - new Vector3(fauxRoot.position.x, 0, fauxRoot.position.z);
                        transitionState = TransitionState.Blend;
                        break;
                    }

                case TransitionState.Blend:
                    {
                        transitionState = TransitionState.Playing;
                        break;

                    }

                case TransitionState.Playing:
                    {

                        break;

                    }

                case TransitionState.Skip:
                    {
                        transitionState = TransitionState.Playing;
                        if (inertializer) inertializer.BlendStep(dT);
                        frameReadyHandler?.Invoke(this, EventArgs.Empty);
                        return;

                    }
            }


            //rig.TrackKinematics();

            if (transitionState != TransitionState.Playing)
            {

                if (inertializer) inertializer.BlendStep(dT);
                frameReadyHandler?.Invoke(this, EventArgs.Empty);
                return;
            }
            matchDelayCountdown = ++matchDelayCountdown % settings.skipFactor;
            if (matchDelayCountdown == 0)
            {
                RootProjection curRefFrame = CurrentReferenceFrame;
                var inputFeatures = input.CurrentTrajectoryAndDirection.Select(v => curRefFrame.InverseTransform(v)).ToList();

                if ((inputFeatures[0] - inputFeatures[1]).magnitude < idleLimit)
                {
                    if (animator.CurrentClip != animator.ClipCount - 1)
                    {
                        foundFrame = new MotionMatcher.MMFrame(animator.ClipCount - 1, 0);
                        transitionState = TransitionState.QueueTransition;
                    }
                }
                else
                {
                    var queryFrame = CurFrame;
                    if (queryFrame.ClipIdx == animator.ClipCount - 1) queryFrame = settings.StartingFrame;
                    foundFrame = motionMatcher.Match(inputFeatures, queryFrame);

                    if (!foundFrame.IsClose(CurFrame, settings.skipTolerance))
                    {
                        transitionState = TransitionState.QueueTransition;
                    }
                }
            }

            if (inertializer) inertializer.BlendStep(dT);
            frameReadyHandler?.Invoke(this, EventArgs.Empty);
        }

        private void FixedUpdate()
        {
            if (updateAlone) Evaluate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }


        public void CycleAnimatorClip()
        {
            transitionState = TransitionState.QueueTransition;
            foundFrame = new MotionMatcher.MMFrame((animator.CurrentClip + 1) % matchingDataset.motionList.Count, animator.CurrentTime);
        }

        public void RestartAnimation()
        {
            animator.JumpToProgress(0);
        }

        [Serializable]
        public class ControllerSettings
        {
            public int skipFactor = 10;
            public double skipTolerance = 0.2;
            public float maxVelocity = 1.2f;
            public float dampingEigenv = -3;
            public int margin = 15;

            [SerializeField]
            EditorFriendlyFrame startingFrame;
            public MatchingWeights weights;

            public MotionMatcher.MMFrame StartingFrame
            {
                get
                {
                    return new MotionMatcher.MMFrame(startingFrame.ClipIdx, startingFrame.TimeInClip);
                }
            }

            [Serializable]
            struct EditorFriendlyFrame
            {
                public int ClipIdx;
                public double TimeInClip;
            }
        }


    }



    class Constraint
    {
        GameObject lLower;
        GameObject rLower;
        GameObject rHand;
        GameObject lHand;

        public void EnforceConstraint()
        {
            lHand.transform.rotation = lLower.transform.rotation;
            rHand.transform.rotation = rLower.transform.rotation;
        }

        public Constraint(ArmHandLabel leftSide, ArmHandLabel rightSide)
        {
            lLower = GameObject.Find(leftSide.ForearmLabel);
            rLower = GameObject.Find(rightSide.ForearmLabel);
            lHand = GameObject.Find(leftSide.HandLabel);
            rHand = GameObject.Find(rightSide.HandLabel);
        }

        public Constraint(ArmHandObject leftSide, ArmHandObject rightSide)
        {
            lLower = leftSide.ForearmObject;
            rLower = rightSide.ForearmObject;
            lHand = leftSide.HandObject;
            rHand = rightSide.HandObject;
        }

        public Constraint()
        {
            lLower = GameObject.Find("mixamorig:LeftForeArm");
            rLower = GameObject.Find("mixamorig:RightForeArm");
            lHand = GameObject.Find("mixamorig:LeftHand");
            rHand = GameObject.Find("mixamorig:RightHand");
        }


        public struct ArmHandObject
        {
            public GameObject ForearmObject { get; }
            public GameObject HandObject { get; }

            public ArmHandObject(GameObject forearmObject, GameObject handObject)
            {
                ForearmObject = forearmObject;
                HandObject = handObject;
            }
        }

        public struct ArmHandLabel
        {
            public string ForearmLabel { get; }
            public string HandLabel { get; }

            public ArmHandLabel(string forearmLabel, string handLabel)
            {
                ForearmLabel = forearmLabel;
                HandLabel = handLabel;
            }
        }
    }
}