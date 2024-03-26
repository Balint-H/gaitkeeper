using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace MotionMatch
{
    public class MMRigBuilder : RigBuilder
    {
        [SerializeField]
        MMController controller;

        void OnEnable()
        {
            
        }

        void Start()
        {
            controller.GetComponentInChildren<Animator>().enabled = false;
            // Build runtime data.
            if (Application.isPlaying)
                Build(controller.Animator.GetGraph());

            onAddRigBuilder?.Invoke(this);

            controller.GetComponentInChildren<Animator>().enabled = true;
            controller.GetComponentInChildren<Animator>().Rebind();
        }
    }
}
