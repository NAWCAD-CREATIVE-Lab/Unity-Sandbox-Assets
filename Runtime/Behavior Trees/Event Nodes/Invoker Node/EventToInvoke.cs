// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	/**
		A serialized pairing of a SandboxEvent to invoke, and an optional target
		to invoke it with.
	*/
	[type: Serializable]
	public class EventToInvoke
	{
		public SandboxEvent Event;

		public UnityEngine.Object Target;

		/**
			Attempt to create a Record from this EventToInvoke.

			Returns null if the Record constructor throws an
			ArgumentNullException.
		*/
		public Record TryCreateRecord()
		{
			Record record = null;

			try
			{ record = new Record(Event, Target); }

			catch (ArgumentNullException) { }

			return record;
		}

		/**
			A read-only Record of an EventToInvoke.

			Implements IEquatable and will be evaluated equal to any other
			EventToInvoke.Record object with the same Event and Target.
		*/
		public sealed class Record : IEquatable<Record>
		{
			/**
				The SandboxEvent to invoke.

				Will not be null.
			*/
			public readonly SandboxEvent Event;

			/**
				An optional target to invoke the SandboxEvent with.
			*/
			public readonly UnityEngine.Object Target;

			public Record(SandboxEvent sandboxEvent, UnityEngine.Object target)
			{
				if (sandboxEvent == null)
					throw new ArgumentNullException(nameof(sandboxEvent));

				Event = sandboxEvent;

				Target = target;
			}

			public bool Equals(Record eventToInvokeRecord)
			{
				if (eventToInvokeRecord == null)
					return false;

				return Event == eventToInvokeRecord.Event && Target == eventToInvokeRecord.Target;
			}

			override public int GetHashCode()
			{
				int hash = 0;

				if (Event != null)
					hash = Event.GetHashCode();

				if (Target != null)
					hash = hash ^ Target.GetHashCode();

				return hash;
			}
		}
	}
}