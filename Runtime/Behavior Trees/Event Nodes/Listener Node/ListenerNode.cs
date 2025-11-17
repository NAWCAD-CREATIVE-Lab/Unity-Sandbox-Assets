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
		A Node that listens for a list of events.

		Can either execute one specific Node on completion, or branch to 
		one of a set of different nodes depending on which event in the list was
		invoked.

		Can either wait for every event in the list to be invoked before
		completion, or complete immediately after any event in the list is
		invoked.
	*/
	[type: Serializable]
	public class ListenerNode : Node
	{
		[field: SerializeReference]
		public Node NextNode;
		
		public bool BranchOnCompletion;
		
		public bool CompleteOnFirstEvent;
		
		[field: SerializeReference]
		public List<EventToListenFor.WithBranch> EventsToListenFor;

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
				out evaluate,
				out teardown,
				BranchOnCompletion,
				CompleteOnFirstEvent,
				EventsToListenFor
			);

		/**
			A read-only record of a ListenerNode.
		*/
		public class Record : Node.Record<ListenerNode>
		{
			static readonly InvalidOperationException readonlyException =
				new InvalidOperationException("This Listener Node Branches On Completion.");

			/**
				If false, the node executes NextNode on completion,
				
				If true, the node looks up the first relevant event invoked as a
				key in EventsToListenForWithBranches and executes the node found
				immediately.
			*/
			public readonly bool BranchOnCompletion;

			/**
				If true, the node is considered complete after the first
				relevant event is invoked.

				If false, all relevant events must be invoked before the node is
				considered complete.
				
				Throws an exception if accessed when BranchOnCompletion is true,
				as the node will always complete after the first event is
				invoked.
			*/
			public bool CompleteOnFirstEvent
			{
				get
				{
					if (BranchOnCompletion)
						throw readonlyException;

					return completeOnFirstEvent;
				}
			}
			readonly bool completeOnFirstEvent;

			/**
				The node that will be evaluated after this one completes,
				if BranchOnCompletion is false.

				Throws an exception if accessed when BranchOnCompletion is true.
			*/
			public Node.IRecord<Node> NextNode
			{
				get
				{
					if (BranchOnCompletion)
						throw readonlyException;

					return nextNode;
				}
			}
			Node.IRecord<Node> nextNode;

			/**
				A unique set of events to listen for.

				Will not be null or contain null.

				Contains the relevant events to listen for regardless of whether
				BranchOnCompletion is true or false.
			*/
			public readonly ReadOnlySet<EventToListenFor.Record> EventsToListenFor;

			/**
				A unique set of events to listen for, and the node that will be
				executed next for each event, if it is the first one invoked
				after the node begins executing.

				Will not be null, or contain a null key.

				Throws an exception if accessed when BranchOnCompletion is
				false.
			*/
			public ReadOnlyDictionary<EventToListenFor.Record, Node.IRecord<Node>> EventsToListenForWithBranches
			{
				get
				{
					if (!BranchOnCompletion)
						throw new InvalidOperationException("This Listener Node does not Branch On Completion.");

					return readOnlyEventsToListenForWithBranches;
				}
			}
			/*
				These are just both just backing fields.
				The ReadOnlyDictionary is exposed in the property above, and the
				regular dictionary is edited internally
			*/
			readonly ReadOnlyDictionary<EventToListenFor.Record, Node.IRecord<Node>>
				readOnlyEventsToListenForWithBranches;
			readonly Dictionary<EventToListenFor.Record, Node.IRecord<Node>>
				eventsToListenForWithBranches;

			/*
				For any possible graph that includes an ListenerNode, a set of
				all ListenerNodes in that graph, and a ListenerNodeRunner
				created to evaluate those nodes.
			*/
			static readonly Dictionary<IReadOnlySet<IRecord<Node>>, (HashSet<Record>, ListenerNodeRunner)>
				nodesAndRunnersForGraphs =
					new Dictionary<IReadOnlySet<IRecord<Node>>, (HashSet<Record>, ListenerNodeRunner)>();

			public Record
			(
				IReadOnlySet<IRecord<Node>> graph,
				ListenerNode original,
				Vector2 position,
				GameObject runningObject,
				Action<Node.IRecord<Node>> nodeStartedRunning,
				Action stoppedRunning,
				out SetRecordReferences setReferences,
				out Action setup,
				out Action evaluate,
				out Action teardown,
				bool branchOnCompletion,
				bool completeOnFirstEvent,
				IEnumerable<EventToListenFor.WithBranch> eventsToListenForWithBranches
			)
				: base
				(
					graph,
					original,
					position,
					runningObject,
					nodeStartedRunning,
					stoppedRunning,
					out setReferences,
					out setup,
					out evaluate,
					out teardown
				)
			{
				BranchOnCompletion = branchOnCompletion;

				this.completeOnFirstEvent = completeOnFirstEvent;

				HashSet<EventToListenFor.Record> eventsToListenForSet = new HashSet<EventToListenFor.Record>();

				EventsToListenFor = new ReadOnlySet<EventToListenFor.Record>(eventsToListenForSet);

				if (BranchOnCompletion)
				{
					this.eventsToListenForWithBranches = new Dictionary<EventToListenFor.Record, Node.IRecord<Node>>();

					this.readOnlyEventsToListenForWithBranches =
						new ReadOnlyDictionary<EventToListenFor.Record, Node.IRecord<Node>>
							(this.eventsToListenForWithBranches);
				}

				else
				{
					this.eventsToListenForWithBranches = null;

					this.readOnlyEventsToListenForWithBranches = null;
				}

				if (eventsToListenForWithBranches != null)
				{
					foreach (EventToListenFor.WithBranch eventToListenForWithBranch in eventsToListenForWithBranches)
					{
						if (eventToListenForWithBranch != null)
						{
							EventToListenFor.Record eventToListenForRecord =
								eventToListenForWithBranch.TryCreateRecord();

							if (eventToListenForRecord != null)
							{
								if (EventsToListenFor.Contains(eventToListenForRecord))
									throw new ArgumentException
									(
										nameof(eventsToListenForWithBranches),
										nameof(eventsToListenForWithBranches) +
										" contains duplicate Events To Listen For."
									);

								eventsToListenForSet.Add(eventToListenForRecord);

								if (BranchOnCompletion)
									this.eventsToListenForWithBranches.Add(eventToListenForRecord, null);
							}
						}
					}
				}

				if (EventsToListenFor.Count == 0)
					throw new ArgumentException
					(
						nameof(eventsToListenForWithBranches),
						nameof(eventsToListenForWithBranches) + " contains no Events To Listen For."
					);
				
				if (nodesAndRunnersForGraphs.ContainsKey(Graph) && nodesAndRunnersForGraphs[Graph].Item2!=null)
					throw new InvalidOperationException
						("Other ListenerNode objects in this graph have already been set up");

				if (!nodesAndRunnersForGraphs.ContainsKey(Graph))
					nodesAndRunnersForGraphs[Graph] = (new HashSet<Record>(), null);

				nodesAndRunnersForGraphs[Graph].Item1.Add(this);
			}

			override protected void OnSetReferences
			(
				IReadOnlyList<Node> nodesReferenced,
				IReadOnlyList<Node.IRecord<Node>> recordsToReference
			)
			{
				InvalidOperationException nodeReferenceNotFoundException = new InvalidOperationException
				(
					"The Listener Node from which this Record was created " +
					" does not contain an Event To Listen For With Branch that matches " +
					" an Event To Listen For in this Record."
				);

				if (BranchOnCompletion)
				{
					if (Original.EventsToListenFor != null)
					{
						foreach
							(EventToListenFor.WithBranch eventToListenForWithBranch in Original.EventsToListenFor)
						{
							if (eventToListenForWithBranch != null && eventToListenForWithBranch.NextNode != null)
							{
								EventToListenFor.Record eventToListenForRecord =
									eventToListenForWithBranch.TryCreateRecord();

								if (eventToListenForRecord != null)
								{
									if (!EventsToListenForWithBranches.ContainsKey(eventToListenForRecord))
										throw nodeReferenceNotFoundException;

									int index = nodesReferenced.IndexOf(eventToListenForWithBranch.NextNode);

									if (index == -1)
										throw new ArgumentException
										(
											nameof(nodesReferenced),
											nameof(nodesReferenced) +
											" does not contain a certain Node that is referenced by " +
											" the Listener Node from which this Record was created."
										);

									eventsToListenForWithBranches[eventToListenForRecord] = recordsToReference[index];
								}
							}
						}
					}
				}

				else if (Original.NextNode != null)
				{
					int index = nodesReferenced.IndexOf(Original.NextNode);

					if (index == -1)
						throw nodeReferenceNotFoundException;

					nextNode = recordsToReference[index];
				}
			}

			override protected void OnSetup()
			{
				if (nodesAndRunnersForGraphs[Graph].Item2==null)
					nodesAndRunnersForGraphs[Graph] =
					(
						null,
						new ListenerNodeRunner
						(
							new ReadOnlySet<ListenerNode.Record>(nodesAndRunnersForGraphs[Graph].Item1),
							RunningObject,
							NodeStarted,
							Stopped,
							EvaluateForNodes
						)
					);
			}

			override protected void OnTeardown()
			{
				if (nodesAndRunnersForGraphs[Graph].Item2!=null)
				{
					nodesAndRunnersForGraphs[Graph].Item2.Teardown();

					nodesAndRunnersForGraphs[Graph] = (null, null);
				}
			}

			override protected void OnEvaluate() =>
				nodesAndRunnersForGraphs[Graph].Item2.EvaluateNode(this);
		}
	}
}