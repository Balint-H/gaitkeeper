using GaitLab;
using System;
using System.Collections.Generic;
using UnityEngine;
using Mujoco;
using System.Linq;

public class PoseErrorSampler : TrainingEventHandler
{
    public override EventHandler Handler => CollectPositions;

    [SerializeField]
    Transform simulationRoot; //Only the joints' local cartesian positions will be used.

    [SerializeField]
    Transform referenceRoot;

    [SerializeField]
    ValueRecorder recorder;

    IReadOnlyList<(MjBaseJoint, MjBaseJoint)> pairedJoints;


    unsafe void CollectPositions(object sender, EventArgs e)
    {
        if (!recorder)
        {
            Debug.Log(PosError);
            return;
        }

        recorder.Record(PosError, "PositionError");
    }

    private void Awake()
    {
        pairedJoints = FilterJoints(simulationRoot).Zip(FilterJoints(referenceRoot), (sJ, rJ) => (sJ, rJ)).ToList();
        foreach( var pair in pairedJoints)
        {
            if (!pair.Item2.name.Contains(pair.Item1.name))
            {
                Debug.Log(pair.Item1.name + ", " + pair.Item2.name);
                Debug.LogError("Mismatched joints in pose error eval!");
            }
        }
    }

    private float PosError
    {
        get
        {
            var simLoc = simulationRoot.position;
            var refLoc = referenceRoot.position;

            var errorSum = pairedJoints.Select(pair => ((pair.Item1.transform.position - simLoc) - (pair.Item2.transform.position - refLoc)).magnitude).Sum();

            return errorSum/pairedJoints.Count;

        }
    }

    public IEnumerable<MjBaseJoint> FilterJoints(Transform root)
    {
        return root.GetComponentsInChildren<MjBaseJoint>().GroupBy(j => j.transform.parent).Select(grp => grp.OrderBy(j => j.name).First()).OrderBy(j => j.name);
    }
}
