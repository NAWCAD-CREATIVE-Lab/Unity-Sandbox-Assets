using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	/**
		A serialized pairing of a SandboxEvent to listen for, and an optional
		list of targets to filter the invocation against.
	*/
	[type: Serializable]
	public class EventToListenFor
	{
		public SandboxEvent Event;

		public List<UnityEngine.Object> TargetFilter;

		[type: Serializable]
		public class WithBranch : EventToListenFor
		{
			[field: SerializeReference]
			public Node NextNode;
		}

		/**
			Attempt to create a Record from this EventToListenFor.

			Returns null if the Record constructor throws an
			ArgumentNullException.
		*/
		public Record TryCreateRecord()
		{
			Record record = null;

			try
			{ record = new Record(Event, TargetFilter); }

			catch (ArgumentNullException) { }

			return record;
		}

		/**
			A read-only Record of an EventToListenFor.

			Implements IEquatable and will be evaluated equal to any other
			EventToListenFor.Record that has the same Event and an equivalent
			target filter.
		*/
		public sealed class Record : IEquatable<Record>
		{
			/**
				The SandboxEvent to listen for.

				Will not be null.
			*/
			public readonly SandboxEvent Event;

			/**
				A unique set of target objects to filter any invocation of the
				SandboxEvent against.

				Will not be null or contain null.
			*/
			public readonly ReadOnlySet<UnityEngine.Object> TargetFilter;

			public Record(SandboxEvent sandboxEvent, IEnumerable<UnityEngine.Object> targetFilter)
			{
				if (sandboxEvent == null)
					throw new ArgumentNullException(nameof(sandboxEvent));

				Event = sandboxEvent;

				HashSet<UnityEngine.Object> targetFilterSet = new HashSet<UnityEngine.Object>();

				TargetFilter = new ReadOnlySet<UnityEngine.Object>(targetFilterSet);

				if (targetFilter != null)
					foreach (UnityEngine.Object target in targetFilter)
						if (target != null && !targetFilterSet.Add(target))
							throw new InvalidOperationException("Listener Node has duplicate Events to Listen For.");
			}

			/**
				If the target filter contains multiple objects, this function
				will split this EventToListenFor.Record into an Enumerable of
				records, each with a target filter that contains only one of the 
				objects in the original target filter.

				If the target filter contains zero or one items, this function
				will return an Enumerable containing only an identical copy of
				this record.

				The SandboxEvent to listen for will remain the same in all
				segments.
			*/
			public IEnumerable<Record> CreateTargetSegments()
			{
				List<Record> segments = new List<Record>();

				if (TargetFilter.Count == 0)
					segments.Add(new Record(Event, null));

				else
					foreach (UnityEngine.Object target in TargetFilter)
						segments.Add(new Record(Event, new List<UnityEngine.Object>() { target }));

				return segments;
			}

			public bool Equals(Record eventToListenForRecord)
			{
				if (eventToListenForRecord == null)
					return false;

				return
					Event == eventToListenForRecord.Event &&
					TargetFilter.SetEquals(eventToListenForRecord.TargetFilter);
			}

			override public int GetHashCode()
			{
				int hash = 0;

				if (Event != null)
					hash = Event.GetHashCode();

				foreach (UnityEngine.Object target in TargetFilter)
					hash = hash ^ target.GetHashCode();

				return hash;
			}
		}
	}
}