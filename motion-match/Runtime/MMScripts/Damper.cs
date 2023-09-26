using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Damper
{
    // Critically dampened change in position and velocity
    // eigenv is the eigenvalue (negative) of the characteristic equation, c1 and c2 are the constants for the particular solution
    public static float DampedVariable(float startValue, float targetVal, float startDerivative, float sampleTime, float eigenv)
    {
        float c1 = startValue - targetVal;
        float c2 = startDerivative - c1 * eigenv;
        return targetVal + (c1 + c2 * sampleTime) * Mathf.Exp(eigenv * sampleTime);
    }

    public static IEnumerable<float> DampedVariable(float startValue, float targetVal, float startDerivative, IEnumerable<float> sampleTimes, float eigenv)
    {
        float c1 = startValue - targetVal;
        float c2 = startDerivative - c1 * eigenv;
        return sampleTimes.Select( t => targetVal + (c1 + c2 * t) * Mathf.Exp((eigenv * t)));
    }

    public static float DampedDerivative(float startValue, float targetVal, float startDerivative, float sampleTime, float eigenv)
    {
        float c1 = startValue - targetVal;
        float c2 = startDerivative - c1 * eigenv;
        return (startDerivative + c2*eigenv*sampleTime) * Mathf.Exp((eigenv * sampleTime));
    }

    public static IEnumerable<float> DampedDerivative(float startValue, float targetVal, float startDerivative, IEnumerable<float> sampleTimes, float eigenv)
    {
        float c1 = startValue - targetVal;
        float c2 = startDerivative - c1 * eigenv;
        return sampleTimes.Select(t => (startDerivative + c2 * eigenv * t) * Mathf.Exp((eigenv * t)));
    }


    public static Vector2 DampedVector(Vector2 startValue, Vector2 targetVal, Vector2 startDerivative, float sampleTime, float eigenv)
    {
        Vector2 c1 = startValue - targetVal;
        Vector2 c2 = startDerivative - c1 * eigenv;
        return targetVal + (c1 + c2 * sampleTime) * Mathf.Exp(eigenv * sampleTime);
    }

    public static IEnumerable<Vector2> DampedVector(Vector2 startValue, Vector2 targetVal, Vector2 startDerivative, IEnumerable<float> sampleTimes, float eigenv)
    {
        Vector2 c1 = startValue - targetVal;
        Vector2 c2 = startDerivative - c1 * eigenv;
        return sampleTimes.Select(t => targetVal + (c1 + c2 * t) * Mathf.Exp((eigenv * t)));
    }


    public static Vector2 DampedVectorDerivative(Vector2 startValue, Vector2 targetVal, Vector2 startDerivative, float sampleTime, float eigenv)
    {
        Vector2 c1 = startValue - targetVal;
        Vector2 c2 = startDerivative - c1 * eigenv;
        return (startDerivative + c2 * eigenv * sampleTime) * Mathf.Exp((eigenv * sampleTime));
    }

    public static IEnumerable<Vector2> DampedVectorDerivative(Vector2 startValue, Vector2 targetVal, Vector2 startDerivative, IEnumerable<float> sampleTimes, float eigenv)
    {
        Vector2 c1 = startValue - targetVal;
        Vector2 c2 = startDerivative - c1 * eigenv;
        return sampleTimes.Select(t => (startDerivative + c2 * eigenv * t) * Mathf.Exp((eigenv * t)));
    }


}
