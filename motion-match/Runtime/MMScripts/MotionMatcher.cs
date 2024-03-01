using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

public class MotionMatcher 
{
    float[][] normalizedTemporalMetadata;
    MMFrame[] frameMap;

    List<FeatureStatistics> trajectoryStatistics;

    TimeIndexConverter timeIndexConverter;

    float[] weightArray;

    int nDimensions;
    int nTrajectoryPoints;


    public float MaxVelocity { get; set; }

    public MotionMatcher(MMDataset datasetIn, MatchingWeights weights, float maxVelocity = 1.2f, int margin=5)
    {
        nDimensions = datasetIn.nDimensions;
        nTrajectoryPoints = datasetIn.nTrajectoryPoints;

        List<double> framerates = datasetIn.motionList.Select(x => x.markedUpMetada.freq).ToList();
        List<List<MMDataset.MetacropRange>> cropRangesInClips = 
            datasetIn.motionList.Select(x => x.markedUpMetada.ranges.Select(r => new MMDataset.MetacropRange(r.start, r.stop)).ToList()).ToList();
        timeIndexConverter = new TimeIndexConverter(framerates, cropRangesInClips);

        MappedFeatures mappedFeatures = MocapLoader.GetMappedFeatures(datasetIn, margin: margin);
        frameMap = mappedFeatures.frameMap.SelectMany(x=>x).ToArray();

        List<List<float>> features = mappedFeatures.features;
        trajectoryStatistics = features.Take(nTrajectoryPoints*4).Select(feature => feature.GetNonInfinityStatistics()).ToList();

        for (int i=0; i<nTrajectoryPoints*2; i++)
        {
            trajectoryStatistics[i] /= maxVelocity;
        }

        List<List<float>> normalizedFeatures = MMUtility.NormalizeListsIgnoreInfinity(mappedFeatures.features);

        weightArray = weights.GetArray();
        IEnumerable<IEnumerable<float>> scaledFeatures = normalizedFeatures.Zip(weightArray, (feature, weight) => feature.Select(sample => sample*weight));

        normalizedTemporalMetadata = scaledFeatures.Transpose().Select(row => row.ToArray()).ToArray();

    }

    public MMFrame Match(IEnumerable<Vector2> trajInput, MMFrame curFrame)
    {
        float[] query = BuildQuery(trajInput, curFrame);

        int minIdx = -1;
        float cost = float.MaxValue;
        for (int i = 0; i < normalizedTemporalMetadata.Length; i++)
        {
            
            float cur_cost = L2Norm(query, normalizedTemporalMetadata[i]);
            if (cur_cost < cost)
            {
                minIdx = i;
                cost = cur_cost;
            }
        }

        return frameMap[minIdx];
    }

    public float[] BuildQuery(IEnumerable<Vector2> trajInput, MMFrame sourceFrame)
    {
        float [] inputDrivenNormalizedQuerySegment = trajInput.SelectMany(v => new[] { v.x, v.y }).Zip(trajectoryStatistics, (feature, stat) => stat.Normalize(feature)).ToArray();
        float[] query = new float[nDimensions];
        
        for (int i=0; i<nTrajectoryPoints*4; i++)
        {
            query[i] = inputDrivenNormalizedQuerySegment[i]*weightArray[i];
        }

        float[] source = GetClipFeaturesAtFrame(sourceFrame);
        for (int i = nTrajectoryPoints*4; i < nDimensions; i++)
        {
            query[i] = source[i];
        }

        return query;
    }


    public float[] GetClipFeaturesAtFrame(MMFrame sourceFrame)
    {
        int idx = timeIndexConverter.ConvertFrameToIdx(sourceFrame);
        if (idx >= normalizedTemporalMetadata.Length) Debug.Log(idx);
        return normalizedTemporalMetadata[idx];
    }

   

    public struct MMFrame
    {

        public int ClipIdx { get; }
        public double TimeInClip { get; }


        public MMFrame(int clipIdx, double timeInClip)
        {
            ClipIdx = clipIdx;
            TimeInClip = timeInClip;
        }

        

        public bool IsClose(MMFrame comparedFrame, double tolerance)
        {
            return this.ClipIdx == comparedFrame.ClipIdx && (this.TimeInClip - comparedFrame.TimeInClip) * (this.TimeInClip - comparedFrame.TimeInClip) < tolerance * tolerance;
        }

        public override string ToString()
        {
            return "Clip: " + ClipIdx + ", Time: " + string.Format("{0,0:F1}", TimeInClip) + " s";
        }
    }

    class TimeIndexConverter
    {
        IReadOnlyList<TemporalClipInfo> TemporalClipInfos { get; }

        public TimeIndexConverter(IEnumerable<double> framerates, IEnumerable<IEnumerable<MMDataset.MetacropRange>> allRanges)
        {
            
            List<TemporalClipInfo> _TemporalClipInfos = new List<TemporalClipInfo> { };

            int totalFrames = 0;
            foreach (var tup in framerates.Zip(allRanges,Tuple.Create))
            {
                double framerate = tup.Item1;
                IEnumerable<MMDataset.MetacropRange> ranges = tup.Item2;
                _TemporalClipInfos.Add(new TemporalClipInfo(framerate, ranges.ToList().AsReadOnly(), totalFrames));
                totalFrames = _TemporalClipInfos.Last().FrameCount;
            }
            TemporalClipInfos = _TemporalClipInfos;
        }

        public int ConvertFrameToIdx(MMFrame queryFrame)
        {
            TemporalClipInfo curClipinfo = TemporalClipInfos[queryFrame.ClipIdx];
            return curClipinfo.GetCroppedIdx(queryFrame.TimeInClip);
        }

        public MMFrame ConvertIdxToFrame(int idx)
        {
            int clipIdx = 0;
            for (int i = 0; i < TemporalClipInfos.Count; i++)
            {
                if (TemporalClipInfos[i].ClipOffset > idx) break;

                clipIdx++;
            }

            return new MMFrame(clipIdx, TemporalClipInfos[clipIdx].GetCroppedTime(idx - TemporalClipInfos[clipIdx].ClipOffset));

        }

        public List<string> ClipInfoDescriptions
        {
            get
            {
                return TemporalClipInfos.Select(x => x.ToString()).ToList();
            }
        }

        class TemporalClipInfo
        {
            public double Framerate { get; }
            public IReadOnlyList<MMDataset.MetacropRange> Ranges { get; }

            public int FrameCount { get; }

            public int ClipOffset { get; }

            public int GetCroppedIdx(double time)
            {
                //int idx = (int)(time * Framerate);
                int idx = (int)Math.Round(time * Framerate);
                int result = 0;
                for(int i=0; i<Ranges.Count; i++)
                {

                    if (Ranges[i].stop > idx)
                    {
                        result += idx - Ranges[i].start;
                        break;
                    }
                    result += Ranges[i].length;
                }
                if (result < 0) result = 0;
                if (result >= FrameCount) result = FrameCount-1;
                return ClipOffset + result;
            }

            public double GetCroppedTime(int idxInClip)
            {
                int result = 0;
                for (int i = 0; i < Ranges.Count; i++)
                {

                    if (Ranges[i].stop > idxInClip)
                    {
                        result += idxInClip - Ranges[i].start;
                        break;
                    }
                    result += Ranges[i].length;
                }
                if (result < 0) result = 0;
                if (result > FrameCount) result = FrameCount;
                return result*Framerate;
            }

            public TemporalClipInfo(double framerate, IEnumerable<MMDataset.MetacropRange>ranges, int offset)
            {
                Framerate = framerate;
                Ranges = ranges.ToList();
                ClipOffset = offset;
                FrameCount = Ranges.Select(range => range.length).Sum();
            }

            public override string ToString()
            {
                return "Framerate: " + Framerate + ", Frame Count: " + FrameCount + ", Clip Offset: " + ClipOffset + "\nRanges: " + string.Join(", ", Ranges);
            }
        }
    }

    FeatureStatistics GetVelocityStats(IEnumerable<float> xVelocities, IEnumerable<float> yVelocities)
    {
        IEnumerable<float> horizontalVelocity = xVelocities.Zip(yVelocities, (x, y) => Mathf.Sqrt(x * x + y * y));
        return horizontalVelocity.GetNonInfinityStatistics();
    }

    float L2Norm(float[] x, float[] y)
    {
        float dist = 0f;
        for (int i = 0; i < x.Length; i++)
        {
            dist += (x[i] - y[i]) * (x[i] - y[i]);
        }

        return dist;
    }
}

static class MocapLoader
{
     public static MappedFeatures GetMappedFeatures(MMDataset dataset, int margin=5)
    {
        IEnumerable<MMDataset.CroppedMetafile> metaFiles = dataset.motionList.Select(x => x.markedUpMetada);
        int nDims = dataset.nDimensions;
        int nTrajectoryPoints = dataset.nTrajectoryPoints;

        List<List<float>> list_data = Enumerable.Range(0, nDims).Select(x => new List<float> { }).ToList();
        var frameMap = new List<List<MotionMatcher.MMFrame>> { };
        foreach ((MMDataset.CroppedMetafile metaFile, int fileIdx) in metaFiles.Select((value, i) => (value, i)))
        {
            string json_text = metaFile.file.text;
            ParsedMetadata json_data = JsonConvert.DeserializeObject<ParsedMetadata>(json_text);
            
            List<MMDataset.MetacropRange> curCrops = metaFile.ranges;
            int maxRange = json_data.Length;
            IEnumerable<int> croppedIndices = DistinctIndicesFromRanges(curCrops, maxRange);

            double freq = metaFile.freq;
            frameMap.Add(croppedIndices.Select(x => new MotionMatcher.MMFrame(timeInClip: x/freq, clipIdx: fileIdx)).ToList());

            List<List<float>> incomingFeatures = json_data.ToList();
            IEnumerable<int> marginIndices = MarginIndicesFromRanges(curCrops, maxRange, margin);
            for (int i=0; i<nTrajectoryPoints*4; i++)
            {
                foreach(int marginIdx in marginIndices)
                {
                    incomingFeatures[i][marginIdx] = float.PositiveInfinity;
                }
            }

            IEnumerable<IEnumerable<float>> incomingFeaturesAtIndices = incomingFeatures.Select(f => croppedIndices.Select(i => f[i]));
            list_data.ElementwiseAddRange(incomingFeaturesAtIndices);

        }

        //var normed = Utility.NormalizeList(list_data);
        return new MappedFeatures { features = list_data, frameMap = frameMap};
    }


    static IEnumerable<int> DistinctIndicesFromRanges(IEnumerable<MMDataset.MetacropRange> curCrops, int maxRange)
    {
        IEnumerable<int> idx = Enumerable.Empty<int>();
        foreach (MMDataset.MetacropRange r in curCrops)
        {
            int start = r.start == -1 ? 0 : Math.Max(0, r.start);
            int stop = r.stop == -1 ? maxRange : Math.Min(r.stop, maxRange);
            idx = idx.Concat(Enumerable.Range(start, stop - start));
        }
        return idx.Distinct();
    }
   
    
    static IEnumerable<int> MarginIndicesFromRanges(IEnumerable<MMDataset.MetacropRange> curCrops, int maxRange, int marginSize)
    {
        IEnumerable<MMDataset.MetacropRange> marginRegions = 
            curCrops.Select(cr => new MMDataset.MetacropRange(startIn: cr.stop - marginSize, stopIn: cr.stop));

        return DistinctIndicesFromRanges(marginRegions, maxRange);
    }

    public class ParsedMetadata
    {
        public List<float[]> t_h;
        public List<float[]> d_h;
        public List<float[]> v_g_l;
        public List<float[]> p_l_lfoot;
        public List<float[]> p_l_rfoot;
        public List<float[]> v_g_lfoot;
        public List<float[]> v_g_rfoot;
        public List<float[]> l_stances;
        public List<float[]> r_stances;

        public int Length
        {
            get => t_h[0].Length;
        }

        public List<List<float>> ToList()
        {
            return new List<List<float[]>> { t_h, d_h, v_g_l, p_l_lfoot, p_l_rfoot, v_g_lfoot, v_g_rfoot }.SelectMany(x => x).Select(x => x.ToList()).ToList();
        }
    }
}

class MappedFeatures
{
    public List<List<float>> features;
    public List<List<MotionMatcher.MMFrame>> frameMap;
}

struct FeatureStatistics
{
    public float Mean { get; }
    public float Std { get; }

    public FeatureStatistics(float mean, float std)
    {
        Mean = mean;
        Std = std;
    }

    public float Normalize(float inp)
    {
        return (inp - Mean) / Std;
    }

    public static FeatureStatistics operator /(FeatureStatistics stats, float scale) => new FeatureStatistics(stats.Mean / scale, stats.Std / scale);
    public static FeatureStatistics operator *(FeatureStatistics stats, float scale) => new FeatureStatistics(stats.Mean * scale, stats.Std * scale);

    public override string ToString()
    {
        return "Mean: " + Mean + ", Std: " + Std;
    }
}

[Serializable]
public struct MatchingWeights
{
    public float trajectory;
    public float direction;
    public float hipVelocity;
    public float leftFootPosition;
    public float rightFootPosition;
    public float leftFootVelocity;
    public float rightFootVelocity;
    public int nTrajPoints;

    public MatchingWeights(float trajectory, float direction, float hipVelocity, float leftFootPosition, float rightFootPosition, float leftFootVelocity, float rightFootVelocity, int nTrajPoints=3)
    {
        this.trajectory = trajectory;
        this.direction = direction;
        this.hipVelocity = hipVelocity;
        this.leftFootPosition = leftFootPosition;
        this.rightFootPosition = rightFootPosition;
        this.leftFootVelocity = leftFootVelocity;
        this.rightFootVelocity = rightFootVelocity;
        this.nTrajPoints = nTrajPoints;
    }

    public float[] GetArray()
    {
        return Enumerable.Repeat(trajectory, nTrajPoints * 2)
            .Concat(Enumerable.Repeat(direction, nTrajPoints * 2))
            .Concat(Enumerable.Repeat(hipVelocity, 3))
            .Concat(Enumerable.Repeat(leftFootPosition, 3))
            .Concat(Enumerable.Repeat(rightFootPosition, 3))
            .Concat(Enumerable.Repeat(leftFootVelocity, 3))
            .Concat(Enumerable.Repeat(rightFootPosition, 3))
            .ToArray();
    }

    public static MatchingWeights Default
    {
        get
        {
            return new MatchingWeights(3, 2, 6, 3, 3, 6, 6);
        }
    }

    public override string ToString()
    {
        return string.Join(", ", this.GetArray());
    }
}