using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CopyPose : MonoBehaviour
{
    [SerializeField]
    Transform source;

    public void FixedUpdate()
    {
        var children = GetComponentsInChildren<Transform>().ToList();
        foreach (Transform sourceChild in source.GetComponentsInChildren<Transform>())
        {
            var child = children.First(ch => ch.name == sourceChild.name);
            child.transform.position = sourceChild.transform.position;
            child.transform.rotation = sourceChild.transform.rotation;
        }
    }
}
