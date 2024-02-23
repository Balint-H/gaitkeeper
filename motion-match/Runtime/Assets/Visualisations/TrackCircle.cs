using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MotionMatch.MMUtility;
using System.Linq;

namespace MotionMatch
{
    public class TrackCircle : MonoBehaviour
    {

        LineRenderer circleLine;
        [SerializeField] public float radius;
        [SerializeField] public int numVertices;
        // Start is called before the first frame update
        void Start()
        {
            UpdateCircle();
        }

        public void UpdateCircle()
        {
            circleLine = GetComponent<LineRenderer>();
            circleLine.positionCount = numVertices;
            var angles = LinSpace(0, 2f * Mathf.PI, numVertices);
            circleLine.SetPositions(angles.Select(a => new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius).ToArray());
        }

    }
}