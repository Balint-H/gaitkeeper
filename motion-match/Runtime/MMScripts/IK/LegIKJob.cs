using UnityEngine;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.Animations;
#else
using UnityEngine.Experimental.Animations;
#endif
using Unity.Collections;

public struct LegIKJob : IAnimationJob
{
    public struct EffectorHandle
    {
        public TransformSceneHandle effector;
        public PropertySceneHandle positionWeight;
        public PropertySceneHandle rotationWeight;
        public PropertySceneHandle pullWeight;
    }

    public struct HintEffectorHandle
    {
        public TransformSceneHandle hint;
        public PropertySceneHandle weight;
    }


    public struct BodyEffectorHandle
    {
        public TransformSceneHandle body;
    }

    public EffectorHandle leftFootEffector;
    public EffectorHandle rightFootEffector;

    public HintEffectorHandle leftKneeHintEffector;
    public HintEffectorHandle rightKneeHintEffector;

    public BodyEffectorHandle bodyEffector;
    public Vector3 bodyPosition;

    public struct IKLimbHandle
    {
        public TransformStreamHandle top;
        public TransformStreamHandle middle;
        public TransformStreamHandle end;
        public float maximumExtension;
    }

    public IKLimbHandle leftLeg;
    public IKLimbHandle rightLeg;

    public float stiffness;
    public int maxPullIteration;

    private EffectorHandle GetEffectorHandle(AvatarIKGoal goal)
    {
        switch (goal)
        {
            default:
            case AvatarIKGoal.LeftFoot: return leftFootEffector;
            case AvatarIKGoal.RightFoot: return rightFootEffector;
        }
    }

    private IKLimbHandle GetIKLimbHandle(AvatarIKGoal goal)
    {
        switch (goal)
        {
            default:
            case AvatarIKGoal.LeftFoot: return leftLeg;
            case AvatarIKGoal.RightFoot: return rightLeg;
        }
    }

    private void SetEffector(AnimationStream stream, AvatarIKGoal goal, ref EffectorHandle handle)
    {
        if (handle.effector.IsValid(stream) && handle.positionWeight.IsValid(stream) && handle.rotationWeight.IsValid(stream))
        {
            AnimationHumanStream humanStream = stream.AsHuman();
            humanStream.SetGoalPosition(goal, handle.effector.GetPosition(stream));
            humanStream.SetGoalRotation(goal, handle.effector.GetRotation(stream));
            humanStream.SetGoalWeightPosition(goal, handle.positionWeight.GetFloat(stream));
            humanStream.SetGoalWeightRotation(goal, handle.rotationWeight.GetFloat(stream));
        }
    }

    private void SetHintEffector(AnimationStream stream, AvatarIKHint goal, ref HintEffectorHandle handle)
    {
        if (handle.hint.IsValid(stream) && handle.weight.IsValid(stream))
        {
            AnimationHumanStream humanStream = stream.AsHuman();
            humanStream.SetHintPosition(goal, handle.hint.GetPosition(stream));
            humanStream.SetHintWeightPosition(goal, handle.weight.GetFloat(stream));
        }
    }


    private void SetMaximumExtension(AnimationStream stream, ref IKLimbHandle handle)
    {
        if (handle.maximumExtension == 0)
        {
            Vector3 top = handle.top.GetPosition(stream);
            Vector3 middle = handle.middle.GetPosition(stream);
            Vector3 end = handle.end.GetPosition(stream);

            Vector3 localMiddle = middle - top;
            Vector3 localEnd = end - middle;

            handle.maximumExtension = localMiddle.magnitude + localEnd.magnitude;
        }
    }

    struct LimbPart
    {
        public Vector3 localPosition;    // local position of this limb relative to body position
        public Vector3 goalPosition;
        public float   goalWeight;
        public float   goalPullWeight;
        public float   maximumExtension; // maximum extension of the limb which define when the pull solver start to pull on the body (spring rest lenght)
        public float   stiffness;        // stiffness of the limb, at 0 the limb is loosen, at 1 the limb is really stiff
    }

    private void PrepareSolvePull(AnimationStream stream, NativeArray<LimbPart> limbParts)
    {
        AnimationHumanStream humanStream = stream.AsHuman();

        Vector3 bodyPosition = humanStream.bodyPosition;

        for (int goalIter = 0; goalIter < 4; goalIter++)
        {
            var effector = GetEffectorHandle((AvatarIKGoal)goalIter);
            var limbHandle = GetIKLimbHandle((AvatarIKGoal)goalIter);
            Vector3 top = limbHandle.top.GetPosition(stream);

            limbParts[goalIter] = new LimbPart {
                localPosition = top - bodyPosition,
                goalPosition = humanStream.GetGoalPosition((AvatarIKGoal)goalIter),
                goalWeight = humanStream.GetGoalWeightPosition((AvatarIKGoal)goalIter),
                goalPullWeight = effector.pullWeight.GetFloat(stream),
                maximumExtension = limbHandle.maximumExtension,
                stiffness = stiffness
            };
        }
    }

    private Vector3 SolvePull(AnimationStream stream)
    {
        AnimationHumanStream humanStream = stream.AsHuman();

        Vector3 originalBodyPosition = humanStream.bodyPosition;
        Vector3 bodyPosition = originalBodyPosition;

        NativeArray<LimbPart> limbParts = new NativeArray<LimbPart>(4, Allocator.Temp);
        PrepareSolvePull(stream, limbParts);
        
        for (int iter = 0; iter < maxPullIteration; iter++)
        {
            Vector3 deltaPosition = Vector3.zero;
            for (int goalIter = 0; goalIter < 4; goalIter++)
            {
                Vector3 top = bodyPosition + limbParts[goalIter].localPosition;
                Vector3 localForce = limbParts[goalIter].goalPosition - top;
                float restLenght = limbParts[goalIter].maximumExtension;
                float currentLenght = localForce.magnitude;

                localForce.Normalize();

                var force = Mathf.Max( limbParts[goalIter].stiffness * (currentLenght - restLenght), 0.0f);

                deltaPosition += (localForce * force * limbParts[goalIter].goalPullWeight * limbParts[goalIter].goalWeight);
            }

            deltaPosition /= (maxPullIteration - iter);
            bodyPosition += deltaPosition;
        }

        limbParts.Dispose();

        return bodyPosition - originalBodyPosition;
    }

    private void Solve(AnimationStream stream)
    {
        AnimationHumanStream humanStream = stream.AsHuman();

        bodyPosition = humanStream.bodyPosition;
        Vector3 bodyPositionDelta = SolvePull(stream);

        bodyPosition += bodyPositionDelta;
        humanStream.bodyPosition = bodyPosition;

        humanStream.SolveIK();
    }

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    { ;
        SetMaximumExtension(stream, ref leftLeg);
        SetMaximumExtension(stream, ref rightLeg);

        SetEffector(stream, AvatarIKGoal.LeftFoot, ref leftFootEffector);
        SetEffector(stream, AvatarIKGoal.RightFoot, ref rightFootEffector);

        SetHintEffector(stream, AvatarIKHint.LeftKnee, ref leftKneeHintEffector);
        SetHintEffector(stream, AvatarIKHint.RightKnee, ref rightKneeHintEffector);


        Solve(stream);
    }
}
