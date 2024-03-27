using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatch
{
    public class HintEffector : MonoBehaviour
    {
        [Range(0.0f, 1.0f)]
        public float weight;

        private void Update()
        {
            var material = GetComponent<Renderer>().material;
            Color color = Color.magenta;
            material.color = IKUtility.FadeEffectorColorByWeight(color, weight);
        }
    }

}