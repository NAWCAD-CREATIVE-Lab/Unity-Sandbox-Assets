// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	/**
		A helper class for ListenerNode to aggregate data and listen for events.

		It exists to avoid managing a lot of confusing static members in the
		ListenerNode class.

		Begins listening for events immediately on creation, but doesn't
		actually begin evaluating nodes until EvaluateNode is called.
	*/
	public class ListenerNodeRunner
	{
		/*
			These objects are the ones notified by the SandboxEvent objects that
			they have been invoked.
		*/
		readonly IReadOnlySet<DelegateListener> registeredListeners;

		/*
			For each listener node, whether or not each contained event has been
			fulfilled.
		*/
		readonly IReadOnlyDictionary<ListenerNode.Record, IDictionary<EventToListenFor.Record, bool>> statusIndex;

		// Will be called before a new node begins to be evaluated 
		readonly Action<Node.IRecord<Node>> nextNodeStarted;

		// Will be called if there are no more nodes to evaluate
		readonly Action noNextNode;

		// A table of all evaluate functions for each possible node in the graph
		readonly IReadOnlyDictionary<Node.IRecord<Node>, Action> EvaluateForNodes;

		bool teardownPerformed = false;

		bool evaluating = false;

		ListenerNode.Record currentNode;

		public ListenerNodeRunner
		(
			IReadOnlySet<ListenerNode.Record> nodes,
			GameObject runningObject,
			Action<Node.IRecord<Node>> nextNodeStarted,
			Action noNextNode,
			IReadOnlyDictionary<Node.IRecord<Node>, Action> evaluateForNodes
		)
		{
			if (nodes == null)
				throw new ArgumentNullException(nameof(nodes));

			if (runningObject == null)
				throw new ArgumentNullException(nameof(runningObject));

			if (nextNodeStarted == null)
				throw new ArgumentNullException(nameof(nextNodeStarted));

			if (noNextNode == null)
				throw new ArgumentNullException(nameof(noNextNode));

			if (evaluateForNodes == null)
				throw new ArgumentNullException(nameof(evaluateForNodes));

			this.nextNodeStarted = nextNodeStarted;

			this.noNextNode = noNextNode;

			this.EvaluateForNodes = evaluateForNodes;

			HashSet<DelegateListener> registeredListeners = new HashSet<DelegateListener>();

			Dictionary<ListenerNode.Record, IDictionary<EventToListenFor.Record, bool>> statusIndex =
				new Dictionary<ListenerNode.Record, IDictionary<EventToListenFor.Record, bool>>();

			/*
				All necessary information needed to create the DelegateListener
				objects.

				Basically just a list of all listener nodes turned inside-out.

				Instead of a list of nodes that contain events to listen for,
				it's a list of events to listen for, and the nodes that
				contain them.

				The data needs to be re-organized like this so that each event
				that may be required by multiple nodes is only registered with
				once per graph.
			*/
			Dictionary<EventToListenFor.Record, Dictionary<ListenerNode.Record, EventToListenFor.Record>>
				listenerSetupInfo =
					new Dictionary<EventToListenFor.Record, Dictionary<ListenerNode.Record, EventToListenFor.Record>>();

			foreach (ListenerNode.Record listenerNode in nodes)
			{
				if (listenerNode != null)
				{
					statusIndex.Add(listenerNode, new Dictionary<EventToListenFor.Record, bool>());

					foreach (EventToListenFor.Record eventToListenFor in listenerNode.EventsToListenFor)
					{
						statusIndex[listenerNode].Add(eventToListenFor, false);

						foreach
							(EventToListenFor.Record eventToListenForSegment in eventToListenFor.CreateTargetSegments())
						{
							if (!listenerSetupInfo.ContainsKey(eventToListenForSegment))
							{
								Dictionary<ListenerNode.Record, EventToListenFor.Record> listeningNodeInfoIndex =
									new Dictionary<ListenerNode.Record, EventToListenFor.Record>();

								listenerSetupInfo.Add(eventToListenForSegment, listeningNodeInfoIndex);

								/*
									The listener is set up to call HandleEvent
									when the SandboxEvent is invoked, along with
									a dictionary of which EventToListenFor has
									just been fulfilled for any given
									ListenerNode.

									This data is embedded into the anonymous
									functions of the listeners, and no longer
									needs to be stored.
								*/
								DelegateListener listener = new DelegateListener
								(
									eventToListenForSegment.Event,
									runningObject,
									(target) => HandleEvent(listeningNodeInfoIndex),
									eventToListenForSegment.TargetFilter
								);

								listener.Enable();

								registeredListeners.Add(listener);
							}

							listenerSetupInfo[eventToListenForSegment].Add
								(listenerNode, eventToListenFor);
						}
					}
				}
			}

			this.registeredListeners = new ReadOnlySet<DelegateListener>(registeredListeners);

			this.statusIndex =
				new ReadOnlyDictionary<ListenerNode.Record, IDictionary<EventToListenFor.Record, bool>>(statusIndex);
		}

		public void Teardown()
		{
			foreach (DelegateListener listener in registeredListeners)
				listener.Disable();

			evaluating = false;

			teardownPerformed = true;
		}

		public void EvaluateNode(ListenerNode.Record listenerNode)
		{
			if (teardownPerformed)
				throw new InvalidOperationException("Teardown has already been performed.");

			if (evaluating)
				throw new InvalidOperationException("Another ListenerNode is currently being evaluated.");

			if (!statusIndex.ContainsKey(listenerNode))
				throw new ArgumentException
					(nameof(listenerNode), nameof(listenerNode) + " was not registered with this ListenerNodeRunner");

			currentNode = listenerNode;

			evaluating = true;
		}

		/*
			Called by the DelegateListener objects when a SandboxEvent is
			invoked.

			listeningNodeInfoIndex indicates the EventToListenFor that should be
			fulfilled for each containing ListenerNode.

			Always returns false, to indicate to the DelegateListener that the
			event should stay registered. Even if the ListenerNode is finished
			evaluating, it may be evaluated again later in the graph.
		*/
		bool HandleEvent(IReadOnlyDictionary<ListenerNode.Record, EventToListenFor.Record> listeningNodeInfoIndex)
		{
			/*
				This callback is only relevant if this runner is currently
				evaluating a ListenerNode that is referenced in
				listeningNodeInfoIndex
			*/
			if (evaluating && listeningNodeInfoIndex.ContainsKey(currentNode))
			{
				EventToListenFor.Record eventToListenFor = listeningNodeInfoIndex[currentNode];

				IDictionary<EventToListenFor.Record, bool> eventListenerStatusTable = statusIndex[currentNode];

				// Event fulfilled!
				eventListenerStatusTable[eventToListenFor] = true;

				// If there are still unfulfilled events in the node, return
				if (!currentNode.BranchOnCompletion && !currentNode.CompleteOnFirstEvent)
					foreach (bool registeredListenerStatus in eventListenerStatusTable.Values)
						if (!registeredListenerStatus)
							return false;

				/*
					If we're at this point, the node has been completely
					fulfilled.

					Reset the status index.
				*/
				foreach
				(
					EventToListenFor.Record eventToListenForIterator in
					new List<EventToListenFor.Record>(eventListenerStatusTable.Keys)
				)
					eventListenerStatusTable[eventToListenForIterator] = false;

				/*
					Find the node to evaluate next
				*/
				Node.IRecord<Node> nextNode = currentNode.BranchOnCompletion ?
					currentNode.EventsToListenForWithBranches[eventToListenFor] :
					currentNode.NextNode;

				evaluating = false;

				// Either stop evaluating, or evaluate the next node
				if (nextNode == null)
					noNextNode();

				else
				{
					nextNodeStarted(nextNode);

					EvaluateForNodes[nextNode]();
				}
			}

			return false;
		}
	}
}