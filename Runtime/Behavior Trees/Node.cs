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
		An abstract parent class for a serialized node in a graph.
	*/
	[type: Serializable]
	abstract public class Node
	{
		/**
			The position where this node should be displayed.

			Only relevant in the Unity Editor. 
		*/
		[field: HideInInspector]
		[field: SerializeField]		protected Vector2 Position;

		/**
			The signature of a method used to set references in a record of a
			Node to other records of nodes in the same graph.

			@param nodesReferenced An ordered list of all serialized Node
			objects in a given graph.

			@param recordsToReference A list of all read-only IRecord<Node>
			objects in the graph. This list should coorespond exactly
			(in order and contents) to nodesReferenced.

			@param evaluateForNodeRecords A dictionary of execute actions that
			coorespond to all the Node record objects in the graph. The keys in
			this dictionary should be identical to the contents of
			recordsToReference.
		*/
		public delegate void SetRecordReferences
		(
			IReadOnlyList<Node> nodesReferenced,
			IReadOnlyList<IRecord<Node>> recordsToReference,
			IReadOnlyDictionary<IRecord<Node>, Action> evaluateForNodeRecords
		);

		/**
			A method implemented by child classes that creates a read-only
			record of this Node.
			
			The record returned from this method will not initially contain any
			references to other Node records in the graph.

			@param graph The graph that the Node record should become a part of.

			@param runningObject The GameObject that should be running the graph
			that the Node record should become a part of.

			@param nodeStarted An Action that should be invoked when a
			different Node record begins evaluating following the completion of
			the created Node record.

			@param stopped An Action that should be invoked if the created
			Node record finishes evaluating and there is no subsequent record to
			evaluate.

			@param setReferences A reference to populate with a method that
			must be called to set references in the created Node record to other
			Node records in the graph.

			@param setup A reference to populate with a method that must be
			called after setReferences in order to perform necessary 
			pre-evaluation configuration for the created Node record.

			@param evaluate A reference to populate with a method that can be
			called after setup to evaluate the created Node record.

			@param teardown A reference to populate with a method that must be
			called after all evaluation has finished in order to perform
			necessary final configuration for the created Node record.
		*/
		abstract public IRecord<Node> CreateRecord
		(
			IReadOnlySet<IRecord<Node>> graph,
			GameObject runningObject,
			Action<IRecord<Node>> nodeStarted,
			Action stopped,
			out SetRecordReferences setReferences,
			out Action setup,
			out Action evaluate,
			out Action teardown
		);

		/**
			A co-variant interface for read-only "records" of Node objects.

			An instance of an implementing class can be implicitly converted to
			an IRecord<Node> object without explicit casting.

			Classes that implement this interface should represent all necessary
			data of a type that inherits from Node, and not allow editing of
			that data after initial setup.
		*/
		public interface IRecord<out NodeType> where NodeType : Node
		{
			/**
				The position where this node should be displayed.

				Only relevant in the Unity Editor.
			*/
			public Vector2 Position { get; }
		}

		/**
			An abstract class providing a basic implementation of IRecord.

			Enforcess the rigid progression of the record through the stages of:
				- Construction
				- Setting references to other Node records
				- Setup
				- Evaluation
				- Teardown

			Performs generic error checking on all incoming data.

			Provides protected abstract methods for implementing classes to
			override.
		*/
		public abstract class Record<NodeType> : IRecord<NodeType> where NodeType : Node
		{
			/**
				The graph that contains this Node Record.

				Will not be null.
			*/
			protected readonly IReadOnlySet<IRecord<Node>> Graph;

			/**
				The original serialized object, inheriting from Node, from which
				this Record was created.

				Will not be null.
			*/
			protected readonly NodeType Original;

			/**
				The position at which this node should be
				displayed. Only relevant in the Unity Editor.
			*/
			public Vector2 Position { get { return position; } }
			readonly Vector2 position;

			/**
				The GameObject that will be running the graph that contains this
				Node Record.

				Will not be null.
			*/
			protected readonly GameObject RunningObject;

			/**
				An Action that must be invoked when a different Node record
				begins evaluating following the completion of this one.

				Will not be null.
			*/
			protected readonly Action<IRecord<Node>> NodeStarted;

			/**
				An Action that must be invoked if this Node Record finishes
				evaluating and there is no subsequent record to evaluate.

				Will not be null.
			*/
			protected readonly Action Stopped;

			/**
				A dictionary of Execute actions that coorespond to all the Node
				record objects in the graph that contains this one.

				Will not be null, contain a null key, or contain null values.
			*/
			protected IReadOnlyDictionary<IRecord<Node>, Action> EvaluateForNodes { get; private set; }

			bool referencesSet = false;

			bool setupPerformed = false;

			bool teardownPerformed = false;

			/**
				@param graph The graph that contains this Node Record.

				@param original The original serialized object, inheriting from
				Node, from which this Record was created.

				@param position The position at which this node should be
				displayed. Only relevant in the Unity Editor.

				@param runningObject The GameObject that will be running the
				graph that this Node record is a part of.

				@param nodeStartedRunning An Action that will be invoked when a
				different Node record begins evaluating following the completion
				of this one.

				@param stoppedRunning An Action that will be invoked if the
				this Node Record finishes evaluating and there is no subsequent
				record to evaluate

				@param setReferences A reference that will be populated with a
				method which must be called in order to set references in this
				Node Record to other Node records in the graph.

				@param setup A reference that will be populated with a method
				which must be called after setReferences in order to perform
				necessary pre-evaluation configuration for this Node Record.

				@param evaluate A reference that will be populated with a method
				that can be called after setup to evaluate this Node Record.

				@param teardown A reference that will be populated with a method
				that must be called after all evaluation has finished in order
				to perform necessary final configuration for this Node record.
			*/
			protected Record
			(
				IReadOnlySet<IRecord<Node>> graph,
				NodeType original,
				Vector2 position,
				GameObject runningObject,
				Action<IRecord<Node>> nodeStarted,
				Action stopped,
				out SetRecordReferences setReferences,
				out Action setup,
				out Action evaluate,
				out Action teardown
			)
			{
				if (graph == null)
					throw new ArgumentNullException(nameof(graph));

				if (original == null)
					throw new ArgumentNullException(nameof(original));

				if (runningObject == null)
					throw new ArgumentNullException(nameof(runningObject));

				if (nodeStarted == null)
					throw new ArgumentNullException(nameof(nodeStarted));

				if (stopped == null)
					throw new ArgumentNullException(nameof(stopped));

				Graph = graph;

				Original = original;

				this.position = position;

				RunningObject = runningObject;

				NodeStarted = nodeStarted;

				Stopped = stopped;

				setReferences = this.setReferences;

				setup = this.setup;

				evaluate = this.evaluate;

				teardown = this.teardown;
			}

			~Record() => teardown();

			void setReferences
			(
				IReadOnlyList<Node> nodesReferenced,
				IReadOnlyList<IRecord<Node>> recordsToReference,
				IReadOnlyDictionary<IRecord<Node>, Action> evaluateForNodes
			)
			{
				if (referencesSet)
					throw new InvalidOperationException("References can only be set once.");

				if (!Graph.Contains(this))
					throw new InvalidOperationException("This Node Record was not found in the containing graph.");

				if (nodesReferenced == null)
						throw new ArgumentNullException(nameof(nodesReferenced));

				if (recordsToReference == null)
					throw new ArgumentNullException(nameof(recordsToReference));
				
				if (evaluateForNodes == null)
					throw new ArgumentNullException(nameof(evaluateForNodes));
				
				foreach (Node node in nodesReferenced)
					if (node == null)
						throw new ArgumentException
							(nameof(nodesReferenced), nameof(nodesReferenced) + " contains a null value.");

				foreach (IRecord<Node> nodeRecord in recordsToReference)
				{
					if (nodeRecord == null)
						throw new ArgumentException
							(nameof(recordsToReference), nameof(recordsToReference) + " contains a null value.");
					
					else if (!Graph.Contains(nodeRecord))
						throw new ArgumentException
						(
							nameof(recordsToReference),
							nameof(recordsToReference) + " contains a Node record not found in the containing graph."
						);
				}

				foreach (IRecord<Node> nodeRecord in evaluateForNodes.Keys)
				{
					if (nodeRecord == null)
						throw new ArgumentException
							(nameof(evaluateForNodes), nameof(evaluateForNodes) + " contains a null key.");

					else if (!Graph.Contains(nodeRecord))
						throw new ArgumentException
						(
							nameof(evaluateForNodes),
							nameof(evaluateForNodes) + " contains a Node record not found in the containing graph."
						);
				}
				
				foreach (Action evaluate in evaluateForNodes.Values)
					if (evaluate == null)
						throw new ArgumentException
							(nameof(evaluateForNodes), nameof(evaluateForNodes) + " contains a null value.");

				if (nodesReferenced.Count != Graph.Count)
					throw new ArgumentException
					(
						nameof(nodesReferenced),
						nameof(nodesReferenced) + " contains a different number of nodes than the containing graph."
					);
				
				if (recordsToReference.Count != Graph.Count)
					throw new ArgumentException
					(
						nameof(recordsToReference),
						nameof(recordsToReference) + " contains a different number of nodes than the containing graph."
					);
				
				if (evaluateForNodes.Count != Graph.Count)
					throw new ArgumentException
					(
						nameof(evaluateForNodes),
						nameof(evaluateForNodes) + " contains a different number of nodes than the containing graph."
					);

				/*
					Duplicate dictionary to eliminate risk of something else
					editing it
				*/
				Dictionary<IRecord<Node>, Action> newEvaluateForNodes = new Dictionary<IRecord<Node>, Action>();
				foreach (IRecord<Node> nodeRecord in evaluateForNodes.Keys)
					newEvaluateForNodes.Add(nodeRecord, evaluateForNodes[nodeRecord]);
				
				EvaluateForNodes = new ReadOnlyDictionary<IRecord<Node>, Action>(newEvaluateForNodes);

				OnSetReferences(nodesReferenced, recordsToReference);

				referencesSet = true;
			}

			/**
				A callback in the child class where references to other
				IRecord<Node> objects in the graph should be set.

				Guaranteed to be called only once in the life of the Record.
				
				Guaranteed to be called before OnSetup, OnEvaluate, or
				OnTeardown can be called.

				Guaranteed to be called only in a Node Record that is included
				in its containing graph.

				@param nodesReferenced A list of all serialized Node objects in
				the BehaviorTree. Guaranteed not be null, empty, contain null
				items, or contain a different number of nodes than the
				containing graph. 

				@param recordsToReference A list of all read-only IRecord<Node>
				objects in the graph. This list should coorespond exactly (in
				contents as well as order) to the objects in nodesReferenced.
				Guaranteed to contain exactly the same items as the Node Record
				object's containing graph.
			*/
			abstract protected void OnSetReferences
				(IReadOnlyList<Node> nodesReferenced, IReadOnlyList<IRecord<Node>> recordsToReference);

			void setup()
			{
				if (!referencesSet)
					throw new InvalidOperationException("References must be set before setup.");

				if (setupPerformed)
					throw new InvalidOperationException("Setup can only be run once.");

				if (teardownPerformed)
					throw new InvalidOperationException("Setup cannot be run after teardown.");

				OnSetup();

				setupPerformed = true;
			}

			/**
				Should perform any registration that must occur before the
				node can be evaluated.

				Guaranteed to run only after OnSetReferences has been called,
				and only once in the life of the Record.
			*/
			abstract protected void OnSetup();

			void teardown()
			{
				if (!setupPerformed)
					throw new InvalidOperationException("Teardown cannot be run before Setup.");

				if (teardownPerformed)
					throw new InvalidOperationException("Teardown can only be run once.");

				OnTeardown();

				teardownPerformed = true;
			}

			/**
				Should perform any de-registration that must occur after the
				node no longer needs to be evaluated.

				Guaranteed to run only after OnSetup has been called, and only
				once in the life of the Record.
			*/
			abstract protected void OnTeardown();

			void evaluate()
			{
				if (!setupPerformed)
					throw new InvalidOperationException("Node cannot be evaluated before Setup.");
				
				if (teardownPerformed)
					throw new InvalidOperationException("Node cannot be evaluated after Teardown.");

				else
					OnEvaluate();
			}

			/**
				Should begin evaluating the logic in this node.

				Guaranteed to run only after OnSetup has been called, and never
				after OnTeardown has been called.
			*/
			abstract protected void OnEvaluate();
		}
	}
}