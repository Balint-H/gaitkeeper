using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ModularAgents.MotorControl;
using Mujoco;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Mujoco.Extensions;

public class MjDofObservationSource : ObservationSource {

  [SerializeField]
  private List<MjBaseJoint> joints;

  private List<IMjJointState> states;

  [SerializeField]
  private bool useVel;

  [SerializeField]
  private bool usePos;

  public override void FeedObservationsToSensor(VectorSensor sensor) {
    foreach (var q in states.SelectMany(state => state.Positions)) {
      sensor.AddObservation((float)q);
    }
    if(!useVel) return;
    foreach (var q in states.SelectMany(state => state.Velocities)) {
      sensor.AddObservation((float)q);
    }
  }

  private void Awake() {
    states = joints.Select(IMjJointState.GetJointState).ToList();
  }

  public override void OnAgentStart() {
  }

  public override int Size => (usePos ? joints.Select(MjState.PosCount).Sum() : 0) +
                              (useVel ? joints.Select(MjState.DofCount).Sum() : 0);
}
