using Mujoco;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mujoco.Extensions;

public class MjTiltHandler : TrainingEventHandler
{
    [SerializeField]
    MjFreeJoint tiltedJoint;

    [SerializeField]
    float maxTiltDegree;

    [SerializeField]
    float minTiltDegree;



    public override EventHandler Handler => (_,_) => MjState.ExecuteAfterMjStart(Tilt);


    unsafe void Tilt()
    {
        var degrees = UnityEngine.Random.Range(minTiltDegree, maxTiltDegree);
        Quaternion rotateQuat = Quaternion.RotateTowards(Quaternion.identity, UnityEngine.Random.rotation, degrees);
        var oldQpos = tiltedJoint.GetQPos();
        Quaternion oldQuat = new Quaternion(w: -oldQpos[3], x: oldQpos[4], y: oldQpos[6], z: oldQpos[5]);
        Vector3 oldPos = new Vector3(oldQpos[0], oldQpos[2], oldQpos[1]);
        MjEngineTool.SetMjTransform(MjScene.Instance.Data->qpos + tiltedJoint.QposAddress, oldPos, rotateQuat * oldQuat);
    }

}
