using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CREATIVE.SandboxAssets.Events
{
	/**
		Allows an event to be invoked when an animation state is being entered
		or exited.

		Requires the Animation Controller asset to be registered as an invoker
		in the inspector.
	*/
	public class AnimationStateInvoker : StateMachineBehaviour
	{
		[field: SerializeField] SandboxEvent EventOnStateEnter = null;
		[field: SerializeField] SandboxEvent EventOnStateExit = null;

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			if (EventOnStateEnter!=null)
				EventOnStateEnter.Invoke(animator.runtimeAnimatorController);
		}

		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			if (EventOnStateExit!=null)
				EventOnStateExit.Invoke(animator.runtimeAnimatorController);
		}
	}
}