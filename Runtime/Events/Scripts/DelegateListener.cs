using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CREATIVE.SandboxAssets.Events
{
	/**
		A listener for SandboxEvents that uses the C# delegate system and
		can reside internally in scripts.

		This is useful for subscribing to multiple events from a single script.
	*/
	public sealed class DelegateListener : Listener
	{
		public bool Enabled
			{ get; private set; }

		public SandboxEvent Event
			{ get; private set; }
		
		public UnityEngine.Object ListeningObject
			{ get; private set; }

		/**
			The method to call when the SandboxEvent is invoked must have a
			signature matching this delegate.
		*/
		public delegate bool EventHandler(UnityEngine.Object target);

		private EventHandler handler;

		/**
			If this list is null, the listener will work normally
			
			If this list is not null, the listener will only take action from
			the SandboxEvent if the target of the invocation is in the list
		*/
		public IEnumerable<UnityEngine.Object> TargetFilter
			{ get; private set; }

		/**
			A constructor that accepts:
				- A SandboxEvent to listen to
				- An object that will represent the listener
				- A method that will be called when the event is invoked
				- A list of objects that will prevent any action from being
				  taken when the Event is invoked, unless the target is
				  contained inside it. Can be null to allow all targets.
			
			These parameters cannot be changed after the object is constructed.
		*/
		public DelegateListener
		(
			SandboxEvent sandboxEvent,
			UnityEngine.Object listeningObject,
			EventHandler handler,
			IEnumerable<UnityEngine.Object> targetFilter = null
		)
		{
			if (sandboxEvent==null)
				throw new ArgumentNullException("sandboxEvent");
			
			if (listeningObject==null)
				throw new ArgumentNullException("listeningObject");
			
			if (handler==null)
				throw new ArgumentNullException("handler");
			
			Event = sandboxEvent;
			ListeningObject = listeningObject;
			this.handler = handler;

			List<UnityEngine.Object> tempFilter = null;

			if (targetFilter != null)
			{
				foreach (UnityEngine.Object obj in targetFilter)
				{
					if (obj!=null)
					{
						if (tempFilter==null)
							tempFilter = new List<UnityEngine.Object>();
						
						if (!tempFilter.Contains(obj))
							tempFilter.Add(obj);
					}
				}
			}

			TargetFilter = tempFilter;
		}

		~DelegateListener() => Disable();

		public void Enable()
		{
			if (!Enabled)
			{
				Event.AddListener(this);
				Enabled = true;
			}
		}

		public void Disable()
		{
			if (Enabled)
			{
				Event.DropListener(this);
				Enabled = false;
			}
		}
		
		public bool Invoke(UnityEngine.Object target)
		{
			if (!Enabled)
				throw new InvalidOperationException
					("Something attepted to invoke a Sandbox Event Delegate Listener that had been disabled.");
			
			bool passesTargetFilter = TargetFilter==null;

			if (TargetFilter!=null)
				foreach (UnityEngine.Object targetMatch in TargetFilter)
					if (targetMatch == target)
						passesTargetFilter = true;
			
			if (Enabled && passesTargetFilter && handler(target))
			{
				Enabled = false;
				return true;
			}

			return false;
		}
	}
}