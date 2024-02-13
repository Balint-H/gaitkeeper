using GaitLab;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PhaseInitializer : TrainingEventHandler {
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
  private struct PhaseTimestamp {
    public ActuatorOSL.GaitPhase LeftPhase;

    public ActuatorOSL.GaitPhase RightPhase;

    public float normalizedTime;
  }

  public void SetActuatorPhases(object sender, EventArgs e) {
    var curNormalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1;
    animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, curNormalizedTime);
    var curTs = timestamps.Last(ts =>
        ts.normalizedTime <= curNormalizedTime);
    leftActuator.phase = curTs.LeftPhase;
    rightActuator.phase = curTs.RightPhase;

    observationSource.intactTrajectoryParams = startParams;
  }

  private void Awake() {
    startParams = observationSource.intactTrajectoryParams.ToList();
  }

}