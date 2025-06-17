using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

using CREATIVE.SandboxAssets.Events;
using CREATIVE.SandboxAssets.BehaviorTrees;

using Node		= CREATIVE.SandboxAssets.BehaviorTrees.Node;
using GraphView	= UnityEditor.Experimental.GraphView;

namespace CREATIVE.SandboxAssets.Editor.BehaviorTrees
{
	/**
		A child of GraphView.Node that serves only to point to the node view
		representing the root node of a BehaviorTree.
	*/
	public class EntryView : GraphView.Node
	{
		readonly public Port RootNodePort;

		readonly public bool ReadOnly;

		readonly SerializedProperty rootNodeProperty;

		readonly SerializedProperty entryViewPositionProperty;

		EntryView()
		{
			this.title = "Entry";

			RootNodePort =
				InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
			RootNodePort.portName = null;
			outputContainer.Add(RootNodePort);

			RefreshExpandedState();
		}

		/**
			Constructor for a read-only EntryView.
			
			@param position The position at which this EntryView should be
			displayed.
		*/
		public EntryView(Vector2 position) : this()
		{
			ReadOnly = true;

			this.rootNodeProperty = null;

			this.entryViewPositionProperty = null;

			style.left = position.x;
			style.top = position.y;

			RootNodePort.SetEnabled(false);
		}

		/**
			Constructor for an editable EntryView.

			@param rootNodeProperty The SerializedProperty of a BehaviorTree
			that contains the reference to its root node.

			@param entryViewPositionProperty The SerializedProperty of a
			BehaviorTree that contains the position at which the entry node
			should be displayed.
		*/
		public EntryView
		(
			SerializedProperty rootNodeProperty,
			SerializedProperty entryViewPositionProperty
		) : this()
		{
			if (rootNodeProperty == null)
				throw new ArgumentNullException(nameof(rootNodeProperty));

			if (entryViewPositionProperty == null)
				throw new ArgumentNullException(nameof(entryViewPositionProperty));

			if (entryViewPositionProperty.propertyType != SerializedPropertyType.Vector2)
				throw new ArgumentException
				(
					nameof(entryViewPositionProperty),
					nameof(entryViewPositionProperty) + " does not represent a Vector 2."
				);

			ReadOnly = false;

			this.rootNodeProperty = rootNodeProperty;

			this.entryViewPositionProperty = entryViewPositionProperty;

			Vector2 position = entryViewPositionProperty.vector2Value;

			style.left = position.x;
			style.top = position.y;
		}

		/**
			Overwrites the reference in the containing BehaviorTree to its root
			node.

			@param nodeView The node view representing the new root node.
		*/
		public void SetRootNode(INodeView<Node.IRecord<Node>, Node> nodeView)
		{
			if (ReadOnly)
				throw new InvalidOperationException("This Entry View is Read-Only.");

			if (nodeView == null)
				throw new ArgumentNullException(nameof(nodeView));

			nodeView.SetManagedReferenceToNodeProperty(rootNodeProperty);
		}

		/**
			Nulls out the reference in the containing BehaviorTree to its root
			node.
		*/
		public void ClearRootNode()
		{
			if (ReadOnly)
				throw new InvalidOperationException("This Entry View is Read-Only.");

			rootNodeProperty.SetManagedReferenceNull();

			rootNodeProperty.serializedObject.ApplyModifiedProperties();
		}

		/**
			An override of GraphView.Node.SetPosition that updates the position
			of this EntryNode in the relevant BehaviorTree.
		*/
		override public void SetPosition(Rect newPosition)
		{
			base.SetPosition(newPosition);

			entryViewPositionProperty.vector2Value = new Vector2(newPosition.xMin, newPosition.yMin);

			rootNodeProperty.serializedObject.ApplyModifiedProperties();
		}
	}
}