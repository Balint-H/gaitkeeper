using Mujoco;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SfRampGenerator : MonoBehaviour
{

    [SerializeField]
    RampSettings settings = new RampSettings { length=2.3885f, height=0.64f, width=0.57f, landingLength=0.57f};

    RampSettings oldSettings;

    [SerializeField]
    Material material;




    [Serializable]
    private struct RampSettings
    {
        [SerializeField]
        public float length;

        [SerializeField]
        public float height;

        [SerializeField]
        public float width;

        [SerializeField]
        public float landingLength;
    }

    private void OnValidate()
    {
# if UNITY_EDITOR
        if (Application.isPlaying) return;
        if (!settings.Equals(oldSettings))
        {
            UnityEditor.EditorApplication.delayCall += DeleteOldGeoms;

            UnityEditor.EditorApplication.delayCall += GenerateSurface;
        }

        oldSettings = settings;
#endif
    }

    private void DeleteOldGeoms()
    {
        foreach (var oldGeom in GetComponentsInChildren<MjGeom>())
        {
            DestroyImmediate(oldGeom.gameObject);
        }
        foreach (var oldGeom in GetComponentsInChildren<Transform>().Skip(1).Where(t => t.childCount < 1))
        {
            DestroyImmediate(oldGeom.gameObject);
        }
    }

    private void GenerateSurface()
    {
        GenerateRamp();
        if (settings.landingLength>0) GenerateLanding();
    }

    private void GenerateRamp()
    {
        var geom = NewGeom($"Ramp");
        var angle = Mathf.Atan2(settings.height, settings.length);
        var slopeLength = Mathf.Sqrt(settings.height * settings.height + settings.length * settings.length);
        var side2length = settings.height / Mathf.Cos(angle);
        geom.ShapeType = MjShapeComponent.ShapeTypes.Box;
        geom.Box.Extents = new Vector3(settings.width / 2, side2length/2, slopeLength/2);
        geom.transform.localPosition = new Vector3(0f, 0f, Mathf.Sqrt(side2length * side2length + slopeLength*slopeLength)/2);
        geom.transform.localRotation = Quaternion.Euler(-angle*Mathf.Rad2Deg, 0f, 0f);
        geom.gameObject.AddComponent<MjMeshFilter>();
        geom.gameObject.AddComponent<MeshRenderer>().material = material;
    }

    private void GenerateLanding()
    {
        var geom = NewGeom($"Landing");
        geom.ShapeType = MjShapeComponent.ShapeTypes.Box;
        geom.Box.Extents = new Vector3(settings.width / 2, settings.height, settings.landingLength / 2);
        geom.transform.localPosition = new Vector3(0f, 0f, settings.length + settings.landingLength / 2);
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
