using Newtonsoft.Json;
using System;
using UnityEditor;
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

        private static ParsedMetadata FromMetafile(string path)
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

        private static ParsedMetadata FromJson(string path)
        {
            string jsonText = System.IO.File.ReadAllText(path);
            return JsonConvert.DeserializeObject<ParsedMetadata>(jsonText);
        }

        private static ParsedMetadata FromH5(string path)
        {
            ParsedMetadata metadata = new ParsedMetadata();

            return metadata;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            MMDataset dataset = target as MMDataset;
            GUIStyle textStyle = GUI.skin.GetStyle("Label");
            textStyle.fontSize = 20;
            EditorGUILayout.LabelField("Motion Data:", textStyle);
            ShowElements(serializedObject.FindProperty("motionList"), dataset);
            GUILayout.Space(20f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nDimensions"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nTrajectoryPoints"));
            serializedObject.ApplyModifiedProperties();
            //GUIUtility.ExitGUI();
        }


        private static void ShowElements(SerializedProperty list, MMDataset dataset)
        {
            bool
                showElementLabels = true,
                showButtons = true;

            for (int i = 0; i < list.arraySize; i++)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


                EditorGUILayout.BeginVertical();

                if (showElementLabels)
                {
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
                } else
                {
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);
                }
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("markedUpMetadata")?.FindPropertyRelative("metadata") == null)
                {
                    EditorGUILayout.HelpBox($"(Element {i}) is missing motion data!", MessageType.Warning);
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

        private static void ShowButtons(SerializedProperty list, int index, MMDataset dataset)
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
                string path = EditorUtility.OpenFilePanel("Select Dataset File", "", "");

                if (!string.IsNullOrEmpty(path))
                {
                    dataset.motionList[index].markedUpMetada.metadata = FromMetafile(path);
                    dataset.motionList[index].markedUpMetada.Framecount = dataset.motionList[index].markedUpMetada.metadata.t_h[0].Length;
                    dataset.motionList[index].markedUpMetada.Freq = dataset.motionList[index].markedUpMetada.metadata.framerate;
                }
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