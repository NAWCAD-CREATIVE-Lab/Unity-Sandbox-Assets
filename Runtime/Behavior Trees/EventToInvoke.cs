using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
    [type: Serializable]
	public class EventToInvoke
	{
		public SandboxEvent Event = null;

		public UnityEngine.Object Target = null;

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