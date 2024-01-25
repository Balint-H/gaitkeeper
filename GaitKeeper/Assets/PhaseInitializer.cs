using GaitLab;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PhaseInitializer : TrainingEventHandler
{
    [SerializeField]
    Animator animator;

    [SerializeField]
    List<PhaseTimestamp> timestamps;

    [SerializeField]
    ActuatorOSL leftActuator;

    [SerializeField]
    ActuatorOSL rightActuator;

    [SerializeField]
    OslObservationSource observationSource;

    private List<ActuatorOSL.FsmSwitchArgs> startParams;

    public override EventHandler Handler => SetActuatorPhases;



    [Serializable]
    private struct PhaseTimestamp
    {
        public ActuatorOSL.GaitPhase LeftPhase;

        public ActuatorOSL.GaitPhase RightPhase;

        public float normalizedTime;
    }

    public void SetActuatorPhases(object sender, EventArgs e)
    {
        var curTs = timestamps.Last(ts => ts.normalizedTime <= animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        leftActuator.phase = curTs.LeftPhase;
        rightActuator.phase = curTs.RightPhase;

        observationSource.intactTrajectoryParams = startParams;
    }

    private void Awake()
    {
        startParams = observationSource.intactTrajectoryParams.ToList();
    }

}
