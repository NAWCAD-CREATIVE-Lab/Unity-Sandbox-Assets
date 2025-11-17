// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	/**
		A unit of a behavior Sequence consisting of two parts, evaluated in
		order:
		- A list of SandboxEvent objects to listen for
		- A list of SandboxEvent objects to invoke

		The listening phase of the Step may be considered complete either when
		all of the events to listen for have been invoked, or when any one of
		the events have been invoked.
	*/
	[type: Serializable]
	public class Step
	{
		[field: SerializeField] List<EventToListenFor> EventsToListenFor;
		
		[field: SerializeField] bool CompleteOnFirstEvent;

		[field: SerializeField] List<EventToInvoke> EventsToInvoke;

		/**
			Converts this Step into an equivalent tuple of a ListenerNode and
			an InvokerNode.
		*/
		public (ListenerNode, InvokerNode) CreateNodes()
		{
			InvokerNode invokerNode = new InvokerNode() { EventsToInvoke = this.EventsToInvoke };

			ListenerNode listenerNode = new ListenerNode()
			{
				BranchOnCompletion = false,

				CompleteOnFirstEvent = this.CompleteOnFirstEvent,

				EventsToListenFor = new List<EventToListenFor.WithBranch>()
			};

			if (EventsToListenFor != null)
				foreach (EventToListenFor eventToListenFor in EventsToListenFor)
					listenerNode.EventsToListenFor.Add
					(
						new EventToListenFor.WithBranch
						{ Event = eventToListenFor.Event, TargetFilter = eventToListenFor.TargetFilter }
					);

			listenerNode.NextNode = invokerNode;

			return (listenerNode, invokerNode);
		}

		/**
			Converts an ordered list of Step objects into an equivalent ordered
			list of tuples, each containing a ListenerNode and an InvokerNode. 
		*/
		static public IReadOnlyList<(ListenerNode, InvokerNode)> CreateNodes(IReadOnlyList<Step> steps)
		{
			if (steps == null)
				throw new ArgumentNullException(nameof(steps));

			List<(ListenerNode, InvokerNode)> nodes = new List<(ListenerNode, InvokerNode)>();
			ListenerNode lastCreatedNode = null;
			for (int i = (steps.Count - 1); i >= 0; i--)
			{
				if (steps[i] != null)
				{
					(ListenerNode, InvokerNode) newNodes = steps[i].CreateNodes();

					newNodes.Item2.NextNode = lastCreatedNode;

					lastCreatedNode = newNodes.Item1;

					nodes.Insert(0, newNodes);
				}
			}

			return nodes;
		}
	}
}