using ModularAgents;
using Mujoco;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using System;

public class RotationDecomposer : MonoBehaviour
{

    [SerializeField]
    List<MjHingeJoint> hinges;

    [SerializeField]
    MjBody mjBody;

    Quaternion initRotation;

    private void Awake()
    {
        initRotation = mjBody.transform.localRotation;
    }

    unsafe void FixedUpdate()
    {
        var axes = hinges.Select(h => MjEngineTool.UnityVector3(MjScene.Instance.Data->xaxis + 3 * h.MujocoId) ).ToList();
        
        if(axes.Count == 1)
        {
            var orth = UnityEngine.Random.onUnitSphere;
            orth -= Vector3.Dot(orth, axes[0]) * axes[0];
            orth = orth.normalized;
            axes.Add(orth);
        }
        if (axes.Count == 2)
        {
            axes.Add(-1*Vector3.Cross(axes[0], axes[1]));
        }

        Matrix<float> basis = Matrix<float>.Build.DenseOfColumns(axes.Select(a=>a.GetComponents()));

        Matrix4x4 bodyRotation4x4 = Matrix4x4.Rotate(mjBody.transform.rotation);
        var columns = new Vector3[] { bodyRotation4x4.GetColumn(0), bodyRotation4x4.GetColumn(1), bodyRotation4x4.GetColumn(2) };
        Matrix<float> bodyRotation = Matrix<float>.Build.DenseOfColumns(columns.Select(c => c.GetComponents()));
        var changedBasisRotation = basis.Transpose() * bodyRotation * basis;

        Quaternion.Inverse(initRotation)

        var decomposed = EulerDecompose(changedBasisRotation);
        Debug.Log(string.Join(", ", decomposed)+'\n'+ string.Join(", ", hinges.Select(h => MjScene.Instance.Data->qpos[h.QposAddress])));


    }

    private static (float, float, float) EulerDecompose(Matrix<float> m)
    {
        return (Mathf.Atan2(-m[1,2], m[2,2]), Mathf.Asin(m[0, 2]), Mathf.Atan2(-m[0, 1], m[0,0]));
    }

    private static (float, float, float) EulerDecomposeZYX(Matrix<float> m)
    {
        return (Mathf.Atan2(m[1, 0], m[0, 2]), Mathf.Asin(-m[2, 0]), Mathf.Atan2(m[2, 1], m[2, 2]));
    }

    private unsafe void OnDrawGizmosSelected()
    {
        var axes = hinges.Select(h => MjEngineTool.UnityVector3(MjScene.Instance.Data->xaxis + 3 * h.MujocoId)).ToList();
        foreach (var (axis, h) in axes.Zip(hinges, Tuple.Create)) 
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(h.transform.position, axis*0.1f);
        }
    }
}
