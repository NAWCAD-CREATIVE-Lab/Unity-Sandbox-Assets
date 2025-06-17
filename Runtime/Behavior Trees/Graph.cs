using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	/**
		A read-only collection of linked Node.IRecord objects that can be
		evaluated as a behavior tree.

		Implements IReadOnlySet<Node.IRecord<Node>> in order to iterate through
		all nodes, including the root node.

		Will not contain null.
	*/
	public class Graph : IReadOnlySet<Node.IRecord<Node>>
	{
		/**
			The first node in the Graph to evaluate.
		*/
		public readonly Node.IRecord<Node> RootNode;

		readonly IReadOnlySet<Node.IRecord<Node>> allNodes;

		readonly Action rootEvaluate;

		bool started = false;

		/**
			@param nodes The Node objects that will be used to create the Graph.
			May be null, empty, or contain nulls. May not include the root node.

			@param rootNode The first Node object that should be evaluated in
			the Graph.
			
			@param runningObject The GameObject that should be running the
			created Graph

			@param currentNodeChanged An Action that will be invoked immediately
			before a new node begins to be evaluated. Will not be invoked before
			the root node is evaluated.

			@param stopped An Action that will be invoked if a node
			completes evaluation and has no subsequent node to evaluate.

			@param teardown A reference that will be populated with a method
			that must be called after the created Graph is no longer being used,
			in order to perform necessary final configuration.
		*/
		public Graph
		(
			IEnumerable<Node> nodes,
			Node rootNode,
			GameObject runningObject,
			Action<Node.IRecord<Node>> currentNodeChanged,
			Action stopped,
			out Action teardown
		)
		{
			if (nodes == null)
				throw new ArgumentNullException(nameof(nodes));

			if (rootNode == null)
				throw new ArgumentNullException(nameof(rootNode));

			if (runningObject == null)
				throw new ArgumentNullException(nameof(runningObject));

			if (currentNodeChanged == null)
				throw new ArgumentNullException(nameof(currentNodeChanged));

			if (stopped == null)
				throw new ArgumentNullException(nameof(stopped));

			/*
				These three collections store the function pointers set by the
				node record constructors that won't be accessible after
				construction.

				The evaluate function pointers are seperated out, because they
				will be fed back into the setReferences function.

				The teardown function pointers aren't in a dictionary because
				they just need to be run, and not necessarily associated with
				their original node.
			*/

			Dictionary<Node.IRecord<Node>, (Node.SetRecordReferences, Action)> nodeRecordSetupInfo =
				new Dictionary<Node.IRecord<Node>, (Node.SetRecordReferences, Action)>();

			Dictionary<Node.IRecord<Node>, Action> evaluateForNodeRecords =
				new Dictionary<Node.IRecord<Node>, Action>();

			HashSet<Action> nodeRecordTeardowns = new HashSet<Action>();

			/*
				These two lists will coorespond exactly to each other,
				node to node record, and be fed into each setReferences function
				so the records can map the references in the nodes to the
				references that should be in the records.
			*/
			List<Node> nodeList = new List<Node>();

			List<Node.IRecord<Node>> nodeRecordList = new List<Node.IRecord<Node>>();

			/*
				Temporary variables for the function pointers that will be
				immediately loaded into the collections above
			*/
			Action nodeRecordSetup;
			Action nodeRecordTeardown;
			Node.SetRecordReferences nodeRecordSetReferences;

			// Create the root record and store all associated info
			this.RootNode = rootNode.CreateRecord
			(
				this,
				runningObject,
				currentNodeChanged,
				stopped,
				out nodeRecordSetReferences,
				out nodeRecordSetup,
				out rootEvaluate,
				out nodeRecordTeardown
			);

			nodeRecordSetupInfo.Add(this.RootNode, (nodeRecordSetReferences, nodeRecordSetup));

			evaluateForNodeRecords.Add(this.RootNode, rootEvaluate);

			nodeRecordTeardowns.Add(nodeRecordTeardown);

			nodeList.Add(rootNode);

			nodeRecordList.Add(this.RootNode);

			// Create all the non-root node records
			foreach (Node node in nodes)
			{
				if (node!=null && node!=rootNode && !nodeList.Contains(node))
				{
					Action nodeRecordEvaluate;

					Node.IRecord<Node> nodeRecord = node.CreateRecord
					(
						this,
						runningObject,
						currentNodeChanged,
						stopped,
						out nodeRecordSetReferences,
						out nodeRecordSetup,
						out nodeRecordEvaluate,
						out nodeRecordTeardown
					);

					nodeRecordSetupInfo.Add(nodeRecord, (nodeRecordSetReferences, nodeRecordSetup));

					evaluateForNodeRecords.Add(nodeRecord, nodeRecordEvaluate);

					nodeRecordTeardowns.Add(nodeRecordTeardown);

					nodeList.Add(node);

					nodeRecordList.Add(nodeRecord);
				}
			}

			// Store all created node records in the graph
			this.allNodes = new ReadOnlySet<Node.IRecord<Node>>
				(new HashSet<Node.IRecord<Node>>(nodeRecordSetupInfo.Keys));

			// Set references for all node records
			foreach (Node.IRecord<Node> nodeRecord in nodeRecordSetupInfo.Keys)
				nodeRecordSetupInfo[nodeRecord].Item1(nodeList, nodeRecordList, evaluateForNodeRecords);

			// Run setup for all node records
			foreach (Node.IRecord<Node> nodeRecord in nodeRecordSetupInfo.Keys)
				nodeRecordSetupInfo[nodeRecord].Item2();

			/*
				Glue all node record teardown functions into one to output from
				the constructor
			*/
			teardown = () =>
			{
				foreach (Action nodeRecordTeardownIterator in nodeRecordTeardowns)
					nodeRecordTeardownIterator();
			};
		}

		/*
			This constructor and the GenericizeSteps method are just used to
			convert a list of steps to a list of nodes, so the first constructor
			can be used.
		*/
		Graph
		(
			(IEnumerable<Node>, Node) nodeTuple,
			GameObject runningObject,
			Action<Node.IRecord<Node>> currentNodeChanged,
			Action stopped,
			out Action teardown
		) : this(nodeTuple.Item1, nodeTuple.Item2, runningObject, currentNodeChanged, stopped, out teardown)
		{ }

		static (IEnumerable<Node>, Node) GenericizeSteps(IReadOnlyList<Step> steps)
		{
			if (steps == null)
				throw new ArgumentNullException(nameof(steps));

			List<Node> nodes = new List<Node>();

			Node rootNode = null;

			foreach ((ListenerNode, InvokerNode) stepNodes in Step.CreateNodes(steps))
			{
				if (rootNode == null)
					rootNode = stepNodes.Item1;

				nodes.Add(stepNodes.Item1);

				nodes.Add(stepNodes.Item2);
			}

			return (nodes, rootNode);
		}

		/**
			@param steps An ordered list of Step objects that will be converted
			into a list of Node objects before being passed to the other
			constructor.
		*/
		public Graph
		(
			IReadOnlyList<Step> steps,
			GameObject runningObject,
			Action<Node.IRecord<Node>> currentNodeChanged,
			Action stopped,
			out Action teardown
		) : this(GenericizeSteps(steps), runningObject, currentNodeChanged, stopped, out teardown) { }

		public void Evaluate()
		{
			if (started)
				throw new InvalidOperationException("Evaluate can only be called once.");

			started = true;

			rootEvaluate();
		}

		public int Count => allNodes.Count;

		public IEnumerator<Node.IRecord<Node>> GetEnumerator() => allNodes.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		public bool IsSubsetOf(IEnumerable<Node.IRecord<Node>> other) => allNodes.IsSubsetOf(other);

		public bool IsSupersetOf(IEnumerable<Node.IRecord<Node>> other) => allNodes.IsSupersetOf(other);

		public bool IsProperSupersetOf(IEnumerable<Node.IRecord<Node>> other) => allNodes.IsProperSupersetOf(other);

		public bool IsProperSubsetOf(IEnumerable<Node.IRecord<Node>> other) => allNodes.IsProperSubsetOf(other);

		public bool Overlaps(IEnumerable<Node.IRecord<Node>> other) => allNodes.Overlaps(other);

		public bool SetEquals(IEnumerable<Node.IRecord<Node>> other) => allNodes.SetEquals(other);

		public bool Contains(Node.IRecord<Node> item) => allNodes.Contains(item);
	}
}