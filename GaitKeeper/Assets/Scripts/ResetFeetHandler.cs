using Mujoco;
using Mujoco.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResetFeetHandler : TrainingEventHandler
{
    [SerializeField]
    MjBody attachmentPoint;

    [SerializeField]
    MjFreeJoint footFreeJoint;

    IEnumerable<MjBaseJoint> joints;

    public override EventHandler Handler => (_,_) => TeleportFeet();


    private void Awake()
    {
        joints = footFreeJoint.GetComponentInParent<MjBody>().GetComponentsInChildren<MjBaseJoint>().Where(j => j is not MjFreeJoint).ToList();
    }

    public unsafe void TeleportFeet()
    {
        var attachmentPointVelocity = MjState.GlobalVelocity(attachmentPoint, objType: MujocoLib.mjtObj.mjOBJ_XBODY);
        var attachmentPointAngularVelocity = GlobalAngularVelocity(MjScene.Instance.Model, MjScene.Instance.Data, attachmentPoint, MujocoLib.mjtObj.mjOBJ_XBODY);
        var attachmentPointPosition = attachmentPoint.GlobalPosition();
        var attachmentPointRotation = attachmentPoint.GlobalRotation();
        MjEngineTool.SetMjTransform(MjScene.Instance.Data->qpos+footFreeJoint.QposAddress, attachmentPointPosition, attachmentPointRotation);
        MjEngineTool.SetMjVector3(MjScene.Instance.Data->qvel + footFreeJoint.DofAddress, attachmentPointVelocity);
        MjEngineTool.SetMjVector3(MjScene.Instance.Data->qvel + footFreeJoint.DofAddress + 3, attachmentPointAngularVelocity);

        foreach(var joint in joints)
        {
            ZeroJoint(MjScene.Instance.Data, joint);
        }

        MujocoLib.mj_forward(MjScene.Instance.Model, MjScene.Instance.Data);

    }

    private static unsafe Vector3 GlobalAngularVelocity(MujocoLib.mjModel_*  model, MujocoLib.mjData_* data, MjBody body, MujocoLib.mjtObj objType)
    {
        Vector3 bodyAngVel = Vector3.zero;
        double[] mjBodyAngVel = new double[6];
        fixed (double* res = mjBodyAngVel)
        {
            MujocoLib.mj_objectVelocity(
                model, MjScene.Instance.Data, (int)objType, body.MujocoId, res, 0);
            bodyAngVel = MjEngineTool.UnityVector3(MjEngineTool.MjVector3AtEntry(res, 0));
        }
        return bodyAngVel;
    }

    private static unsafe void ZeroJoint(MujocoLib.mjData_* data, MjBaseJoint joint)
    {
        switch (joint)
        {
            case MjHingeJoint hinge:
                data->qpos[joint.QposAddress] = 0;
                data->qvel[joint.DofAddress] = 0;
                break;
            default:
                throw new System.NotImplementedException("Joint type of foot not supported");
        }
    }    

}
