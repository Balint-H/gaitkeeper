using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Mujoco.Extensions;


public class MjHeightIK : TrainingEventHandler
{
    [SerializeField]
    Transform origin;

    [SerializeField]
    Transform animRoot;

    [SerializeField]
    float smoothing;

    [SerializeField]
    bool updateAlone;

    [SerializeField]
    float baseOfSupportEdgeLength;

    [SerializeField]
    List<Transform> additionalBoSSources;

    public override EventHandler Handler => StepIK;

    public void StepIK(object sender, EventArgs e)
    {
        var height = GetBoSPoints().Select(p => MjState.MjGroundRayCast(new Ray(p, Vector3.down))).Max(t => t.Item1.y);

        animRoot.position = new Vector3(animRoot.position.x, smoothing * animRoot.position.y + (1 - smoothing) * height, animRoot.position.z);
    }

    IEnumerable<Vector3> GetBoSPoints()
    {
        if (baseOfSupportEdgeLength > 0f)
        {
            yield return origin.position + 0.57735f * baseOfSupportEdgeLength * origin.forward;  // uses 1/sqrt(3)
            yield return origin.position - 0.288675f * baseOfSupportEdgeLength * origin.forward + 0.5f * baseOfSupportEdgeLength * origin.right;
            yield return origin.position - 0.288675f * baseOfSupportEdgeLength * origin.forward - 0.5f * baseOfSupportEdgeLength * origin.right;
            yield return origin.position;
        }
        else
        {
            yield return origin.position;
        }
        foreach(var t in additionalBoSSources)
        {
            yield return new Vector3(t.position.x, origin.position.y, t.position.z);
        }
    }

    public void FixedUpdate()
    {
        if (!updateAlone) return;
        StepIK(this, EventArgs.Empty);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        var height = GetBoSPoints().Select(p => MjState.MjGroundRayCast(new Ray(p, Vector3.down))).Max(t => t.Item1.y);
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(new Vector3(origin.position.x, height, origin.position.z), 0.03f * Vector3.one);

        foreach (var bos in GetBoSPoints())
        {
            (var bosPosition, _, _) = MjState.MjGroundRayCast(new Ray(bos, Vector3.down));
            Gizmos.DrawSphere(bosPosition, 0.015f);
        }
    }


}
