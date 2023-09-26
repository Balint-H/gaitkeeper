using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathVisualiser : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    Transform rootToFollow;

    private void LateUpdate()
    {
        transform.position = new Vector3(rootToFollow.position.x, transform.position.y, rootToFollow.position.z);
    }
}
