using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GaitLab
{
    public class PositionSampler : TrainingEventHandler
    {
        public override EventHandler Handler => CollectPositions;

        [SerializeField]
        List<Transform> trackedTransforms;

        [SerializeField]
        ValueRecorder recorder;


        unsafe void CollectPositions(object sender, EventArgs e)
        {
            if (!recorder) return;
            foreach (var tr in trackedTransforms)
            {
                recorder.Record(tr.position, tr.name);
            }
        }
    }
}