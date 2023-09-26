using MotionMatch;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AutoInput : DampedTrajectoryInput
{

    [SerializeField]
    protected List<Transform> landmarks;

    [SerializeField]
    protected LandmarkSelection landmarkSelectionMode;

    protected int landmarkIdx;
    protected Vector3 hitPoint;
    protected bool targetIsActive;

    [SerializeField]
    protected TrackCircle circle;

    [SerializeField]
    public Transform fauxRootInWorld;

    [SerializeField]
    float distThreshold;

    [SerializeField]
    float targetDistance;

    [SerializeField]
    WaitSettings waitSettings;

    [SerializeField]
    bool projectDownForDistance;

    public bool TargetIsActive
    {
        get => targetIsActive;
        set
        {
            if (targetIsActive != value)
            {
                if (value == true)
                {
                    circle.transform.position = new Vector3(hitPoint.x, 0.05f, hitPoint.z);
                    targetIsActive = true;
                }
                else
                {
                    targetIsActive = false;
                    float waitTime = waitSettings.SampleWaitTime();
                    if (waitTime == 0f)
                    {
                        hitPoint = GetNextPoint();

                        TargetIsActive = true;
                    }
                    else
                    {
                        StartCoroutine(SetNewTarget(waitTime));
                    }



                }
            }

        }
    }

    IEnumerator SetNewTarget(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        hitPoint = GetNextPoint();

        TargetIsActive = true;

    }



    protected override void InitializeInput()
    {
        targetIsActive = true;
        TargetIsActive = false;
    }

    protected override void UpdateDirection()
    {
        if (TargetIsActive)
        {
            Vector3 differenceVector = projectDownForDistance ? fauxRootInWorld.position.Horizontal3D() - hitPoint : fauxRootInWorld.position - hitPoint;
            if (differenceVector.magnitude < targetDistance)
            {
                analogueDirection = Vector2.zero;
                TargetIsActive = false;
            }
            else
            {
                Vector2 projectedDirection = new Vector3(differenceVector.x, differenceVector.z);

                analogueDirection = Vector2.ClampMagnitude(-projectedDirection, 1);
            }
        }
    }

    private Vector3 GetNextPoint()
    {
        switch (landmarkSelectionMode)
        {
            case LandmarkSelection.Random: return GetRandomPoint();
            case LandmarkSelection.OrderedLandmark: return GetNextLandmark();
            case LandmarkSelection.RandomLandmark: return GetRandomLandmark();
            default: return GetNextLandmark();
        }
    }

    private Vector3 GetNextLandmark()
    {
        landmarkIdx = (landmarkIdx + 1) % landmarks.Count;
        return landmarks[landmarkIdx].position;
    }

    private Vector3 GetRandomLandmark()
    {
        landmarkIdx = Random.Range(0, landmarks.Count);
        return landmarks[landmarkIdx].position;
    }

    private Vector3 GetRandomPoint()
    {
        landmarkIdx = -1;
        var bb = GeometryUtility.CalculateBounds(landmarks.Select(t => t.position).ToArray(), Matrix4x4.identity);
        var newPoint = new Vector3(Random.Range(bb.min.x, bb.max.x), Random.Range(bb.min.y, bb.max.y), Random.Range(bb.min.z, bb.max.z));
        return (fauxRootInWorld.position.Horizontal3D() + new Vector3(0, newPoint.y, 0) - newPoint).magnitude > distThreshold ? newPoint : GetRandomPoint();
    }

    public void SetHitPoint(Vector3 pos)
    {
        hitPoint = pos;
        targetIsActive = false;
        TargetIsActive = true;
    }



    [System.Serializable]
    protected struct WaitSettings
    {
        [SerializeField, Range(0,1)]
        float waitProbability;

        [SerializeField, Tooltip("In seconds")]
        Vector2 waitRange;

        public float SampleWaitTime()
        {
            if (waitProbability == 0f) return 0;
            float a = Random.value;
            if (a < (1f - waitProbability)) return 0f;
            else
            {
                float m = (waitRange.y - waitRange.x) / waitProbability;
                float b = waitRange.y - m;
                return m * a + b;
            }
        }
    }



}

public enum LandmarkSelection
{
    Random,
    OrderedLandmark,
    RandomLandmark,
}

public interface IMMInput
{
    public float Eignv { get; set; }
    public IEnumerable<Vector2> CurrentTrajectoryAndDirection { get; }

    public Vector2 AnalogueDirection { get; }
    public Vector2 AnalogueHeading { get; }
}