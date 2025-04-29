using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	abstract public class TreeGraphViewNode : UnityEditor.Experimental.GraphView.Node
	{
		public readonly Port PreviousNodePort;

		public readonly bool ReadOnly;
		
		protected readonly TreeGraphView treeGraphView;
		
		readonly SerializedProperty nodeProperty;
		public SerializedProperty NodeProperty
		{
			get
			{
				if (ReadOnly)
					throw new InvalidOperationException
						("This Tree Graph View Node is Read-Only and does not represent a Serialized Property.");
				
				return nodeProperty;
			}
		}

		readonly Node node;
		public Node Node
		{
			get
			{
				if (ReadOnly)
					return node;
				
				return nodeProperty.managedReferenceValue as Node;
			}
		}

		TreeGraphViewNode(TreeGraphView treeGraphView, string title)
		{
			if (treeGraphView == null)
				throw new ArgumentNullException(nameof(treeGraphView));
			
			if (String.IsNullOrWhiteSpace(title))
				throw new ArgumentNullException(nameof(title));
			
			this.treeGraphView = treeGraphView;

			this.title = title;
			
			PreviousNodePort =
				InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(Node));
			PreviousNodePort.portName = null;
			inputContainer.Add(PreviousNodePort);
		}

		protected TreeGraphViewNode(TreeGraphView treeGraphView, Node node, string title)
			: this(treeGraphView, title)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));
			
			ReadOnly = true;

			this.node = node;

			this.nodeProperty = null;

			style.left = node.Position.x;
			style.top = node.Position.y;

			PreviousNodePort.SetEnabled(false);
		}

		protected TreeGraphViewNode(TreeGraphView treeGraphView, SerializedProperty nodeProperty, string title)
			: this(treeGraphView, title)
		{
			if (nodeProperty == null)
				throw new ArgumentNullException(nameof(nodeProperty));

			if (nodeProperty.ManagedReferenceIsOfType(typeof(Node)))
				throw new ArgumentException
					(nameof(nodeProperty), nameof(nodeProperty) + " is not a Managed Reference of type Node.");
			
			ReadOnly = false;

			this.nodeProperty = nodeProperty;

			this.node = null;

			Vector2 position = NodeProperty.FindPropertyRelative(nameof(Node.Position)).vector2Value;

			style.left = position.x;
			style.top = position.y;
		}

		override public void SetPosition(Rect newPosition)
		{
			base.SetPosition(newPosition);

			if (ReadOnly)
				throw new InvalidOperationException
					("The position of a Tree Graph View Node cannot be set when the Node is Read-Only.");

			NodeProperty.FindPropertyRelative(nameof(Node.Position)).vector2Value =
				new Vector2(newPosition.xMin, newPosition.yMin);
			
			NodeProperty.serializedObject.ApplyModifiedProperties();
		}
	}
}