using ModularAgents.MotorControl;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MjQfrcTrackReward : RewardSource
{
    [SerializeField]
    ImpedanceControllerComponent deviceController;

    [SerializeField]
    float factor;

    public override void OnAgentStart()
    {
    }

    public override float Reward
    {
        get
        {
            Debug.Log(Mathf.Exp(factor * deviceController.QfrcErrors.Select(e => e * e).Sum()));
            return Mathf.Exp(factor * deviceController.QfrcErrors.Select(e => e * e).Sum());
        }
    }
}
