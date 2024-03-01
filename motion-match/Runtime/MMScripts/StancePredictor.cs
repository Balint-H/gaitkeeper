using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;
using UnityEngine.PlayerLoop;
using static MocapLoader;
using static UnityEngine.GraphicsBuffer;

namespace MotionMatch
{
    public class StancePredictor : MonoBehaviour
    {
        [SerializeField]
        MMDataset dataset;

        [SerializeField]
        MMController controller;

        [SerializeField]
        Transform animationRoot;

        [SerializeField]
        Transform leftFootBase;

        [SerializeField]
        Transform rightFootBase;

        [SerializeField]
        Transform leftFoot;

        [SerializeField]
        Transform rightFoot;

        [SerializeField]
        bool visualize;

        public List<StanceList> leftStances;
        public List<StanceList> rightStances;

        public void Awake()
        {
            leftStances = new List<StanceList>();
            rightStances = new List<StanceList>();
            for (int i = 0; i < dataset.motionList.Count; i++)
            {
                ParsedMetadata json_data = JsonConvert.DeserializeObject<ParsedMetadata>(dataset.motionList[i].markedUpMetada.file.text);
                leftStances.Add(StanceList.FromDeserializedList(json_data.l_stances.Transpose().Select(x=>x.ToArray()).ToList()));
                rightStances.Add(StanceList.FromDeserializedList(json_data.r_stances.Transpose().Select(x => x.ToArray()).ToList()));
            }
            if(!visualize) controller.frameReadyHandler += (_, _) => UpdateFootbases();
        }

        private void UpdateFootbases()
        {
            MotionMatcher.MMFrame curFrame = controller.CurFrame;
            if (curFrame.ClipIdx == -1 || curFrame.ClipIdx >= leftStances.Count)
            {
                leftFootBase.position = leftFoot.position.Horizontal3D();
                rightFootBase.position = rightFoot.position.Horizontal3D();
                return;
            }
            (var leftPos, var leftDir, _) = leftStances[curFrame.ClipIdx].FindNext((float)curFrame.TimeInClip);
            (var rightPos, var rightDir, _) = rightStances[curFrame.ClipIdx].FindNext((float)curFrame.TimeInClip);
            leftFootBase.position = animationRoot.TransformPoint(leftPos.ProjectTo3D());
            rightFootBase.position = animationRoot.TransformPoint(rightPos.ProjectTo3D());
        }

        private void FixedUpdate()
        {
            if (!visualize) return;
            var curFrame = new MotionMatcher.MMFrame(0, Time.fixedTime);
            
            if (curFrame.ClipIdx == -1 || curFrame.ClipIdx >= leftStances.Count)
            {
                leftFootBase.position = leftFoot.position.Horizontal3D();
                rightFootBase.position = rightFoot.position.Horizontal3D();
                return;
            }
            (var leftPos, var leftDir, var ltime) = leftStances[curFrame.ClipIdx].FindNext((float)curFrame.TimeInClip);
            (var rightPos, var rightDir, var rtime) = rightStances[curFrame.ClipIdx].FindNext((float)curFrame.TimeInClip);
            leftFootBase.position = animationRoot.TransformPoint(leftPos.ProjectTo3D())+transform.localPosition;
            rightFootBase.position = animationRoot.TransformPoint(rightPos.ProjectTo3D())+transform.localPosition;
            Debug.Log(ltime);
            Debug.Log(rtime);
        }

        public class StanceList
        {
            public List<Vector2> globalPos;
            public List<Vector2> globalDir;
            public List<float> stanceTime;

            public static StanceList FromDeserializedList(List<float[]> data)
            {
                var stanceList = new StanceList();
                stanceList.globalPos = data[0].Zip(data[1], (x, y) => new Vector2(y, -x)).ToList();
                stanceList.globalDir = data[2].Zip(data[3], (x, y) => new Vector2(y, -x)).ToList();
                stanceList.stanceTime = data[4].ToList();
                return stanceList;
            }

            public (Vector2, Vector2, float) FindNext(float target)
            {
                int low = 0, high = globalPos.Count;
                if (target>=stanceTime[high-1]) return (globalPos[high-1], globalDir[high-1], stanceTime[high-1]);
                while (low != high)
                {
                    int mid = (low + high) / 2;
                    if (stanceTime[mid] <= target)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid;
                    }
                }
                return (globalPos[low], globalDir[low], stanceTime[low]);
            }
        }

    }
}
