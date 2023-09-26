using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public static class MMUtility
{
    public static IEnumerable<float> LinSpace(float start, float end, int numElem)
    {
        for (int i = 0; i < numElem; i ++)
        {
            yield return (float)i/(numElem-1)*(end-start)+start;
        }
    }

    public static IEnumerable<Vector2> LinSpace(Vector2 start, Vector2 end, int numElem)
    {
        for (int i = 0; i < numElem; i++)
        {
            yield return (float)i / (numElem - 1) * (end - start) + start;
        }
    }

    public static IEnumerable<float> CumulativeSum(this IEnumerable<float> sequence)
    {
        float sum = 0f;
        foreach (var item in sequence)
        {
            sum += item;
            yield return sum;
        }
    }

    public static IEnumerable<Vector2> CumulativeSum(this IEnumerable<Vector2> sequence)
    {
        Vector2 sum = new Vector2{ x=0, y=0 } ;
        foreach (var item in sequence)
        {
            sum += item;
            yield return sum;
        }
    }

    public static float StdDev(this IEnumerable<float> values)
    {
        float ret = 0;
        int count = values.Count();
        if (count > 1)
        {
            //Compute the Average
            float avg = values.Average();

            //Perform the Sum of (value-avg)^2
            float sum = values.Sum(d => (d - avg) * (d - avg));

            //Put it all together
            ret = (float)Math.Sqrt(sum / count);
        }
        return ret;
    }

    public static IEnumerable<IEnumerable<T>> Transpose<T>(this IEnumerable<IEnumerable<T>> list)
    {
        return
            //generate the list of top-level indices of transposed list
            Enumerable.Range(0, list.First().Count())
            //selects elements at list[y][x] for each x, for each y
            .Select(x => list.Select(y => y.ElementAt(x)));
    }

    public static void ElementwiseAddRange<T>(this List<List<T>> features, IEnumerable<IEnumerable<T>> incoming)
    {
        foreach (var tup in features.Zip(incoming, Tuple.Create))
        {
            var featureIn = tup.Item2;
            tup.Item1.AddRange(featureIn);
        }
    }

    internal static List<List<float>> NormalizeListsIgnoreInfinity(List<List<float>> listOfLists)
    {
        IEnumerable<FeatureStatistics> statistics = listOfLists.Select(feature => feature.GetNonInfinityStatistics());

        return listOfLists.Zip(statistics, (feature, stat) => feature.Select(sample => stat.Normalize(sample)).ToList() ).ToList();
    }

    internal static FeatureStatistics GetNonInfinityStatistics(this IEnumerable<float> feature)
    {
        float mean = feature.Where(sample => !float.IsInfinity(sample)).Average();
        float std = feature.Where(sample => !float.IsInfinity(sample)).StdDev();
        return new FeatureStatistics(mean: mean, std: std);
    }

    internal static IEnumerable<Transform> FlattenTransformHierarchy(Transform root)
    {
        yield return root;

        foreach(Transform childTransform in root)
        {
            IEnumerable<Transform> subList = FlattenTransformHierarchy(childTransform);
            foreach(Transform subListElement in subList)
            {
                yield return subListElement;
            }
        }

        yield break;
    }

    public static Vector2 Invert(this Vector2 vector)
    {
        return new Vector2(1/vector.x, 1/vector.y);
    }

    public static Vector3 ProjectTo3D(this Vector2 vector)
    {
        return new Vector3(vector.x, 0f, vector.y);
    }

    public static Vector2 Horizontal(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }

    public static Vector3 Horizontal3D(this Vector3 vector3)
    {
        return new Vector3(vector3.x, 0f, vector3.z);
    }
}
