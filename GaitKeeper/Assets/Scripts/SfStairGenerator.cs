using Mujoco;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SfStairGenerator : MonoBehaviour
{

    [SerializeField]
    StairSettings settings =  new StairSettings {stepLength=0.3f, stepHeight=0.16f, stepWidth=0.57f, stepCount=4};

    StairSettings oldSettings;

    [SerializeField]
    Material material;


    [Serializable]
    private struct StairSettings
    {
        [SerializeField]
        public float stepLength;

        [SerializeField]
        public float stepHeight;

        [SerializeField]
        public float stepWidth;

        [SerializeField]
        public int stepCount;
    }

    private void OnValidate()
    {
# if UNITY_EDITOR
        if (Application.isPlaying) return;
        if(!settings.Equals(oldSettings))
        {
            UnityEditor.EditorApplication.delayCall += DeleteOldGeoms;

            UnityEditor.EditorApplication.delayCall += GenerateStair;
        }

        oldSettings = settings;
#endif
    }

    private void DeleteOldGeoms()
    {
        foreach(var oldGeom in GetComponentsInChildren<MjGeom>())
        {
            DestroyImmediate(oldGeom.gameObject);
        }
        foreach (var oldGeom in GetComponentsInChildren<Transform>().Skip(1).Where(t => t.childCount < 1))
        {
            DestroyImmediate(oldGeom.gameObject);
        }
    }

    private void GenerateStair()
    {
        for (int n = 0; n < settings.stepCount; n++)
        {
            GenerateStep(n);
        }
    }

    private void GenerateStep(int stepNum)
    {
        var geom = NewGeom($"Step {stepNum}");
        geom.ShapeType = MjShapeComponent.ShapeTypes.Box;
        geom.Box.Extents = new Vector3(settings.stepWidth/2, settings.stepHeight * (stepNum + 1), settings.stepLength/2);
        geom.transform.localPosition = new Vector3(0f, 0f, (settings.stepLength * stepNum) + settings.stepLength / 2);
        geom.gameObject.AddComponent<MjMeshFilter>();
        geom.gameObject.AddComponent<MeshRenderer>().material = material;
    }

    private MjGeom NewGeom(string name)
    {
        var geom = new GameObject().AddComponent<MjGeom>();
        geom.ShapeType = MjShapeComponent.ShapeTypes.Box;
        geom.name = name;
        geom.transform.parent = transform;
        return geom;
    }

 

   

}
