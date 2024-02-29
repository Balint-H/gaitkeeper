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
            public CroppedMetafile markedUpMetadata;

        }

        [Serializable]
        public class CroppedMetafile
        {
            public SerializedMetadata metadata;

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
        public class SerializedMetadata
        {
            public List<ArrayWrapper> t_h;
            public List<ArrayWrapper> d_h;
            public List<ArrayWrapper> v_g_l;
            public List<ArrayWrapper> p_l_lfoot;
            public List<ArrayWrapper> p_l_rfoot;
            public List<ArrayWrapper> v_g_lfoot;
            public List<ArrayWrapper> v_g_rfoot;

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
                return new List<List<ArrayWrapper>> { t_h, d_h, v_g_l, p_l_lfoot, p_l_rfoot, v_g_lfoot, v_g_rfoot }
                    .SelectMany(x => x.Select(aw => aw.arr))
                    .Select(x => x.ToList()).ToList();
            }

            [Serializable]
            public class ArrayWrapper
            {
                public float[] arr;
                public float this[int key]
                {
                    get
                    {
                        return arr[key];
                    }
                    set
                    {
                        arr[key] = value;
                    }
                }

                public static ArrayWrapper Wrap(float[] arr)
                {
                    return new ArrayWrapper { arr = arr };
                }

                public int Length => arr.Length;
            }

            public class JsonParsedMetadata
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

                public SerializedMetadata ToSerializedMetadata()
                {
                    SerializedMetadata metadata = new SerializedMetadata();
                    metadata.t_h = t_h.Select(arr => ArrayWrapper.Wrap(arr)).ToList();
                    metadata.d_h = d_h.Select(arr => ArrayWrapper.Wrap(arr)).ToList();
                    metadata.v_g_l = v_g_l.Select(arr => ArrayWrapper.Wrap(arr)).ToList();
                    metadata.p_l_lfoot = p_l_lfoot.Select(arr => ArrayWrapper.Wrap(arr)).ToList();
                    metadata.p_l_rfoot = p_l_rfoot.Select(arr => ArrayWrapper.Wrap(arr)).ToList();
                    metadata.v_g_lfoot = v_g_lfoot.Select(arr => ArrayWrapper.Wrap(arr)).ToList();
                    metadata.v_g_rfoot = v_g_rfoot.Select(arr => ArrayWrapper.Wrap(arr)).ToList();
                    metadata.framerate = framerate;
                    metadata.TimeSamples = TimeSamples;
                    return metadata;
                }

            }
        }

    }
}
