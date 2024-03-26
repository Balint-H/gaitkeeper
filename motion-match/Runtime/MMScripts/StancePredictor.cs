using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;
using UnityEngine.PlayerLoop;
using static MocapLoader;
using static UnityEngine.GraphicsBuffer;
using UnityEditorInternal.VR;
using static MotionMatcher;

namespace MotionMatch
{
    public class StancePredictor : MonoBehaviour
    {
        [SerializeField]
        MMDataset dataset;

        public MMDataset Dataset => dataset;

        [SerializeField]
        MMController controller;
        public MMController Controller => controller;

        [SerializeField]
        Transform animationRoot;

        [SerializeField]
        Transform leftFootBase;
        public Transform LeftFootBase => leftFootBase;

        [SerializeField]
        Transform rightFootBase;
        public Transform RightFootBase => rightFootBase;

        [SerializeField]
        public Transform leftFootBaseSource;

        [SerializeField]
        public Transform rightFootBaseSource;

        [SerializeField]
        TextAsset leftStanceFile;

        [SerializeField]
        TextAsset rightStanceFile;

        [SerializeField]
        bool visualize;

        public List<StanceList> leftStances;
        public List<StanceList> rightStances;

        float leftT;
        float rightT;

        public void Awake()
        {
            leftStances = JsonConvert.DeserializeObject<List<StanceList>>(leftStanceFile.text);
            rightStances = JsonConvert.DeserializeObject<List<StanceList>>(rightStanceFile.text);
            leftT = -1;
            rightT = -1;
            controller.ShiftPerformed += ForecastStep;
            controller.PreBlend += CheckForecast;
        }

        private void CheckForecast()
        {
            MotionMatcher.MMFrame curFrame = controller.CurFrame;
            if (curFrame.ClipIdx == -1 || curFrame.ClipIdx >= leftStances.Count)
            {
                leftFootBase.position = leftFootBaseSource.position.Horizontal3D();
                rightFootBase.position = rightFootBaseSource.position.Horizontal3D();
                return;
            }
            (var leftPos, var leftDir, var newLeftT) = leftStances[curFrame.ClipIdx].FindNext((float)curFrame.TimeInClip);
            (var rightPos, var rightDir, var newRightT) = rightStances[curFrame.ClipIdx].FindNext((float)curFrame.TimeInClip);

            if (newLeftT != leftT || newRightT != rightT)
            {
                leftT = newLeftT;
                rightT = newRightT;
                ForecastStep(newLeftT, newRightT);
            }
        }

        private void ForecastStep()
        {
            var curFrame = controller.CurFrame;
           

            if (curFrame.ClipIdx == -1 || curFrame.ClipIdx >= leftStances.Count)
            {
                leftFootBase.position = leftFootBaseSource.position.Horizontal3D();
                rightFootBase.position = rightFootBaseSource.position.Horizontal3D();
                return;
            }
            var storedTime = curFrame.TimeInClip;

            (var leftPos, var leftDir, var ltime) = leftStances[curFrame.ClipIdx].FindNext((float)curFrame.TimeInClip);
            (var rightPos, var rightDir, var rtime) = rightStances[curFrame.ClipIdx].FindNext((float)curFrame.TimeInClip);

            ForecastStep(ltime, rtime);
        }


        private void ForecastStep(float ltime, float rtime)
        {
            var curFrame = controller.CurFrame;
            var storedTime = curFrame.TimeInClip;

            Quaternion forwardOrientation;
            Vector3 globalPosition;

            controller.JumpToTime(ltime);
            controller.EvaluateCurrentPose();
            forwardOrientation = Quaternion.LookRotation(leftFootBaseSource.forward.Horizontal3D(), Vector3.up);
            globalPosition = leftFootBaseSource.position.Horizontal3D();
            PrepareForPrediction(globalPosition, forwardOrientation);
            leftFootBase.position = PostProcessPosition(globalPosition, forwardOrientation);
            leftFootBase.rotation = PostProcessRotation(leftFootBase.position, forwardOrientation);

            controller.JumpToTime(rtime);
            controller.EvaluateCurrentPose();
            forwardOrientation = Quaternion.LookRotation(leftFootBaseSource.forward.Horizontal3D(), Vector3.up);
            globalPosition = rightFootBaseSource.position.Horizontal3D();
            PrepareForPrediction(globalPosition, forwardOrientation);
            rightFootBase.position = PostProcessPosition(globalPosition, forwardOrientation);
            rightFootBase.rotation = PostProcessRotation(rightFootBase.position, forwardOrientation);

            controller.JumpToTime(storedTime);
            controller.EvaluateCurrentPose();
        }

        protected virtual Vector3 PostProcessPosition(Vector3 globalPosition, Quaternion globalForwardOrientation)
        {
            return globalPosition;
        }

        protected virtual Quaternion PostProcessRotation(Vector3 globalPosition, Quaternion globalForwardOrientation)
        {
            return globalForwardOrientation;
        }

        protected virtual void PrepareForPrediction(Vector3 globalPosition, Quaternion globalForwardOrientation)
        {

        }



        public class StanceList
        {
            public List<Vector2> localPos;
            public List<Vector2> localDir;
            public List<float> stanceTime;

            public static StanceList FromDeserializedList(List<float[]> data)
            {
                var stanceList = new StanceList();
                stanceList.localPos = data[0].Zip(data[1], (x, y) => new Vector2(y, -x)).ToList();
                stanceList.localDir = data[2].Zip(data[3], (x, y) => new Vector2(y, -x)).ToList();
                stanceList.stanceTime = data[4].ToList();
                return stanceList;
            }

            public (Vector2, Vector2, float) FindNext(float target)
            {
                int low = 0, high = localPos.Count;
                if (target>=stanceTime[high-1]) return (localPos[high-1], localDir[high-1], stanceTime[high-1]);
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
                return (localPos[low], localDir[low], stanceTime[low]);
            }
        }

    }
}
