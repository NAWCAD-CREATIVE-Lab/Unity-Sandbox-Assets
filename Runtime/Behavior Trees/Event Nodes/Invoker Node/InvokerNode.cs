using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	/**
		A Node that invokes a list of events and then immediately executes
		another Node.
	*/
	[type: Serializable]
	public class InvokerNode : Node
	{
		[field: SerializeReference]
		public Node NextNode;

		[field: SerializeReference]
		public List<EventToInvoke> EventsToInvoke;

		override public Node.IRecord<Node> CreateRecord
		(
			IReadOnlySet<IRecord<Node>> graph,
			GameObject runningObject,
			Action<IRecord<Node>> nodeStarted,
			Action stopped,
			out SetRecordReferences setReferences,
			out Action setup,
			out Action evaluate,
			out Action teardown
		) =>
			new Record
			(
				graph,
				this,
				Position,
				runningObject,
				nodeStarted,
				stopped,
				out setReferences,
				out setup,
				out teardown,
				out evaluate,
				EventsToInvoke
			);

		/**
			A read-only record of an InvokerNode.
		*/
		public class Record : Node.Record<InvokerNode>
		{
			/**
				The node that will be evaluated after this one.
			*/
			public Node.IRecord<Node> NextNode { get; private set; }

			/**
				A unique set of events to invoke.

				Will not be null, nor contain null.
			*/
			public readonly ReadOnlySet<EventToInvoke.Record> EventsToInvoke;

			/*
				For any possible graph that includes an InvokerNode, the events
				that those nodes might invoke. And a bool indicating whether
				they have been registered.

				This needs to be static, and the list of events de-duplicated,
				so that each graph only registers itself once per-event, even
				if there might be multiple invoker nodes invoking the same
				event.
			*/
			static readonly Dictionary<IReadOnlySet<IRecord<Node>>, (HashSet<SandboxEvent>, bool)>
				registeredEventsForGraphs =
					new Dictionary<IReadOnlySet<IRecord<Node>>, (HashSet<SandboxEvent>, bool)>();

			public Record
			(
				IReadOnlySet<IRecord<Node>> graph,
				InvokerNode original,
				Vector2 position,
				GameObject runningObject,
				Action<IRecord<Node>> nodeStarted,
				Action stopped,
				out SetRecordReferences setReferences,
				out Action setup,
				out Action teardown,
				out Action evaluate,
				IEnumerable<EventToInvoke> eventsToInvoke
			)
				: base
				(
					graph,
					original,
					position,
					runningObject,
					nodeStarted,
					stopped,
					out setReferences,
					out setup,
					out evaluate,
					out teardown
				)
			{
				HashSet<EventToInvoke.Record> eventsToInvokeSet = new HashSet<EventToInvoke.Record>();

				EventsToInvoke = new ReadOnlySet<EventToInvoke.Record>(eventsToInvokeSet);

				if (eventsToInvoke != null)
				{
					foreach (EventToInvoke eventToInvoke in eventsToInvoke)
					{
						if (eventToInvoke != null)
						{
							EventToInvoke.Record eventToInvokeRecord = eventToInvoke.TryCreateRecord();

							if (eventToInvokeRecord != null && !eventsToInvokeSet.Add(eventToInvokeRecord))
								throw new InvalidOperationException("Invoker Node has duplicate Events to Invoke.");
						}
					}
				}

				if (registeredEventsForGraphs.ContainsKey(Graph) && registeredEventsForGraphs[Graph].Item2)
					throw new InvalidOperationException
						("Other InvokerNode objects in this graph have already been set up");

				if (!registeredEventsForGraphs.ContainsKey(Graph))
						registeredEventsForGraphs[Graph] = (new HashSet<SandboxEvent>(), false);

				foreach (EventToInvoke.Record eventToInvoke in EventsToInvoke)
					registeredEventsForGraphs[Graph].Item1.Add(eventToInvoke.Event);
			}

			override protected void OnSetReferences
			(
				IReadOnlyList<Node> nodesReferenced,
				IReadOnlyList<IRecord<Node>> recordsToReference
			)
			{
				if (Original.NextNode != null)
				{
					int index = nodesReferenced.IndexOf(Original.NextNode);

					if (index == -1)
						throw new ArgumentException
						(
							nameof(nodesReferenced),
							nameof(nodesReferenced) +
							" does not contain a certain Node that is referenced by " +
							" the Invoker Node from which this Record was created."
						);

					NextNode = recordsToReference[index];
				}
			}

			/**
				Registers all SandboxEvent objects that the graph containing
				this Node might invoke.

				Ensures that this is only done once for the graph, and that
				any calls to OnSetup for other InvokerNode objects in the same
				graph are redundant.
			*/
			override protected void OnSetup()
			{
				if (!registeredEventsForGraphs[Graph].Item2)
				{
					foreach (SandboxEvent sandboxEvent in registeredEventsForGraphs[Graph].Item1)
						sandboxEvent.AddInvoker(RunningObject);

					registeredEventsForGraphs[Graph] = (registeredEventsForGraphs[Graph].Item1, true);
				}
			}

			/**
				Un-registers all SandboxEvent objects that the graph containing
				this Node might invoke.

				Ensures that this is only done once for the graph, and that
				any calls to OnSetup for other InvokerNode objects in the same
				graph are redundant.
			*/
			override protected void OnTeardown()
			{
				if (registeredEventsForGraphs[Graph].Item2)
				{
					foreach (SandboxEvent sandboxEvent in registeredEventsForGraphs[Graph].Item1)
						sandboxEvent.DropInvoker(RunningObject);

					registeredEventsForGraphs[Graph] = (registeredEventsForGraphs[Graph].Item1, false);
				}
			}

			/**
				Invokes all events specififed in this node, and evaluates the
				next node.
			*/
			override protected void OnEvaluate()
			{
				foreach (EventToInvoke.Record eventToInvoke in EventsToInvoke)
					eventToInvoke.Event.Invoke(RunningObject, eventToInvoke.Target);

				if (NextNode == null)
					Stopped();

				else
				{
					NodeStarted(NextNode);
					EvaluateForNodes[NextNode]();
				}
			}
		}
	}
}