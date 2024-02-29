using Newtonsoft.Json;
using System;
using System.Data;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static MotionMatch.MMDataset;

namespace MotionMatch
{

    [CustomEditor(typeof(MMDataset))]
    public class MMDatasetEditor : Editor
    {
        private static GUIContent
            moveButtonContent = new GUIContent("\u21b4", "Move down"),
            reassignButtonContent = new GUIContent("\u21ba", "Reassign"),
            deleteButtonContent = new GUIContent("-", "Delete"),
            addButtonContent = new GUIContent("+", "Add");

        private static GUILayoutOption buttonWidth = GUILayout.Width(100f);

        private static SerializedMetadata FromMetafile(string path)
        {
            switch (System.IO.Path.GetExtension(path))
            {
                case ".json":
                    return FromJson(path);
                case ".bytes":
                    return FromH5(path);
                default:
                    throw new Exception($"File extension \"{System.IO.Path.GetExtension(path)}\" for metadata not supported");
            }
        }

        private static SerializedMetadata FromJson(string path)
        {
            string jsonText = System.IO.File.ReadAllText(path);
            return JsonConvert.DeserializeObject<SerializedMetadata.JsonParsedMetadata>(jsonText).ToSerializedMetadata();
        }

        private static SerializedMetadata FromH5(string path)
        {
            SerializedMetadata metadata = new SerializedMetadata();

            return metadata;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            MMDataset dataset = target as MMDataset;
            GUIStyle textStyle = GUI.skin.GetStyle("Label");
            int oldSize = textStyle.fontSize;
            textStyle.fontSize = 20;
            EditorGUILayout.LabelField("Motion Data:", textStyle);
            textStyle.fontSize = oldSize;
            ShowElements(serializedObject.FindProperty("motionList"), dataset);
            GUILayout.Space(20f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nDimensions"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nTrajectoryPoints"));
            serializedObject.ApplyModifiedProperties();
        }


        private void ShowElements(SerializedProperty list, MMDataset dataset)
        {
            bool
                showElementLabels = true,
                showButtons = true;
            
            for (int i = 0; i < list.arraySize; i++)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


                EditorGUILayout.BeginVertical();

                if (dataset.motionList?[i]?.markedUpMetadata?.metadata?.t_h == null ||
                    dataset.motionList?[i]?.markedUpMetadata?.metadata?.t_h.Count == 0 ||
                    dataset.motionList?[i]?.markedUpMetadata?.metadata?.t_h[0].Length == 0)
                {
                    EditorGUILayout.HelpBox($"(Element {i}) is missing motion data!", MessageType.Warning);
                }
                else if (showElementLabels)
                {
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i).FindPropertyRelative("Name"));
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i).FindPropertyRelative("clip"));
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i).FindPropertyRelative("markedUpMetadata").FindPropertyRelative("ranges"));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Frames:");
                    GUI.enabled = false;
                    var frameLabel = dataset.motionList[i].markedUpMetadata.metadata.t_h[0].Length.ToString();
                    EditorGUILayout.TextField(frameLabel);
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    var freqLabel = dataset.motionList[i].markedUpMetadata.Freq.ToString();
                    EditorGUILayout.LabelField("Frequency:");
                    GUI.enabled = false;
                    EditorGUILayout.TextField(freqLabel);
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                } else
                {
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);
                }
                
                if (showButtons)
                {
                    GUILayout.Space(15f);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    ShowButtons(list, i, dataset);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    
                }
                EditorGUILayout.EndVertical();
            }

            if (showButtons && list.arraySize == 0 && GUILayout.Button(addButtonContent, EditorStyles.miniButton, buttonWidth))
            {
                list.arraySize += 1;
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        private void LoadFile(MMDataset dataset, int index)
        {
            string path = EditorUtility.OpenFilePanel("Select Dataset File", "", "");

            if (!string.IsNullOrEmpty(path))
            {
                dataset.motionList[index].markedUpMetadata.metadata = FromMetafile(path);
                dataset.motionList[index].markedUpMetadata.Framecount = dataset.motionList[index].markedUpMetadata.metadata.t_h[0].Length;
                dataset.motionList[index].markedUpMetadata.Freq = dataset.motionList[index].markedUpMetadata.metadata.framerate;
            }
            EditorUtility.SetDirty(dataset);
            serializedObject.ApplyModifiedProperties();
        }

        private void ShowButtons(SerializedProperty list, int index, MMDataset dataset)
        {
            if (GUILayout.Button(moveButtonContent, EditorStyles.miniButtonLeft, buttonWidth))
            {
                list.MoveArrayElement(index, index + 1);
            }
            GUILayout.Space(5f);
            if (GUILayout.Button(addButtonContent, EditorStyles.miniButtonMid, buttonWidth))
            {
                list.InsertArrayElementAtIndex(index);
            }
            GUILayout.Space(5f);
            if (GUILayout.Button(reassignButtonContent, EditorStyles.miniButtonMid, buttonWidth))
            {
                EditorApplication.delayCall += () => LoadFile(dataset, index);
            }
            GUILayout.Space(5f);
            if (GUILayout.Button(deleteButtonContent, EditorStyles.miniButtonRight, buttonWidth))
            {
                int oldSize = list.arraySize;
                list.DeleteArrayElementAtIndex(index);
                if (list.arraySize == oldSize)
                {
                    list.DeleteArrayElementAtIndex(index);
                }
            }
        }

    }
}