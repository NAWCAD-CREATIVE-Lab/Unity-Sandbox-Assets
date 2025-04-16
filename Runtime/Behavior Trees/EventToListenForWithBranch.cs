using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
    [type: Serializable]
	public sealed class EventToListenForWithBranch
	{
		public SandboxEvent Event;

		public List<UnityEngine.Object> TargetFilter;

		[field: SerializeReference]
		public Node NextNode;

		public EventToListenForWithBranch CleanCloneWithoutNextNode
		{
			get
			{
				EventToListenFor eventToListenFor = (new EventToListenFor(this)).CleanClone;

				if (eventToListenFor == null)
					return null;
				
				return new EventToListenForWithBranch(eventToListenFor);
			}
		}

		public EventToListenForWithBranch() { }

		public EventToListenForWithBranch(EventToListenFor eventToListenFor)
		{
			if (eventToListenFor != null)
			{
				Event = eventToListenFor.Event;

				TargetFilter = eventToListenFor.TargetFilter;
			}
		}

		override public bool Equals(object eventToListenForWithBranchObject)
		{
			if (eventToListenForWithBranchObject == null)
				return false;
			
			if (!(eventToListenForWithBranchObject is EventToListenForWithBranch))
				throw new ArgumentException
					(
						nameof(eventToListenForWithBranchObject),
						nameof(eventToListenForWithBranchObject) + " is not of type EventToListenForWithBranch"
					);
			
			EventToListenForWithBranch eventToListenForWithBranch =
				eventToListenForWithBranchObject as EventToListenForWithBranch;
			
			if (!(new EventToListenFor(this)).Equals(new EventToListenFor(eventToListenForWithBranch)))
				return false;
			
			return NextNode == eventToListenForWithBranch.NextNode;
		}

		override public int GetHashCode()
		{
			int hash = (new EventToListenFor(this)).GetHashCode();
			
			if (NextNode != null)
				hash = hash ^ NextNode.GetHashCode();
			
			return hash;
		}
	}

	[type: Serializable]
	public sealed class EventToInvoke
	{
		public SandboxEvent Event;

		public UnityEngine.Object Target;

		public EventToInvoke CleanClone
		{
			get
			{
				if (this.Event == null)
					return null;
				
				return new EventToInvoke()
				{
					Event = this.Event,
					Target = this.Target
				};
			}
		}

		override public bool Equals(object eventToInvokeObject)
		{
			if (eventToInvokeObject == null)
				return false;
			
			if (!(eventToInvokeObject is EventToInvoke))
				throw new ArgumentException
					(
						nameof(eventToInvokeObject),
						nameof(eventToInvokeObject) + " is not of type EventToInvoke"
					);
			
			EventToInvoke eventToInvoke = eventToInvokeObject as EventToInvoke;
			
			return Event==eventToInvoke.Event && Target==eventToInvoke.Target;
		}

		override public int GetHashCode()
		{
			int hash = 0;

			if (Event!=null)
				hash = Event.GetHashCode();
			
			if (Target!=null)
				hash = hash ^ Target.GetHashCode();
			
			return hash;
		}
	}
}