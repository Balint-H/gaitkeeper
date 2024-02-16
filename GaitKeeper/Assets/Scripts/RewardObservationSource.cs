using System;
using System.Collections;
using System.Collections.Generic;
using ModularAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

public class RewardObservationSource : ObservationSource {
  public RewardSignal rewardSignal;
  private float lastReward;

  public override void FeedObservationsToSensor(VectorSensor sensor) {
    sensor.AddObservation(lastReward);
  }

  public override void OnAgentStart() {

  }

  private void Awake() {
    rewardSignal.OnCalculateReward += f => lastReward = f;
  }

  public override int Size => 1;
}
