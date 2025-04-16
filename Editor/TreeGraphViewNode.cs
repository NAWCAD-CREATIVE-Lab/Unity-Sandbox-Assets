using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	abstract public class TreeGraphViewNode : UnityEditor.Experimental.GraphView.Node
	{
		public readonly TreeGraphView TreeGraphView;
		
		public readonly SerializedProperty NodeProperty;

		public readonly Port PreviousNodePort;

		protected TreeGraphViewNode(TreeGraphView treeGraphView, SerializedProperty nodeProperty, string title)
		{
			if (treeGraphView == null)
				throw new ArgumentNullException(nameof(treeGraphView));
			
			if (nodeProperty == null)
				throw new ArgumentNullException(nameof(nodeProperty));
			
			if (String.IsNullOrWhiteSpace(title))
				throw new ArgumentNullException(nameof(title));
			
			TreeGraphView = treeGraphView;

			NodeProperty = nodeProperty;
			
			this.title = title;

			PreviousNodePort =
				InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(Node));
			PreviousNodePort.portName = null;
			inputContainer.Add(PreviousNodePort);

			Vector2 position = NodeProperty.FindPropertyRelative(nameof(Node.Position)).vector2Value;

			style.left = position.x;
			style.top = position.y;
		}

		override public void SetPosition(Rect newPosition)
		{
			base.SetPosition(newPosition);

			NodeProperty.FindPropertyRelative(nameof(Node.Position)).vector2Value =
				new Vector2(newPosition.xMin, newPosition.yMin);
			
			NodeProperty.serializedObject.ApplyModifiedProperties();
		}
	}
}