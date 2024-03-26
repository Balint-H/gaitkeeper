using MotionMatch;
using Mujoco.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.UI.Image;

public class RayCastStancePredictor : StancePredictor
{
    [SerializeField]
    float baseOfSupportEdgeLength;

    List<Vector3> bosPoints;
    Vector3 normal;
    Vector3 forward;

    protected override void PrepareForPrediction(Vector3 globalPosition, Quaternion globalForwardOrientation)
    {
        bosPoints = GetBoSPoints(globalPosition, globalForwardOrientation).Select(p => MjState.MjGroundRayCast(new Ray(p+Vector3.up*3, Vector3.down)).Item1).ToList();
    }

    protected override Vector3 PostProcessPosition(Vector3 globalPosition, Quaternion globalForwardOrientation)
    {
        var center = bosPoints[^1];
        return new Vector3(center.x, bosPoints.Max(p => p.y), center.z);
    }

    protected override Quaternion PostProcessRotation(Vector3 globalPosition, Quaternion globalForwardOrientation)
    {
        var vec1 = bosPoints[1] - bosPoints[0];
        var vec2 = bosPoints[2] - bosPoints[0];

        normal = Vector3.Cross(vec1/baseOfSupportEdgeLength, vec2/baseOfSupportEdgeLength).normalized;
        forward = (globalForwardOrientation * Vector3.forward).normalized;
        forward = (forward - Vector3.Dot(forward, normal) * normal).normalized;
        return Quaternion.LookRotation(forward, normal);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || bosPoints==null) return;

        foreach (var bosPoint in bosPoints)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bosPoint, 0.1f);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(bosPoints[^1], 0.1f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(bosPoints[^1], normal*1f);

        Gizmos.DrawRay(bosPoints[^1], forward*1f);
    }

    IEnumerable<Vector3> GetBoSPoints(Vector3 originPosition, Quaternion globalForwardOrientation)
    {
        var originForward = globalForwardOrientation * Vector3.forward;
        var originRight = globalForwardOrientation * Vector3.right;
        if (baseOfSupportEdgeLength > 0f)
        {
            yield return originPosition + 0.57735f * baseOfSupportEdgeLength * originForward;  // uses 1/sqrt(3)
            yield return originPosition - 0.288675f * baseOfSupportEdgeLength * originForward + 0.5f * baseOfSupportEdgeLength * originRight;
            yield return originPosition - 0.288675f * baseOfSupportEdgeLength * originForward - 0.5f * baseOfSupportEdgeLength * originRight;
            yield return originPosition;
        }
        else
        {
            yield return originPosition;
        }
    }
}
