using GaitLab;
using ModularAgents.MotorControl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ModularAgents;
using Unity.MLAgents;
using UnityEngine;


public class FsmProsthesisDecisionRequester : DecisionRequester
{
    [SerializeField]
    ModularAgent agent;

    [SerializeField]
    ActuatorOSL fsmSource;

    [SerializeField]
    ImpedanceControllerComponent controller;

    [SerializeField]
    bool differential;

    [SerializeField, Range(0,1)]
    float smoothing;

    [SerializeField]
    List<ImpedanceParams> previousParams;

    [Serializable]
    private struct ImpedanceParams
    {
        public string Name;

        [SerializeField]
        public List<double> PosGains;

        [SerializeField]
        public List<double> VelGains;

        [SerializeField]
        public List<double> TargetAngles;

        public ImpedanceParams MixWith(ImpedanceParams param, float t)
        {
            return new ImpedanceParams
            {
                PosGains = PosGains.Zip(param.PosGains, (c, d) => d*t + c*(1-t)).ToList(),
                VelGains = VelGains.Zip(param.VelGains, (c, d) => d*t + c*(1-t)).ToList(),
                TargetAngles = TargetAngles.Zip(param.TargetAngles, (c, d) => d* t + c*(1-t)).ToList()
            };
        }

    }

    private void Awake()
    {
        fsmSource.FsmSwitchEvent += RequestDecision;
        agent.OnBegin += (_, _) => RequestDecision(this, fsmSource.SwitchArgs);
    }

    public void RequestDecision(object sender, ActuatorOSL.FsmSwitchArgs e)
    {
        if(differential)
        {
            controller.activePosGains = previousParams[(int)e.NewPhase].PosGains;
            controller.activeVelGains = previousParams[(int)e.NewPhase].VelGains;
            controller.activeAngleOffsets = previousParams[(int)e.NewPhase].TargetAngles;
        }

        agent.RequestDecision();

        if(differential)
        {
            var paramsToSave = new ImpedanceParams();
            paramsToSave.PosGains = controller.GetPosGainVector().ToList();
            paramsToSave.VelGains = controller.GetVelGainVector().ToList();
            paramsToSave.TargetAngles = controller.GetAngleOffsetVector().ToList();
            previousParams[(int)e.NewPhase] = paramsToSave.MixWith(previousParams[(int)e.NewPhase], smoothing);
        }
    }
}
