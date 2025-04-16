using System;
using System.Collections;
using System.Collections.Generic;
using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
    [type: Serializable]
	public sealed class EventToListenFor
	{
		public SandboxEvent Event;

		public List<UnityEngine.Object> TargetFilter;

		private List<UnityEngine.Object> CleanTargetFilter
		{
			get
			{
				List<UnityEngine.Object> cleanedTargetFilter = new List<UnityEngine.Object>();

				if (TargetFilter != null)
					foreach (UnityEngine.Object target in TargetFilter)
						if (target!=null && !cleanedTargetFilter.Contains(target))
							cleanedTargetFilter.Add(target);
				
				return cleanedTargetFilter;
			}
		}

		public EventToListenFor CleanClone
		{
			get
			{
				if (this.Event == null)
					return null;
				
				return new EventToListenFor()
				{
					Event = this.Event,

					TargetFilter = this.CleanTargetFilter
				};
			}
		}

		public EventToListenFor() { }

		public EventToListenFor(EventToListenForWithBranch eventToListenForWithBranch)
		{
			if (eventToListenForWithBranch != null)
			{
				Event = eventToListenForWithBranch.Event;

				TargetFilter = eventToListenForWithBranch.TargetFilter;
			}
		}

		public List<EventToListenFor> TargetSegements
		{
			get
			{
				List<EventToListenFor> segments = new List<EventToListenFor>();

				if (TargetFilter.Count==0)
					segments.Add(new EventToListenFor() { Event = Event });

				else
					foreach (UnityEngine.Object target in TargetFilter)
						segments.Add
						(
							new EventToListenFor()
							{
								Event = Event,
								TargetFilter = new List<UnityEngine.Object>() { target }
							}
						);
				
				return segments;
			}
		}

		override public bool Equals(object eventToListenForObject)
		{
			if (eventToListenForObject == null)
				return false;
			
			if (!(eventToListenForObject is EventToListenFor))
				throw new ArgumentException
					(
						nameof(eventToListenForObject),
						nameof(eventToListenForObject) + " is not of type EventToListenFor"
					);
			
			EventToListenFor eventToListenFor = eventToListenForObject as EventToListenFor;
			
			if (Event != eventToListenFor.Event)
				return false;
			
			List<UnityEngine.Object> cleanedTargetFilter1 = CleanTargetFilter;
			List<UnityEngine.Object> cleanedTargetFilter2 = eventToListenFor.CleanTargetFilter;

			if (cleanedTargetFilter1.Count != cleanedTargetFilter2.Count)
				return false;
			
			foreach (UnityEngine.Object target in cleanedTargetFilter1)
				if (!cleanedTargetFilter2.Contains(target))
					return false;
			
			return true;
		}

		override public int GetHashCode()
		{
			int hash = 0;

			if (Event!=null)
				hash = Event.GetHashCode();
			
			foreach (UnityEngine.Object target in CleanTargetFilter)
				hash = hash ^ target.GetHashCode();
			
			return hash;
		}
	}
}