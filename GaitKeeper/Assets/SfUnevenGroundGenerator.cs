using Mujoco;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SfUnevenGroundGenerator : MonoBehaviour
{
    [SerializeField]
    float length;

    float oldLength;

    [SerializeField]
    float width;

    float oldWidth;

    [SerializeField]
    Material highMaterial;

    [SerializeField]
    Material midMaterial;

    [SerializeField]
    Material lowMaterial;
    
    



    const float crossBarLength = 0.05f;
    const float platformLength = 0.18f;
    const float platformWidth = 0.15f;
    const float platformGap = 0.075f;
    const float platformHeight = 0.012f;

    float segmentLength = crossBarLength + platformLength;

    private void OnValidate()
    {
# if UNITY_EDITOR
        if (Application.isPlaying) return;
        if(length != oldLength || width != oldWidth)
        {
            UnityEditor.EditorApplication.delayCall += DeleteOldGeoms;

            UnityEditor.EditorApplication.delayCall += GenerateSurface;
        }

        oldLength = length;
        oldWidth = width;
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

    private void GenerateSurface()
    {
        float curLength = 0f;
        for (int n=0; n<200; n++)
        {
            curLength += GenerateCrossBar(n);
            if (curLength > length) break;
            curLength += GeneratePlatformSegment(n);
            if(curLength > length) break;
        }
        GenerateLowPlatform();
        foreach(var geom in  GetComponentsInChildren<MjGeom>())
        {
            geom.Settings.Filtering.Group = 4;
            geom.Settings.Filtering.Conaffinity = 0;
        }    
    }

    private MjGeom NewGeom(string name)
    {
        var geom = new GameObject().AddComponent<MjGeom>();
        geom.ShapeType = MjShapeComponent.ShapeTypes.Box;
        geom.name = name;
        geom.transform.parent = transform;
        return geom;
    }

    private float GenerateCrossBar(int n)
    {
        var geom = NewGeom($"Crossbar {n}");
        geom.Box.Extents = new Vector3(width/2, platformHeight*3, crossBarLength/2);
        geom.transform.localPosition = new Vector3(0f, -platformHeight, n * segmentLength+crossBarLength/2);

        geom.gameObject.AddComponent<MjMeshFilter>();
        geom.gameObject.AddComponent<MeshRenderer>().material = midMaterial;

        return crossBarLength;
    }

    private float GeneratePlatformSegment(int n)
    {
        var lengthStartPos = n * segmentLength + crossBarLength;
        var curWidth = -(n%3) * platformWidth;
        var segmentRoot = new GameObject($"Segment {n}");
        segmentRoot.transform.parent = transform;
        segmentRoot.transform.localPosition = Vector3.zero;
        segmentRoot.transform.localRotation = Quaternion.identity;
        List<MjGeom> geoms = new List<MjGeom>();
        for(int m=0; m<200;  m++) {

            if(curWidth >= 0) geoms.Add(SubGenerateHighPlatform(m, lengthStartPos));
            curWidth += platformWidth;
            if (curWidth >= width) break;
            if (curWidth >= 0) geoms.Add(SubGeneratePlatformGap(m, lengthStartPos));
            curWidth += platformGap + platformWidth;
            if (curWidth >= width) break;
            if (curWidth >= 0) geoms.Add(SubGeneratePlatformGap(m, lengthStartPos, postLow: true));
            curWidth += platformGap;
            if (curWidth >= width) break;
        }

        foreach (var geom in geoms)
        {
            geom.transform.parent = segmentRoot.transform;
            geom.gameObject.AddComponent<MjMeshFilter>();
            if(geom.name.ToUpper().Contains("HIGH")) geom.gameObject.AddComponent<MeshRenderer>().material = highMaterial;
            else if(geom.name.ToUpper().Contains("LOW")) geom.gameObject.AddComponent<MeshRenderer>().material = lowMaterial;
            else geom.gameObject.AddComponent<MeshRenderer>().material = midMaterial;


            geom.transform.localPosition = geom.transform.localPosition + new Vector3(-(n % 3) * platformWidth - width/2, 0, 0);
        }

        return platformLength;
    }

    private MjGeom SubGenerateHighPlatform(int n, float lengthStartPos)
    {
        var geom = NewGeom($"High Platform {n}");
        geom.Box.Extents = new Vector3(platformWidth/2, platformHeight * 4, platformLength / 2);
        geom.transform.localPosition = new Vector3(n * (2 * platformWidth + 2*platformGap) +platformWidth/2, -platformHeight, lengthStartPos + platformLength / 2);
        return geom;
    }

    private MjGeom SubGeneratePlatformGap(int n, float lengthStartPos, bool postLow = false)
    {
        var geom = NewGeom($"Platform Gap {n}");
        geom.Box.Extents = new Vector3(platformGap/2, platformHeight * 3, platformLength / 2);
        geom.transform.localPosition = new Vector3(n * (2 * platformWidth + 2 * platformGap) + ( postLow? (2*platformWidth + 3*platformGap / 2) : (platformWidth + platformGap / 2)), 
                                                   -platformHeight, lengthStartPos + platformLength / 2);
        return geom;
    }

    private MjGeom SubGenerateLowPlatform(int n, float lengthStartPos)
    {
        var geom = NewGeom($"Low Platform {n}");
        geom.Box.Extents = new Vector3(platformWidth / 2, platformHeight * 2, platformLength / 2);
        geom.transform.localPosition = new Vector3(n * (2 * platformWidth + 2 * platformGap) + 3*platformWidth / 2 +platformGap, -platformHeight, lengthStartPos + platformLength / 2);
        return geom;
    }

    private MjGeom GenerateLowPlatform()
    {
        var geom = NewGeom($"Low Platform");
        var extendedLength = length + segmentLength - crossBarLength;
        geom.Box.Extents = new Vector3(width/2, platformHeight * 2, (extendedLength-extendedLength%segmentLength)/2);
        geom.transform.localPosition = new Vector3(0, -platformHeight, (extendedLength-extendedLength%segmentLength)/2);
        geom.gameObject.AddComponent<MjMeshFilter>();
        geom.gameObject.AddComponent<MeshRenderer>().material = lowMaterial;
        return geom;

    }

}
