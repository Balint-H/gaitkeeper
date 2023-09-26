using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using ModularAgents.Kinematic;
using ModularAgents.Kinematic.Mujoco;
using Mujoco;
using Newtonsoft.Json;
using System.IO;

namespace GaitLab
{
    namespace Mujoco
    {
        public class KinematicSampler : TrainingEventHandler
        {
            public override EventHandler Handler => CollectPositions;

            [SerializeField]
            List<MjBody> trackedBodies;

            List<IKinematic> trackedKinematics;

            [SerializeField]
            ValueRecorder recorder;

            [SerializeField]
            bool shouldExportStats;

            private Dictionary<string, float> massStats;


            unsafe void CollectPositions(object sender, EventArgs e)
            {
                LazyInitializeKinematics();

                if (!recorder) return;
                foreach (var k in trackedKinematics)
                {
                    recorder.Record(k.CenterOfMass, k.Name);
                }
            }

            public void ExportStats(object sender, EventArgs e)
            {
                var jout = JsonConvert.SerializeObject(massStats, Formatting.Indented);
                File.WriteAllText(Path.Combine(Application.dataPath, "mass_stats.json"), jout);
            }    

            private void LazyInitializeKinematics()
            {
                if (trackedKinematics != null && trackedKinematics.Count > 0) return;
                trackedKinematics = trackedBodies.Select(mjb => MjKinematicExtensions.GetIKinematic(mjb.transform)).ToList();
                if (recorder && shouldExportStats)
                {
                    LazyInitializeKinematics();
                    recorder.OnExport += ExportStats;
                    massStats = trackedKinematics.ToDictionary(k => k.Name, k => k.Mass);
                }
            }
        }
    }
}