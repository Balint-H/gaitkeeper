using ModularAgents.MotorControl.Mujoco;
using Mujoco.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedPDController : SPDActuatorComponent
{

    [SerializeField]
    float[] actionsInit;

    [SerializeField]
    bool updateActions;

    void FixedUpdate()
    {
        if(updateActions) ApplyActions(actionsInit);
    }

    void Start()
    {
        StartCoroutine(DelayedStart());
        
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        MjState.ExecuteAfterMjStart(MjInitialize);
        ApplyActions(actionsInit);
    }

}
