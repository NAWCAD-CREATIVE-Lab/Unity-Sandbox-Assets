// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CREATIVE.SandboxAssets.Events
{
	/**
		Interface that all Sandbox Event Listeners must implement.

		Requires:
			- An event to listen to
			- An Object that represents the listener
			  (will display in the SandboxEvent inspector)
			- A method that will be called when the event is invoked.
			  This method must accept a target parameter and return true if the
			  listener should be dropped after the Invoke method completes
			- A list of objects that will prevent any action from being taken
			  when the Event is invoked, unless the target is contained inside
			  it. Can be null to allow all targets.
	*/
	public interface Listener
	{
		public abstract SandboxEvent Event
			{ get; }
		
		public abstract UnityEngine.Object ListeningObject
			{ get; }
		
		public abstract IEnumerable<UnityEngine.Object> TargetFilter
			{ get; }
		
		public abstract bool Invoke(UnityEngine.Object target = null);
	}
}
