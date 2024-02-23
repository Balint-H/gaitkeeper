using MotionMatch;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathologicalIntent : MonoBehaviour, IAnimationController
{
    [SerializeField]
    GameObject inputObject;

    [SerializeField]
    float velocityScale;

    [SerializeField]
    float velocitySmoothingFactor;

    IMMInput landmarkGenerator;

    Vector3 prevIntent;

    private void Awake()
    {
        landmarkGenerator = inputObject.GetComponent<IMMInput>();
    }

    public Vector3 GetDesiredVelocity()
    {
        var newIntent = landmarkGenerator.AnalogueDirection.ProjectTo3D()*velocityScale;
        prevIntent = Vector3.Lerp(newIntent, prevIntent, velocitySmoothingFactor);
        return prevIntent;

    }

    public void OnAgentInitialize()
    {

    }

    public void OnReset()
    {

    }

}