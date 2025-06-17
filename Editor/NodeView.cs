using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

using CREATIVE.SandboxAssets.BehaviorTrees;

using Node			= CREATIVE.SandboxAssets.BehaviorTrees.Node;
using GraphView		= UnityEditor.Experimental.GraphView;

namespace CREATIVE.SandboxAssets.Editor.BehaviorTrees
{
	/**
		A co-variant interface for visual representations of Node objects.

		These visual representations would be connected by GraphView Port
		objects, likely contained in GraphView Node objects.

		Assumes a graph structure where all nodes have exactly
		one input (possibly connecting from multiple nodes), but multiple
		possible outputs (each possibly connecting to only one node).

		An instance of an implementing class can be implicitly converted to an
		INodeView<Node.IRecord<Node>, Node> object without explicit casting.

		Many of these methods are valid only for instances of implementing
		classes that represent Node objects editable through SerializedProperty
		objects.

		Some of these methods are valid only for instances of implementing
		classes that represent read-only Node.IRecord objects.
	*/
	public interface INodeView<out RecordType, out NodeType>
		where RecordType : Node.IRecord<NodeType>
		where NodeType : Node
	{
		/**
			Should return a Port that connects as an input from other INodeView
			objects. These other objects would represent nodes that evaluate the
			node underlying this node view upon their completion.
		*/
		public Port PreviousNodePort { get; }

		/**
			Should return true if this node view is read-only.

			Should return false if this node view is editable.
		*/
		public bool ReadOnly { get; }

		/**
			Should return true if this node view represents the given node
			record.

			Only valid if this node view is read-only.
		*/
		public bool RepresentsNode(Node.IRecord<Node> node);

		/**
			Should return true if this node view represents the given serialized
			node.

			Only valid if this node view is editable.
		*/
		public bool RepresentsNodeProperty(SerializedProperty nodeProperty);

		/**
			Would be called after the position of a node view is shifted in the
			graph view
			
			Should update the serialized position field in the underlying node.

			Only valid if this node view is editable.
		*/
		public void SaveVisualElementPosition();

		/**
			Should overwrite both the position of the node view, as well as the
			serialized position in the underlying node.

			Only valid if this node view is editable.
		*/
		public void SetPosition(Vector2 position);

		/**
			Should overwrite the given serialized reference to a node
			with the serialized node represented by this node view.

			Only valid if this node view is editable.
		*/
		public void SetManagedReferenceToNodeProperty(SerializedProperty nodeReferenceProperty);

		/**
			Should delete the serialized node represented by this node view from
			the containing serialized BehaviorTree.

			Can assume that this node view will not be used after being called.

			Would not be used consecutively for multiple node views
			in the same graph view. QueueNodePropertyForDeletion would be used
			instead.

			Only valid if this node view is editable.
		*/
		public void DeleteNodeProperty();

		/**
			Should add the serialized node represented by this node view to an
			internal static queue to be deleted from the containing serialized
			BehaviorTree at some point in the future.

			This is necessary for batch deletions of multiple node records as
			references to items in serialized lists are denoted by their index
			and may no longer be valid if earlier items have been deleted.

			Only valid if this node view is editable.
		*/
		public void QueueNodePropertyForDeletion();

		/**
			Should delete the serialized node represented by this node view
			(along with all others that have been previously queued using
			QueueNodePropertyForDeletion) from the containing serialized
			BehaviorTree.

			Can assume that this node view will not be used after being called.

			Only valid if this node view is editable.
		*/
		public void DeleteQueuedProperties();

		/**
			Should return any ports that this node view uses as an output to
			connect to the given node view.

			Should return an empty Enumerable if this node view does not output
			to the given node view.

			Only valid if the given node view has the same read-only status as
			this node view.
		*/
		public IEnumerable<Port> GetConnectedOutputPorts(INodeView<Node.IRecord<Node>, Node> nextNode);

		/**
			Should overwrite the serialized Node reference that is represented
			by the given port with the serialized Node that is represented by
			the given node view.

			Only valid if this node view is editable.
		*/
		public void SetConnectionForOutputPort(Port outputPort, INodeView<Node.IRecord<Node>, Node> nextNode);

		/**
			Should null out the serialized Node reference that is represented by
			the given port.

			Only valid if this node view is editable.
		*/
		public void ClearConnectionForOutputPort(Port outputPort);
	}

	/**
		An interface for a factory class that creates different types of
		INodeView objects.
	*/
	public interface INodeViewFactory
	{
		/**
			Should return a default string that can optionally be used as a
			title for created node views, if none is provided by the underlying
			node. 
		*/
		public string DefaultTitle { get; }

		/**
			Should return an INodeView object that cannot be edited, and that
			represents a given Node record using a given GraphView.Node.

			@param graphViewNode The GraphView.Node that should represent the 
			Node record.

			@param node The Node record that should be represented.

			@param title The title that should be displayed on the
			GraphView.Node.

			@param active Whether or not the given node is currently being
			evaluated.
		*/
		public INodeView<Node.IRecord<Node>, Node> CreateReadOnly
		(
			GraphView.Node graphViewNode,
			Node.IRecord<Node> node,
			string title,
			bool active
		);

		/**
			Should return an INodeView object that can be edited from the
			GraphView, and that represents a given serialized Node using a given
			GraphView.Node.

			@param graphViewNode The GraphView.Node that should represent the 
			serialized Node.

			@param nodeProperty The serialized Node that should be represented.

			@param title The title that should be displayed on the
			GraphView.Node.

			@param clearPort A function that can be called to disconnect the
			edge (if one exists) from a given output Port on the created node
			view.

			@param drawEdges A function that can be called to connect the
			necessary edges from all output ports on the created node view.
		*/
		public INodeView<Node.IRecord<Node>, Node> CreateEditable
		(
			GraphView.Node graphViewNode,
			SerializedProperty nodeProperty,
			string title,
			Action<Port> clearPort,
			Action<GraphView.Node> drawEdges
		);

		/**
			Should add a new Node to the given serialized BehaviorTree and
			return an INodeView object that is editable from the GraphView, and
			that represents the given Node.

			@param graphViewNode The GraphView.Node that should represent the 
			serialized Node.

			@param behaviorTree The serialized BehaviorTree that should contain
			the new Node.

			@param title The title that should be displayed on the
			GraphView.Node.

			@param clearPort A function that can be called to disconnect the
			edge (if one exists) from a given output Port on the created node
			view.

			@param drawEdges A function that can be called to connect the
			necessary edges from all output ports on the created node view.
		*/
		public INodeView<Node.IRecord<Node>, Node> CreateNew
		(
			GraphView.Node graphViewNode,
			SerializedObject behaviorTree,
			string title,
			Action<Port> clearPort,
			Action<GraphView.Node> drawEdges
		);
	}

	/**
		A basic, incomplete implementation of the INodeView interface that
		handles most of the general data processing for a visual representation
		of a serialized Node or Node record.
	*/
	public abstract class NodeView<RecordType, NodeType> : INodeView<RecordType, NodeType>
		where RecordType : Node.IRecord<NodeType>
		where NodeType : Node
	{
		/**
			A generic exception that can be thrown when this NodeView is
			read-only and functions are called to edit an underlying serialized
			Node. 
		*/
		protected readonly InvalidOperationException ReadOnlyException =
			new InvalidOperationException("This Node View is read-only, and does not represent a serialized Node.");

		/**
			A generic exception that can be thrown when this NodeView is
			editable and functions are called to access an underlying Node
			record.
		*/
		protected readonly InvalidOperationException EditableException =
			new InvalidOperationException("This Node View is Editable and does not represent a Node Record.");

		/**
			The GraphView.Node that represents the backing serialized Node or
			Node record.
		*/
		protected readonly GraphView.Node GraphViewNode;

		/**
			The Port that connects as an input from other INodeView objects.
		*/
		public Port PreviousNodePort { get => previousNodePort; }
		readonly Port previousNodePort;

		/**
			True if this NodeView is read-only.

			False if this NodeView is editable.
		*/
		readonly bool readOnly;
		public bool ReadOnly { get => readOnly; }

		/**
			The serialzied Node backing this NodeView.

			Throws an exception if accessed when ReadOnly is true.
		*/
		protected SerializedProperty NodeProperty
		{
			get
			{
				if (ReadOnly)
					throw ReadOnlyException;

				return nodeProperty;
			}
		}
		readonly SerializedProperty nodeProperty;

		/**
			The Node record backing this NodeView.

			Throws an exception if accessed when ReadOnly is false.
		*/
		protected RecordType Node
		{
			get
			{
				if (!ReadOnly)
					throw EditableException;

				return node;
			}
		}
		readonly RecordType node;

		/**
			A function that can be called to disconnect the edge (if one exists)
			from a given output Port on this NodeView.
		*/
		readonly protected Action<Port> ClearPort;

		/**
			A function that can be called to connect the necessary edges from
			all output ports on this node view.
		*/
		protected void DrawEdges() => drawEdges(GraphViewNode);
		readonly Action<GraphView.Node> drawEdges;

		NodeView
		(
			GraphView.Node graphViewNode,
			bool readOnly,
			string title,
			Action<Port> clearPort,
			Action<GraphView.Node> drawEdges,
			bool active
		)
		{
			if (graphViewNode == null)
				throw new ArgumentNullException(nameof(graphViewNode));

			if (string.IsNullOrWhiteSpace(title))
				throw new ArgumentNullException(nameof(title));

			this.readOnly = readOnly;

			if (!ReadOnly)
			{
				if (clearPort == null)
					throw new ArgumentNullException(nameof(clearPort));

				if (drawEdges == null)
					throw new ArgumentNullException(nameof(drawEdges));
			}

			GraphViewNode = graphViewNode;

			ClearPort = clearPort;

			this.drawEdges = drawEdges;

			GraphViewNode.title = (readOnly && active) ? title + " (Active)" : title;

			this.previousNodePort =
				GraphViewNode.InstantiatePort
					(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(Node));
			PreviousNodePort.portName = null;
			GraphViewNode.inputContainer.Add(PreviousNodePort);
		}

		/**
			The constructor for a read-only NodeView, representing a Node
			record.

			@param graphViewNode The GraphView.Node that will represent the 
			Node record.

			@param node The Node record that will be represented.

			@param title The title that will be displayed on the
			GraphView.Node.

			@param active Whether or not this NodeView is currently being
			evaluated.
		*/
		protected NodeView
		(
			GraphView.Node graphViewNode,
			RecordType node,
			string title,
			bool active
		)
			: this(graphViewNode, true, title, null, null, active)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));

			this.node = node;

			this.nodeProperty = null;

			GraphViewNode.style.left = node.Position.x;
			GraphViewNode.style.top = node.Position.y;

			PreviousNodePort.SetEnabled(false);
		}

		/**
			The constructor for an editable NodeView, representing a serialized
			Node.

			@param graphViewNode The GraphView.Node that will represent the 
			serialized Node.

			@param nodeProperty The serialized Node that will be represented.

			@param title The title that will be displayed on the
			GraphView.Node.

			@param clearPort A function that can be called to disconnect the
			edge (if one exists) from a given output Port on the created node
			view.

			@param drawEdges A function that can be called to connect the
			necessary edges from all output ports on the created node view.
		*/
		protected NodeView
		(
			GraphView.Node graphViewNode,
			SerializedProperty nodeProperty,
			string title,
			Action<Port> clearPort,
			Action<GraphView.Node> drawEdges
		)
			: this(graphViewNode, false, title, clearPort, drawEdges, false)
		{
			if (nodeProperty == null)
				throw new ArgumentNullException(nameof(nodeProperty));

			if (!nodeProperty.ManagedReferenceIsOfType(typeof(Node)))
				throw new ArgumentException
					(nameof(nodeProperty), nameof(nodeProperty) + " is not a Managed Reference of type Node.");

			this.nodeProperty = nodeProperty;

			Vector2 position = NodeProperty.FindPropertyRelative(nameof(Node.Position)).vector2Value;

			GraphViewNode.style.left = position.x;
			GraphViewNode.style.top = position.y;
		}

		/**
			Returns true if this NodeView represents the given node record.

			Throws an exception if this NodeView is editable.
		*/
		public bool RepresentsNode(Node.IRecord<Node> node)
		{
			if (!ReadOnly)
				throw EditableException;

			if (node == null)
				throw new ArgumentNullException(nameof(node));

			return System.Object.ReferenceEquals(this.node, node);
		}

		/**
			Returns true if this NodeView represents the given serialized node.

			Throws an exception if this NodeView is read-only.
		*/
		public bool RepresentsNodeProperty(SerializedProperty nodeProperty)
		{
			if (ReadOnly)
				throw ReadOnlyException;

			if (nodeProperty == null)
				throw new ArgumentNullException(nameof(nodeProperty));

			return NodeProperty.ManagedReferenceEquals(nodeProperty);
		}

		/**
			Overwrites the given serialized reference to a node with the
			serialized node represented by this NodeView.

			Throws an exception if this NodeView is read-only.
		*/
		public void SetManagedReferenceToNodeProperty(SerializedProperty nodeReferenceProperty)
		{
			if (ReadOnly)
				throw ReadOnlyException;

			if (nodeReferenceProperty == null)
				throw new ArgumentNullException(nameof(nodeReferenceProperty));

			if (!nodeReferenceProperty.IsManagedReference())
				throw new ArgumentException
				(
					nameof(nodeReferenceProperty),
					nameof(nodeReferenceProperty) + " does not reporesent a Managed Reference."
				);

			if (nodeReferenceProperty.serializedObject != NodeProperty.serializedObject)
				throw new ArgumentException
				(
					nameof(nodeReferenceProperty),
					nameof(nodeReferenceProperty) + " does not belong to the same SerializedObject" +
					" as the Property represented by this Node View."
				);

			nodeReferenceProperty.SetManagedReference(NodeProperty);

			NodeProperty.serializedObject.ApplyModifiedProperties();
		}

		/**
			Deletes the serialized node represented by this NodeView from 
			containing serialized BehaviorTree.

			Assumes that this node view will not be used after being called.

			Must not be used consecutively for multiple NodeView obejcts in the
			same GraphView. QueueNodePropertyForDeletion should be used instead.

			Throws an exception if this NodeView is read-only.
		*/
		public void DeleteNodeProperty()
		{
			if (ReadOnly)
				throw ReadOnlyException;

			TreeSerialization.RemoveNode(NodeProperty);
		}

		/**
			Adds the serialized Node represented by this NodeView to an
			internal static queue to be deleted from the containing serialized
			BehaviorTree at some point in the future.

			Throws an exception if this NodeView is read-only.
		*/
		public void QueueNodePropertyForDeletion()
		{
			if (ReadOnly)
				throw ReadOnlyException;

			TreeSerialization.QueueNodeForRemoval(NodeProperty);
		}

		/**
			Deletes the serialized node represented by this NodeView, along with
			all others that have been previously queued using
			QueueNodePropertyForDeletion, from the containing serialized
			BehaviorTree.

			Assumes that this node view will not be used after being called.

			Throws an exception if this NodeView is read-only.
		*/
		public void DeleteQueuedProperties()
		{
			if (ReadOnly)
				throw ReadOnlyException;

			QueueNodePropertyForDeletion();

			TreeSerialization.RemoveQueuedNodes(NodeProperty.serializedObject);
		}

		abstract public IEnumerable<Port> GetConnectedOutputPorts(INodeView<Node.IRecord<Node>, Node> nextNode);

		abstract public void SetConnectionForOutputPort(Port outputPort, INodeView<Node.IRecord<Node>, Node> nextNode);

		abstract public void ClearConnectionForOutputPort(Port outputPort);

		/**
			Overwrites both the position of the NodeView, as well as the
			serialized position in the underlying node.

			Throws an exception if this NodeView is read-only.
		*/
		public void SetPosition(Vector2 position)
		{
			GraphViewNode.style.left = position.x;
			GraphViewNode.style.top = position.y;

			SaveVisualElementPosition();
		}

		/**
			Should be called after the position of a node view is shifted in the
			GraphView
			
			Updates the serialized position field in the underlying Node.

			Throws an exception if this NodeView is read-only.
		*/
		public void SaveVisualElementPosition()
		{
			if (ReadOnly)
				throw ReadOnlyException;

			NodeProperty.FindPropertyRelative(nameof(Node.Position)).vector2Value =
				new Vector2(GraphViewNode.style.left.value.value, GraphViewNode.style.top.value.value);

			NodeProperty.serializedObject.ApplyModifiedProperties();
		}
	}
}