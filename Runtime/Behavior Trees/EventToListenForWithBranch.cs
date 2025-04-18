using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
    [type: Serializable]
	public class EventToListenForWithBranch
	{
		public SandboxEvent Event = null;

		public List<UnityEngine.Object> TargetFilter = new List<UnityEngine.Object>();

		[field: SerializeReference]
		public Node NextNode = null;

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
}