using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[System.Serializable]
public struct MMAnimator
{
	PlayableGraph Graph;
	AnimationMixerPlayable Mixer;
	public int ClipCount
	{
		get { return Mixer.GetInputCount(); }
	}

	public int CurrentClip { get; private set; }
	public double CurrentTime { get { return GetPlayable(CurrentClip).GetTime(); } }

	public void Configure(UnityEngine.Animator animator, List<AnimationClip> sourceClips, Avatar avatar ,int nClipIdx = 0, bool removeIdleIK = false)
	{
		animator.applyRootMotion = true;
		animator.avatar = avatar;
		animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
		animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
		Graph = PlayableGraph.Create();
		Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
		Mixer = AnimationMixerPlayable.Create(Graph, sourceClips.Count);

		CurrentClip = nClipIdx;

		foreach(var tup in sourceClips.Zip(Enumerable.Range(0, sourceClips.Count), Tuple.Create))
        {
			(AnimationClip clip, int idx) = tup;
			AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(Graph, clip);
			if (idx == sourceClips.Count - 1 && removeIdleIK)
			{
				clipPlayable.SetApplyPlayableIK(false);
				clipPlayable.SetApplyFootIK(false);  // For the Idle animation
			}
			clipPlayable.Pause();
			Mixer.ConnectInput(idx, clipPlayable, 0);
		}
		 
		AnimationPlayableOutput output = AnimationPlayableOutput.Create(Graph, "AnimatedHumanoid", animator);
		output.SetSourcePlayable(Mixer);
	}

	public void Play()
	{
		if (!Graph.IsValid())
			return;
		Mixer.SetInputWeight(CurrentClip, 1);
		GetPlayable(CurrentClip).Play();
		Graph.Play();
	}

	public void Stop()
	{

		Graph.Stop();

	}

	public void Destroy()
	{
		if (!Graph.IsValid())
			return;
		Graph.Destroy();
	}

	Playable GetPlayable(int nClipIdx)
	{
		return Mixer.GetInput(nClipIdx);
	}

	public void CycleClip()
	{
		PlayClip((CurrentClip + 1) % Mixer.GetInputCount());
	}

	public void PlayClip(int targetClipIdx)
    {
		double oldTime = GetPlayable(CurrentClip).GetTime();
		GetPlayable(CurrentClip).Pause();
		SwitchToClip(targetClipIdx);
		JumpToTime(oldTime);
		GetPlayable(CurrentClip).Play();
	}

	public void PlayFromFrame(MotionMatcher.MMFrame frameIn)
	{
		GetPlayable(CurrentClip).Pause();
		SwitchToClip(frameIn.ClipIdx);
		JumpToTime(frameIn.TimeInClip);
		GetPlayable(CurrentClip).Play();
	}

	public void SwitchToClip(int nTargetClipIdx)
	{
		
		foreach (int nClipIdx in Enumerable.Range(0, Mixer.GetInputCount()))
		{
			Mixer.SetInputWeight(nClipIdx, nClipIdx == nTargetClipIdx ? 1 : 0);
		}
		CurrentClip = nTargetClipIdx;
		
	}

	public void JumpToTime(double timeIn)
    {
		GetPlayable(CurrentClip).SetTime(timeIn);
	}

	public void JumpToProgress(double dProg)
	{
		var clip = GetPlayable(CurrentClip);
		clip.SetTime(dProg * clip.GetDuration());
	}

	public void Evaluate(float dT)
    {
		Graph.Evaluate(dT);
    }


	public double Speed
    {
		get => GetPlayable(CurrentClip).GetSpeed();
		set => GetPlayable(CurrentClip).SetSpeed(value);
	}
}