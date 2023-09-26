using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ModularAgents.Kinematic;

public class MMKinematicRigSubscriber : MonoBehaviour
{
    [SerializeField] 
    MjKinematicRig rig;

    [SerializeField]
    MMController controller;

    private void Awake()
    {
        if (!rig.enabled) return;
        controller.frameReadyHandler += (object sender, EventArgs args) => rig.TrackKinematics();
    }

}
