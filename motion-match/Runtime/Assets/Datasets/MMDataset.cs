using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;

namespace MotionMatch
{

    [CreateAssetMenu(fileName = "MMDataset", menuName = "MMDataset", order = 2)]
    public class MMDataset : ScriptableObject
    {
        public List<Motion> motionList;
        public int nDimensions;
        public int nTrajectoryPoints;

        //  metadata.t_h = Hdf5Reader.GetArray(path, "metadata", "t_h").ToList();

        [Serializable]
        public class Motion
        {
            public string Name;
            public AnimationClip clip;
            public CroppedMetafile markedUpMetada;

        }

        [Serializable]
        public class CroppedMetafile
        {
            public ParsedMetadata metadata;

            public double Freq = 0;

            public int Framecount = 0;
            public List<MetacropRange> ranges;
        }

        [Serializable]
        public struct MetacropRange
        {
            public int start;
            public int stop;
            public readonly int length;

            public MetacropRange(int startIn, int stopIn)
            {
                start = startIn;
                stop = stopIn;
                length = stop - start;
            }

            public override string ToString()
            {
                return "[" + start + ", " + stop + "]";
            }
        }

        [Serializable]
        public class ParsedMetadata
        {
            public List<float[]> t_h;
            public List<float[]> d_h;
            public List<float[]> v_g_l;
            public List<float[]> p_l_lfoot;
            public List<float[]> p_l_rfoot;
            public List<float[]> v_g_lfoot;
            public List<float[]> v_g_rfoot;

            [JsonProperty(PropertyName = "Framerate")]
            public float framerate;

            [JsonProperty(PropertyName = "Time Samples")]
            public List<float> TimeSamples;

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
}
