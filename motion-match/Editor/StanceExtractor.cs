/*
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static MotionMatch.StancePredictor;
using static MocapLoader;
using UnityEditorInternal.VR;

namespace MotionMatch
{
    public class StanceExtractor : Editor
    {
        [MenuItem("CONTEXT/StancePredictor/Extract Retargeted Stances")]
        private static void ExtractRetargetedStances(MenuCommand menuCommand)
        {
            var predictor = menuCommand.context as StancePredictor;
            MMAnimator mmAnimator = predictor.Controller.GetNewAnimator();
            List<StanceList> leftStances = new List<StanceList>();
            List<StanceList> rightStances = new List<StanceList>();
            for (int i = 0; i < predictor.Dataset.motionList.Count; i++)
            {
                ParsedMetadata json_data = JsonConvert.DeserializeObject<ParsedMetadata>(predictor.Dataset.motionList[i].markedUpMetada.file.text);
                leftStances.Add(StanceList.FromDeserializedList(json_data.l_stances.Transpose().Select(x => x.ToArray()).ToList()));
                rightStances.Add(StanceList.FromDeserializedList(json_data.r_stances.Transpose().Select(x => x.ToArray()).ToList()));
            }

            List<StanceList> leftProcessedStances = new List<StanceList>();
            List<StanceList> rightProcessedStances = new List<StanceList>();

            for (int i = 0; i < predictor.Dataset.motionList.Count; i++)
            {
                var curT = leftStances.Select(s => s.stanceTime).ElementAt(i);
                mmAnimator.SwitchToClip(i);
                leftProcessedStances.Add(new StanceList());

                leftProcessedStances[i].stanceTime = curT;
                leftProcessedStances[i].localPos = new List<Vector2>();
                leftProcessedStances[i].localDir = new List<Vector2>();
                foreach (float t in curT)
                {
                    mmAnimator.JumpToTime(t);
                    mmAnimator.Evaluate();
                    leftProcessedStances[i].localPos.Add(predictor.Controller.Model.InverseTransformPoint(predictor.leftFoot.position).Horizontal());
                    leftProcessedStances[i].localDir.Add(predictor.Controller.Model.InverseTransformDirection(predictor.leftFoot.forward).Horizontal());
                    Debug.Log($"Recorded Pos: {leftProcessedStances[i].localPos.Last()}");
                }
                Debug.Log($"Left stance times: {string.Join(", ", curT)}");

                curT = rightStances.Select(s => s.stanceTime).ElementAt(i);
                rightProcessedStances.Add(new StanceList());
                rightProcessedStances[i].stanceTime = curT;
                rightProcessedStances[i].localPos = new List<Vector2>();
                rightProcessedStances[i].localDir = new List<Vector2>();
                foreach (float t in curT)
                {
                    mmAnimator.JumpToTime(t);
                    mmAnimator.Evaluate();
                    rightProcessedStances[i].localPos.Add(predictor.Controller.Model.InverseTransformPoint(predictor.rightFoot.position).Horizontal());
                    rightProcessedStances[i].localDir.Add(predictor.Controller.Model.InverseTransformDirection(predictor.rightFoot.forward).Horizontal());
                }
                Debug.Log($"Right stance times: {string.Join(", ", curT)}");
            }
            var jsonText = JsonConvert.SerializeObject(leftProcessedStances, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            string path = EditorUtility.SaveFilePanel("Save JSON", "", "left_stances.json", "json");

            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, jsonText);
            }

            jsonText = JsonConvert.SerializeObject(rightProcessedStances, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            path = EditorUtility.SaveFilePanel("Save JSON", "", "right_stances.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, jsonText);
            }
        }

        [MenuItem("CONTEXT/StancePredictor/Extract Retargeted Stances SLOWLY")]
        private static void ScheduleSlow(MenuCommand menuCommand)
        {
            var predictor = menuCommand.context as StancePredictor;
            predictor.StartCoroutine(ExtractRetargetedStancesSlowly(menuCommand));
        }

        private static IEnumerator ExtractRetargetedStancesSlowly(MenuCommand menuCommand)
        {
            var predictor = menuCommand.context as StancePredictor;
            predictor.Controller.SwitchToClip(0);
            predictor.Controller.StopAnimation();

            MMAnimator mmAnimator = predictor.Controller.GetNewAnimator();
            List<StanceList> leftStances = new List<StanceList>();
            List<StanceList> rightStances = new List<StanceList>();
            for (int i = 0; i < predictor.Dataset.motionList.Count; i++)
            {
                ParsedMetadata json_data = JsonConvert.DeserializeObject<ParsedMetadata>(predictor.Dataset.motionList[i].markedUpMetada.file.text);
                leftStances.Add(StanceList.FromDeserializedList(json_data.l_stances.Transpose().Select(x => x.ToArray()).ToList()));
                rightStances.Add(StanceList.FromDeserializedList(json_data.r_stances.Transpose().Select(x => x.ToArray()).ToList()));
            }

            List<StanceList> leftProcessedStances = new List<StanceList>();
            List<StanceList> rightProcessedStances = new List<StanceList>();

            for (int i = 0; i < predictor.Dataset.motionList.Count; i++)
            {
                var curT = leftStances.Select(s => s.stanceTime).ElementAt(i);
                mmAnimator.SwitchToClip(i);
                leftProcessedStances.Add(new StanceList());

                leftProcessedStances[i].stanceTime = curT;
                leftProcessedStances[i].localPos = new List<Vector2>();
                leftProcessedStances[i].localDir = new List<Vector2>();
                foreach (float t in curT)
                {
                    mmAnimator.JumpToTime(t);
                    mmAnimator.Evaluate();
                    leftProcessedStances[i].localPos.Add(predictor.Controller.Model.InverseTransformPoint(predictor.leftFoot.position).Horizontal());
                    leftProcessedStances[i].localDir.Add(predictor.Controller.Model.InverseTransformPoint(predictor.leftFoot.forward).Horizontal());
                    predictor.LeftFootBase.position = predictor.Controller.Model.TransformPoint(leftProcessedStances[i].localPos.Last().ProjectTo3D());
                    Debug.Log($"Recorded Pos: {leftProcessedStances[i].localPos.Last()}");
                    yield return new WaitForSeconds(0.5f);
                }
                Debug.Log($"Left stance times: {string.Join(", ", curT)}");

                curT = rightStances.Select(s => s.stanceTime).ElementAt(i);
                rightProcessedStances.Add(new StanceList());
                rightProcessedStances[i].stanceTime = curT;
                rightProcessedStances[i].localPos = new List<Vector2>();
                rightProcessedStances[i].localDir = new List<Vector2>();
                foreach (float t in curT)
                {
                    mmAnimator.JumpToTime(t);
                    mmAnimator.Evaluate();
                    rightProcessedStances[i].localPos.Add(predictor.Controller.Model.InverseTransformPoint(predictor.rightFoot.position).Horizontal());
                    rightProcessedStances[i].localDir.Add(predictor.Controller.Model.InverseTransformPoint(predictor.rightFoot.forward).Horizontal());
                    predictor.RightFootBase.position = predictor.Controller.Model.TransformPoint(rightProcessedStances[i].localPos.Last().ProjectTo3D());
                    yield return new WaitForSeconds(0.5f);
                }
                Debug.Log($"Right stance times: {string.Join(", ", curT)}");
            }
  
        }
    }
}
*/
