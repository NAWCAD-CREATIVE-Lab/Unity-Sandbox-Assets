using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	public class TreeGraphViewEntryNode : UnityEditor.Experimental.GraphView.Node
	{
		readonly SerializedProperty entryNodePositionProperty;
		
		public readonly Port RootNodePort;

		public readonly bool ReadOnly;

		readonly SerializedProperty rootNodeProperty;
		public SerializedProperty RootNodeProperty
		{
			get
			{
				if (ReadOnly)
					throw new InvalidOperationException
					(
						"Something tried to get the Root Node Serialized Property " + 
						"of a Tree Graph View Entry Node that is Read Only."
					);
				
				return rootNodeProperty;
			}
		}

		public readonly Node rootNode;
		public Node RootNode
		{
			get
			{
				if (ReadOnly)
					return rootNode;
				
				return rootNodeProperty.managedReferenceValue as Node;
			}
		}

		public bool RootNodeIsSet
		{
			get
			{
				if (ReadOnly)
					return rootNode != null;
				
				return !RootNodeProperty.ManagedReferenceIsNull();
			}
		}

		TreeGraphViewEntryNode()
		{
			this.title = "Entry";

			RootNodePort =
				InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
			RootNodePort.portName = null;
			outputContainer.Add(RootNodePort);

			RefreshExpandedState();
		}

		public TreeGraphViewEntryNode
		(
			Node rootNode,
			Vector2 position
		) : this()
		{
			ReadOnly = true;
			
			this.rootNode = rootNode;

			this.rootNodeProperty = null;

			style.left = position.x;
			style.top = position.y;

			RootNodePort.SetEnabled(false);
		}

		public TreeGraphViewEntryNode
		(
			SerializedProperty rootNodeProperty,
			SerializedProperty entryNodePositionProperty
		) : this()
		{
			if (rootNodeProperty == null)
				throw new ArgumentNullException(nameof(rootNodeProperty));
			
			if (entryNodePositionProperty == null)
				throw new ArgumentNullException(nameof(entryNodePositionProperty));
			
			if (entryNodePositionProperty.propertyType != SerializedPropertyType.Vector2)
				throw new ArgumentException
				(
					nameof(entryNodePositionProperty),
					nameof(entryNodePositionProperty) + " does not represent a Vector 2."
				);
			
			ReadOnly = false;

			this.rootNode = null;

			this.rootNodeProperty = rootNodeProperty;

			this.entryNodePositionProperty = entryNodePositionProperty;

			Vector2 position = entryNodePositionProperty.vector2Value;

			style.left = position.x;
			style.top = position.y;
		}

		override public void SetPosition(Rect newPosition)
		{
			base.SetPosition(newPosition);

			entryNodePositionProperty.vector2Value = new Vector2(newPosition.xMin, newPosition.yMin);

			RootNodeProperty.serializedObject.ApplyModifiedProperties();
		}
	}
}