using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MMUtility;


public class MMIntent : MonoBehaviour, IAnimationController
{
    [SerializeField]
    GameObject inputObject;

    IMMInput landmarkGenerator;

    private void Awake()
    {
        landmarkGenerator = inputObject.GetComponent<IMMInput>();
    }

    public Vector3 GetDesiredVelocity()
    {
        return landmarkGenerator.AnalogueDirection.ProjectTo3D();
    }

    public void OnAgentInitialize()
    {

    }

    public void OnReset()
    {

    }

}
