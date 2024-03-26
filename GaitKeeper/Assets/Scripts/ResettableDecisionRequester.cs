using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class ResettableDecisionRequester : DecisionRequester
{
    int stepOffset;
    bool shouldSynchronise;

    protected override bool ShouldRequestDecision(DecisionRequestContext context)
    {
        if (shouldSynchronise)
        {
            stepOffset = DecisionPeriod - (Academy.Instance.StepCount % DecisionPeriod);
            shouldSynchronise = false;
        }

        return (context.AcademyStepCount + stepOffset) % DecisionPeriod == 0;
    }

    public void SynchroniseDecisionNextStep()
    {
        shouldSynchronise = true;
    }
}
