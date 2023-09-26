using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MMDataset", menuName = "MMDataset", order = 2)]
public class MMDataset : ScriptableObject
{
	public List<Motion> motionList;
	public int nDimensions;
	public int nTrajectoryPoints;

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
		public TextAsset file;
		public double freq;
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
            return "["+start+", "+stop+"]";
        }
    }

}



