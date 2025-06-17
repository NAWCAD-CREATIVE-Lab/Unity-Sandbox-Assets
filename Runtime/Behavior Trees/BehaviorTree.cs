using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
#endif

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	/**
		A serialized collection of linked Node objects that can be converted 
		into a runnable Graph of behavior.

		Node types supported in a behavior Graph can be freely created or
		extended.
	*/
#if UNITY_EDITOR
	[type: CreateAssetMenu(fileName = "Behavior Tree", menuName = "NAWCAD CREATIVE Lab/Sandbox Assets/Behavior Tree")]
#endif
	public class BehaviorTree : ScriptableObject
	{
		/**
			An serialized List of linked Node objects.
			
			May be empty and may contain null references or duplicates.
		*/
		[field: SerializeReference]
		public List<Node> Nodes;

		/**
			The Node that should be evaluated first. May be null.
		*/
		[field: SerializeReference]
		public Node RootNode;

		/**
			The position where the node should be displayed in the Behavior
			Tree that points to the RootNode.

			Only relevant in the Unity Editor. 
		*/
		public Vector2 EntryNodePosition;

		/**
			Attempts to create a Graph object from this BehaviorTree.
			
			May throw exceptions if this BehaviorTree does not represent a
			valid Graph.

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
		public Graph CreateGraph
		(
			GameObject runningObject,
			Action<Node.IRecord<Node>> currentNodeChanged,
			Action stopped,
			out Action teardown
		) =>
			new Graph(Nodes, RootNode, runningObject, currentNodeChanged, stopped, out teardown);

#if UNITY_EDITOR
		
		/**
			A custom Unity inspector for BehaviorTree objects.

			Renders a blank pane, as Behavior Trees should be edited with the
			Behavior Tree Editor window.
		*/
		[type: CustomEditor(typeof(BehaviorTree))]
		public class Inspector : Editor
		{
			public override VisualElement CreateInspectorGUI() => new VisualElement();
		}
#endif
		}
}
