using GaitLab;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class OslObservationSource : ObservationSource {
  [SerializeField]
  bool oneHotEncode;

  [SerializeField]
  ActuatorOSL prosthesisFsm;

  [SerializeField]
  ActuatorOSL intactFsm;


  List<ActuatorOSL.FsmSwitchArgs> prosthesisTrajectoryParams;

  [SerializeField]
  public List<ActuatorOSL.FsmSwitchArgs> intactTrajectoryParams;


  public override int Size => (oneHotEncode ? 4 : 1) + 4 * 8;

  public override void FeedObservationsToSensor(VectorSensor sensor) {
    if (oneHotEncode) {
      var code = new bool[4];
      code[(int)prosthesisFsm.ActiveBehaviour.phase] = true;
      foreach (var b in code) {
        sensor.AddObservation(b);
      }
    } else {
      sensor.AddObservation(((float)prosthesisFsm.ActiveBehaviour.phase - 2) / 2f);
    }

    foreach ((var p, var i) in
             prosthesisTrajectoryParams.Zip(intactTrajectoryParams, Tuple.Create)) {
      sensor.AddObservation((float)p.KneeAngle);
      sensor.AddObservation((float)p.AnkleAngle);
      sensor.AddObservation((float)p.SwitchTime);
      sensor.AddObservation((float)p.AxialForce);

      sensor.AddObservation((float)i.KneeAngle - (float)p.KneeAngle);
      sensor.AddObservation((float)i.AnkleAngle - (float)p.AnkleAngle);
      sensor.AddObservation((float)i.SwitchTime - (float)p.SwitchTime);
      sensor.AddObservation((float)i.AxialForce - (float)p.AxialForce);
    }
  }

  public void Awake() {
    OnAgentStart();
  }

  public override void OnAgentStart() {
    if (prosthesisTrajectoryParams != null) return;
    prosthesisTrajectoryParams = new List<ActuatorOSL.FsmSwitchArgs> { };
    for (int i = 0; i < 4; i++) {
      prosthesisTrajectoryParams.Add(intactTrajectoryParams[i]);
    }

    prosthesisFsm.FsmSwitchEvent += UpdateParams;
    intactFsm.FsmSwitchEvent += (_, args) => intactTrajectoryParams[(int)args.NewPhase] = args;
  }

  private void UpdateParams(object sender, ActuatorOSL.FsmSwitchArgs args) {
    prosthesisTrajectoryParams[(int)args.NewPhase] = args;
  }

}