using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using ModularAgents;

namespace GaitLab
{

    public class MjGaitEventDetector : MonoBehaviour
    {
        [SerializeField]
        MjContactForceAggregator leftFootGRF;

        [SerializeField]
        MjContactForceAggregator rightFootGRF;

        [SerializeField]
        Transform leftAnkleTransform;

        [SerializeField]
        Transform rightAnkleTransform;

        GaitEventCollection events;

        [SerializeField]
        bool shouldSave;

        [SerializeField]
        string fileName;

        private void Awake()
        {
            events = new GaitEventCollection();

            leftFootGRF.LiftOff += (object sender, EventArgs e) => events.LeftLiftOffs.AddEvent(Time.fixedTime, leftAnkleTransform.position);
            leftFootGRF.FootFall += (object sender, EventArgs e) => events.LeftFootFalls.AddEvent(Time.fixedTime, leftAnkleTransform.position);

            rightFootGRF.LiftOff += (object sender, EventArgs e) => events.RightLiftOffs.AddEvent(Time.fixedTime, rightAnkleTransform.position);
            rightFootGRF.FootFall += (object sender, EventArgs e) => events.RightFootFalls.AddEvent(Time.fixedTime, rightAnkleTransform.position);
        }

        private void OnApplicationQuit()
        {
            if (!shouldSave) return;
            string output = JsonConvert.SerializeObject(events);

            File.WriteAllText(Path.Combine(Application.dataPath, fileName), output);
        }

        private class GaitEventSeries
        {
            public List<float> timestamps;
            public List<float[]> ankleLocations;

            public GaitEventSeries()
            {
                this.timestamps = new List<float>();
                this.ankleLocations = new List<float[]>();
            }

            public void AddEvent(float t, Vector3 ankleLoc)
            {
                timestamps.Add(t);
                ankleLocations.Add(ankleLoc.GetComponents().ToArray());
            }
        }

        private class GaitEventCollection
        {
            public GaitEventSeries LeftLiftOffs;
            public GaitEventSeries LeftFootFalls;

            public GaitEventSeries RightLiftOffs;
            public GaitEventSeries RightFootFalls;

            public GaitEventCollection()
            {
                LeftLiftOffs = new GaitEventSeries();
                LeftFootFalls = new GaitEventSeries();
                RightLiftOffs = new GaitEventSeries();
                RightFootFalls = new GaitEventSeries();
            }
        }

    }
}