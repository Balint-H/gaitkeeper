using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatch;


public class MMControllerHandler : TrainingEventHandler
{
    [SerializeField]
    MMController controller;

    public override EventHandler Handler => controller.Handler;
}
