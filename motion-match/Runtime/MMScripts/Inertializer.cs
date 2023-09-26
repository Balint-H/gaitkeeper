using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/*public class Inertializer : MonoBehaviour
{
    IEnumerable<InertializedRotation> inertializedRotations;
    InertializedHeight inertializedRootHeight;
    public float blendTime;
    public void UpdateBlendTimes(float blendTime)
    {
        this.blendTime = blendTime;
        foreach (var inertializedRotation in inertializedRotations)
        {
            inertializedRotation.BlendTime = this.blendTime;
        }
    }

    public Transform root;

    private void Start()
    {
        IEnumerable<Transform> subscribingTransforms = Utility.FlattenTransformHierarchy(root).ToList();
        inertializedRotations = subscribingTransforms.Select(x => new InertializedRotation(x, blendTime)).ToList();
        inertializedRootHeight = new InertializedHeight(subscribingTransforms.First(), blendTime);

        float deltaTime = Time.deltaTime;
        foreach (InertializedRotation inertializedRotation in inertializedRotations)
        {
            inertializedRotation.UpdateTrack(deltaTime);
        }

        inertializedRootHeight.UpdateTrack(deltaTime);

    }

    // Update is called once per frame
    void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        foreach (InertializedRotation inertializedRotation in inertializedRotations)
        {
            inertializedRotation.Blend(deltaTime);
        }

        inertializedRootHeight.Blend(deltaTime);
    }

    class InertializedRotation
    {
        public Transform transform;
        InertializationEvaluator<QuaternionState, Quaternion> blender;

        public float BlendTime
        {
            get
            {
                return blender.BlendTime;
            }

            set
            {
                blender.BlendTime = value;
            }
        }

        public InertializedRotation(Transform transform, float blendTime)
        {
            this.transform = transform;
            blender = new InertializationEvaluator<QuaternionState, Quaternion>(transform.localRotation, blendTime);
        }

        public void UpdateTrack(float deltatime)
        {
            blender.UpdateState(transform.localRotation, deltatime);
        }

        public void Blend(float deltaTime)
        {
            transform.localRotation = blender.BlendTowards(transform.localRotation, deltaTime);
            blender.UpdateState(transform.localRotation, deltaTime);
        }
    }

    class InertializedHeight
    {
        Transform transform;
        InertializationEvaluator<ScalarState, float> blender;

        public float BlendTime
        {
            get
            {
                return blender.BlendTime;
            }

            set
            {
                blender.BlendTime = value;
            }
        }

        public InertializedHeight(Transform transform, float blendTime)
        {
            this.transform = transform;
            blender = new InertializationEvaluator<ScalarState, float>(transform.position.y, blendTime);
        }

        public void UpdateTrack(float deltatime)
        {
            blender.UpdateState(transform.position.y, deltatime);
        }


        public void Blend(float deltaTime)
        {
            transform.position = new Vector3(transform.position.x, blender.BlendTowards(transform.position.y, deltaTime), transform.position.z);
            blender.UpdateState(transform.position.y, deltaTime);
        }
    }


    class InertializationEvaluator<TBlend, U> where TBlend : IInertializationState<U>, new()
    {
        TBlend curState;
        float blendTime;
        public float BlendTime { get => blendTime; set => blendTime = value; }

        public InertializationEvaluator(U firstPose, float blendTime)
        {
            this.blendTime = blendTime;

            curState = new TBlend();
            curState.UpdatePose(firstPose, 1f);
            curState.TargetPose = firstPose;
        }



        public void UpdateState(U curPose, float deltaTime)
        {
            curState.UpdatePose(curPose, deltaTime);
        }

        public U BlendTowards(U targetPose, float deltaTime)
        {
            curState.TargetPose = targetPose;

            curState.PrepareState();

            float x0 = curState.X0;
            float v0 = curState.V0;
            if (Mathf.Abs(x0) < 0.001f) return targetPose;
            float t1 = blendTime;
            //t1 = Mathf.Min(blendTime, -5 * (x0 + float.Epsilon) / (v0 - float.Epsilon));

            float a0 = (-8 * v0 * t1 - 20 * x0) / (t1 * t1);
            //if (a0 < 0) a0 = 0;

            float A = -(a0 * t1 * t1 + 6 * v0 * t1 + 12 * x0) / (2 * t1 * t1 * t1 * t1 * t1);
            float B = (3 * a0 * t1 * t1 + 16 * v0 * t1 + 30 * x0) / (2 * t1 * t1 * t1 * t1);
            float C = -(3 * a0 * t1 * t1 + 12 * v0 * t1 + 20 * x0) / (2 * t1 * t1 * t1);

            float t = deltaTime;
            float xt = A * (t * t * t * t * t) + B * (t * t * t * t) + C * (t * t * t) + a0 / 2 * (t * t) + v0 * t + x0;

            U outPose = curState.GetBlendedAtState(xt);
            return outPose;
        }


        struct BlendParams
        {
            public float A;
            public float B;
            public float C;
            public float a0;
            public float v0;
            public float x0;

            public BlendParams(float a, float b, float c, float a0, float v0, float x0)
            {
                A = a;
                B = b;
                C = c;
                this.a0 = a0;
                this.v0 = v0;
                this.x0 = x0;
            }

            public float EvaluateQuinticModel(float t)
            {
                return A * (t * t * t * t * t) + B * (t * t * t * t) + C * (t * t * t) + a0 / 2 * (t * t) + v0 * t + x0;
            }
        }



    }

    public interface IInertializationState<T>
    {
        public float X0 { get; }
        public float V0 { get; }

        public T TargetPose { get; set; }

        public void PrepareState();
        public T GetBlendedAtState(float stateIn);
        public void UpdatePose(T poseIn, float deltaTime);
    }

    class QuaternionState : IInertializationState<Quaternion>
    {

        Quaternion startPose;
        Quaternion pastPose;
        Quaternion targetPose;
        public Quaternion TargetPose { get => targetPose; set => targetPose = value; }


        public void UpdatePose(Quaternion value, float deltatimeIn)
        {
            pastPose = startPose;
            startPose = value;
            deltatime = deltatimeIn;
        }

        public void PrepareState()
        {
            Q0.ToAngleAxis(out x0, out axis);
        }

        float deltatime;
        float x0;
        public float X0 { get => x0; }
        Vector3 axis;


        Quaternion Q0
        {
            get
            {
                return startPose * Quaternion.Inverse(targetPose);
            }
        }

        Quaternion Q_1
        {
            get
            {
                return pastPose * Quaternion.Inverse(targetPose);
            }
        }

        float X_1
        {
            get
            {
                Quaternion q = Q_1;
                Vector3 pastQuatAxis = new Vector3(q.x, q.y, q.z);

                return 2 * 180 / Mathf.PI * Mathf.Atan2(Vector3.Dot(pastQuatAxis, axis), q.w);
            }

        }

        public float V0
        {
            get
            {
                float diff = x0 - X_1;
                //if (diff * x0 <= 0) return 0;
                return diff / deltatime;
            }
        }

        public Quaternion GetBlendedAtState(float stateIn)
        {
            return Quaternion.AngleAxis(stateIn, axis) * targetPose;
        }


    }

    class ScalarState : IInertializationState<float>
    {

        float startPose;
        float pastPose;
        float targetPose;
        public float TargetPose { get => targetPose; set => targetPose = value; }


        public void UpdatePose(float value, float deltatimeIn)
        {
            pastPose = startPose;
            startPose = value;
            deltatime = deltatimeIn;
        }


        float deltatime;
        public float X0 { get => startPose - targetPose; }

        float X_1
        {
            get => pastPose - targetPose;
        }

        public float V0
        {
            get
            {
                float diff = X0 - X_1;
                //if (diff * X0 <= 0) return 0;
                return diff / deltatime;
            }
        }

        public float GetBlendedAtState(float scalar)
        {
            return scalar + targetPose;
        }

        public void PrepareState()
        {

        }


    }
}*/

public class Inertializer : MonoBehaviour
{
    IEnumerable<InertializedRotation> inertializedRotations;
    InertializedHeight inertializedRootHeight;
    float blendTime;
    List<string> slowTransforms = new List<string> { "Hand", "Arm", "Neck", "Head", "Hip"};

    public void UpdateBlendTimes(float blendTime)
    {
        this.blendTime = blendTime;
        foreach (var inertializedRotation in inertializedRotations)
        {
            inertializedRotation.BlendTime = this.blendTime;
        }
    }

    public void Initialize(Transform root, float blendTime)
    {
        this.blendTime = blendTime;
        IEnumerable<Transform> subscribingTransforms = MMUtility.FlattenTransformHierarchy(root).ToList();
        inertializedRotations = subscribingTransforms.Select(x => new InertializedRotation(x, blendTime)).ToList();
        inertializedRootHeight = new InertializedHeight(subscribingTransforms.First(), blendTime);

        float deltaTime = Time.deltaTime;
        foreach (InertializedRotation inertializedRotation in inertializedRotations)
        {
            if (slowTransforms.Any(inertializedRotation.transform.name.Contains)) inertializedRotation.BlendTime *= 3;
            inertializedRotation.UpdateTrack(deltaTime);
        }

        inertializedRootHeight.UpdateTrack(deltaTime);

    }

    // Update is called once per frame
    public void BlendStep(float deltaTime)
    {
        foreach (InertializedRotation inertializedRotation in inertializedRotations)
        {
            inertializedRotation.Blend(deltaTime);
        }

        inertializedRootHeight.Blend(deltaTime);
    }

    public void TrackStep(float deltaTime)
    {
        foreach (InertializedRotation inertializedRotation in inertializedRotations)
        {
            inertializedRotation.UpdateTrack(deltaTime);
        }

        inertializedRootHeight.UpdateTrack(deltaTime);
    }

    class InertializedRotation
    {
        public Transform transform;
        InertializationEvaluator<QuaternionState, Quaternion> blender;

        public float BlendTime
        {
            get
            {
                return blender.BlendTime;
            }

            set
            {
                blender.BlendTime = value;
            }
        }

        public InertializedRotation(Transform transform, float blendTime)
        {
            this.transform = transform;
            blender = new InertializationEvaluator<QuaternionState, Quaternion>(transform.localRotation, blendTime);
        }

        public void UpdateTrack(float deltatime)
        {
            blender.UpdateState(transform.localRotation, deltatime);
        }

        public void Blend(float deltaTime)
        {
            transform.localRotation = blender.BlendTowards(transform.localRotation, deltaTime);
            blender.UpdateState(transform.localRotation, deltaTime);
        }
    }

    class InertializedHeight
    {
        Transform transform;
        InertializationEvaluator<ScalarState, float> blender;

        public float BlendTime
        {
            get
            {
                return blender.BlendTime;
            }

            set
            {
                blender.BlendTime = value;
            }
        }

        public InertializedHeight(Transform transform, float blendTime)
        {
            this.transform = transform;
            blender = new InertializationEvaluator<ScalarState, float>(transform.position.y, blendTime);

        }

        public void UpdateTrack(float deltatime)
        {
            blender.UpdateState(transform.position.y, deltatime);
        }


        public void Blend(float deltaTime)
        {
            transform.position = new Vector3(transform.position.x, blender.BlendTowards(transform.position.y, deltaTime), transform.position.z);
            blender.UpdateState(transform.position.y, deltaTime);
        }
    }


    class InertializationEvaluator<TBlend, U> where TBlend : IInertializationState<U>, new()
    {
        TBlend curState;
        float blendTime;
        public float BlendTime { get => blendTime; set => blendTime = value; }

        public InertializationEvaluator(U firstPose, float blendTime)
        {
            this.blendTime = blendTime;

            curState = new TBlend();
            curState.UpdatePose(firstPose, blendTime);
            curState.TargetPose = firstPose;
        }



        public void UpdateState(U curPose, float deltaTime)
        {
            curState.UpdatePose(curPose, deltaTime);
        }

        public U BlendTowards(U targetPose, float deltaTime)
        {
            curState.TargetPose = targetPose;

            curState.PrepareState();

            float x0 = curState.X0;
            float v0 = curState.V0;
            if (Mathf.Abs(x0) < 0.001f) return targetPose;
            float t1 = blendTime;
            //t1 = Mathf.Min(blendTime, -5 * (x0 + float.Epsilon) / (v0 - float.Epsilon));

            float a0 = (-8 * v0 * t1 - 20 * x0) / (t1 * t1);
            //if (a0 < 0) a0 = 0;

            float A = -(a0 * t1 * t1 + 6 * v0 * t1 + 12 * x0) / (2 * t1 * t1 * t1 * t1 * t1);
            float B = (3 * a0 * t1 * t1 + 16 * v0 * t1 + 30 * x0) / (2 * t1 * t1 * t1 * t1);
            float C = -(3 * a0 * t1 * t1 + 12 * v0 * t1 + 20 * x0) / (2 * t1 * t1 * t1);

            float t = Mathf.Min(deltaTime, 0.05f);
            float xt = A * (t * t * t * t * t) + B * (t * t * t * t) + C * (t * t * t) + a0 / 2 * (t * t) + v0 * t + x0;

            U outPose = curState.GetBlendedAtState(xt);
            return outPose;
        }


        struct BlendParams
        {
            public float A;
            public float B;
            public float C;
            public float a0;
            public float v0;
            public float x0;

            public BlendParams(float a, float b, float c, float a0, float v0, float x0)
            {
                A = a;
                B = b;
                C = c;
                this.a0 = a0;
                this.v0 = v0;
                this.x0 = x0;
            }

            public float EvaluateQuinticModel(float t)
            {
                return A * (t * t * t * t * t) + B * (t * t * t * t) + C * (t * t * t) + a0 / 2 * (t * t) + v0 * t + x0;
            }
        }



    }

    public interface IInertializationState<T>
    {
        public float X0 { get; }
        public float V0 { get; }

        public T TargetPose { get; set; }

        public void PrepareState();
        public T GetBlendedAtState(float stateIn);
        public void UpdatePose(T poseIn, float deltaTime);
    }

    class QuaternionState : IInertializationState<Quaternion>
    {

        Quaternion startPose;
        Quaternion pastPose;
        Quaternion targetPose;
        public Quaternion TargetPose { get => targetPose; set => targetPose = value; }


        public void UpdatePose(Quaternion value, float deltatimeIn)
        {
            pastPose = startPose;
            startPose = value;
            deltatime = deltatimeIn;
        }

        public void PrepareState()
        {
            Q0.ToAngleAxis(out x0, out axis);
        }

        float deltatime;
        float x0;
        public float X0 { get => x0; }
        Vector3 axis;


        Quaternion Q0
        {
            get
            {
                return startPose * Quaternion.Inverse(targetPose);
            }
        }

        Quaternion Q_1
        {
            get
            {
                return pastPose * Quaternion.Inverse(targetPose);
            }
        }

        float X_1
        {
            get
            {
                Quaternion q = Q_1;
                Vector3 pastQuatAxis = new Vector3(q.x, q.y, q.z);

                return 2 * 180 / Mathf.PI * Mathf.Atan2(Vector3.Dot(pastQuatAxis, axis), q.w);
            }

        }

        public float V0
        {
            get
            {
                float diff = x0 - X_1;
                //if (diff * x0 <= 0) return 0;
                return diff / deltatime;
            }
        }

        public Quaternion GetBlendedAtState(float stateIn)
        {
            return Quaternion.AngleAxis(stateIn, axis) * targetPose;
        }


    }

    class ScalarState : IInertializationState<float>
    {

        float startPose;
        float pastPose;
        float targetPose;
        public float TargetPose { get => targetPose; set => targetPose = value; }


        public void UpdatePose(float value, float deltatimeIn)
        {
            pastPose = startPose;
            startPose = value;
            deltatime = deltatimeIn;
        }


        float deltatime;
        public float X0 { get => startPose - targetPose; }

        float X_1
        {
            get => pastPose - targetPose;
        }

        public float V0
        {
            get
            {
                float diff = X0 - X_1;
                //if (diff * X0 <= 0) return 0;
                return diff / deltatime;
            }
        }

        public float GetBlendedAtState(float scalar)
        {
            return scalar + targetPose;
        }

        public void PrepareState()
        {

        }


    }
}