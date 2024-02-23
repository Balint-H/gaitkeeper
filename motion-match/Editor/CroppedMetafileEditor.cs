using UnityEngine;
using UnityEditor;
using static MotionMatch.MMDataset;
using System.IO;
using System;
using Newtonsoft.Json;

namespace MotionMatch
{
    [CustomPropertyDrawer(typeof(CroppedMetafile))]
    public class CroppedMetafileEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(property.FindPropertyRelative("ranges"));
            EditorGUILayout.BeginHorizontal();
            var frameCount = property.FindPropertyRelative("Framecount");
            EditorGUILayout.LabelField("Frames:");
            GUI.enabled = false;
            EditorGUILayout.PropertyField(frameCount, new GUIContent());
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            var freq = property.FindPropertyRelative("Freq");
            EditorGUILayout.LabelField("Frequency:");
            GUI.enabled = false;
            EditorGUILayout.PropertyField(freq, new GUIContent());
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

    }
}
