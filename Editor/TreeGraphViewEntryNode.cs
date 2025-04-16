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
	sealed public class TreeGraphViewEntryNode : UnityEditor.Experimental.GraphView.Node
	{
		public readonly Port RootNodePort;

		public readonly SerializedProperty RootNodeProperty;

		private SerializedProperty entryNodePositionProperty;

		public bool RootNodeIsSet { get { return !RootNodeProperty.ManagedReferenceIsNull(); } }

		public TreeGraphViewEntryNode
		(
			SerializedProperty rootNodeProperty,
			SerializedProperty entryNodePositionProperty
		)
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
			
			this.title = "Entry";

			RootNodeProperty = rootNodeProperty;

			this.entryNodePositionProperty = entryNodePositionProperty;

			RootNodePort =
				InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
			RootNodePort.portName = null;
			outputContainer.Add(RootNodePort);

			Vector2 position = entryNodePositionProperty.vector2Value;

			style.left = position.x;
			style.top = position.y;

			RefreshExpandedState();
		}

		override public void SetPosition(Rect newPosition)
		{
			base.SetPosition(newPosition);

			entryNodePositionProperty.vector2Value = new Vector2(newPosition.xMin, newPosition.yMin);

			RootNodeProperty.serializedObject.ApplyModifiedProperties();
		}
	}
}