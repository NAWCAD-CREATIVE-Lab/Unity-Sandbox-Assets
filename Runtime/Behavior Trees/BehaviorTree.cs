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
#if UNITY_EDITOR
	[type: CreateAssetMenu(fileName = "Behavior Tree", menuName = "NAWCAD CREATIVE Lab/Sandbox Assets/Behavior Tree")]
#endif
	public class BehaviorTree : ScriptableObject
	{
		[SerializeReference]
		public List<Node> Nodes;

		[SerializeReference]
		public Node RootNode;

		public void CleanCloneFrom(BehaviorTree source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			
			if (source.Nodes == null)
				throw new InvalidOperationException(nameof(source.Nodes) + " is null.");
			
			if (source.RootNode == null)
				throw new InvalidOperationException(nameof(source.RootNode) + " is null.");
			
			if (!source.Nodes.Contains(source.RootNode))
				throw new InvalidOperationException(nameof(source.Nodes) + " does not contain the Root Node.");

			Nodes = new List<Node>(source.Nodes);

			Nodes.Remove(source.RootNode);

			Nodes.Insert(0, source.RootNode);
			
			Nodes = Node.CleanCloneList(Nodes);
			
			RootNode = Nodes[0];
		}

#if UNITY_EDITOR
		public Vector2 EntryNodePosition;
		
		[type: CustomEditor(typeof(BehaviorTree))]
		public class Inspector : Editor
		{
			public override VisualElement CreateInspectorGUI()
			{
				return new VisualElement();
			}
		}
		
		public static SerializedProperty AddListenerNode(SerializedObject serializedTree)
		{
			if (serializedTree == null)
				throw new ArgumentNullException(nameof(serializedTree));
			
			if (!(serializedTree.targetObject is BehaviorTree))
				throw new ArgumentException
					(nameof(serializedTree), nameof(serializedTree) + " does not represent a Behavior Tree.");
			
			SerializedProperty nodesProperty = serializedTree.FindProperty(nameof(Nodes));

			SerializedProperty newListenerProperty = nodesProperty.GetArrayElementAtIndex(nodesProperty.arraySize++);

			newListenerProperty.managedReferenceValue = new ListenerNode();

			serializedTree.ApplyModifiedProperties();

			return newListenerProperty;
		}

		public static SerializedProperty AddInvokerNode(SerializedObject serializedTree)
		{
			if (serializedTree == null)
				throw new ArgumentNullException(nameof(serializedTree));
			
			if (!(serializedTree.targetObject is BehaviorTree))
				throw new ArgumentException
					(nameof(serializedTree), nameof(serializedTree) + " does not represent a Behavior Tree.");
			
			SerializedProperty nodesProperty = serializedTree.FindProperty(nameof(Nodes));

			SerializedProperty newInvokerProperty = nodesProperty.GetArrayElementAtIndex(nodesProperty.arraySize++);

			newInvokerProperty.managedReferenceValue = new InvokerNode();

			serializedTree.ApplyModifiedProperties();

			return newInvokerProperty;
		}

		public static bool RemoveNode(SerializedProperty nodeProperty)
		{
			if (nodeProperty == null)
				throw new ArgumentNullException(nameof(nodeProperty));
			
			if (!(nodeProperty.IsManagedReference() && nodeProperty.ManagedReferenceIsOfType(typeof(Node))))
				throw new ArgumentException
					(nameof(nodeProperty), nameof(nodeProperty) + " does not represent a Behavior Tree Node.");

			if (!nodeProperty.DeleteCommand())
				return false;

			nodeProperty.serializedObject.ApplyModifiedProperties();

			return true;
		}

		public static bool RemoveNodes(IEnumerable<SerializedProperty> nodeProperties)
		{
			if (nodeProperties == null)
				throw new ArgumentNullException(nameof(nodeProperties));
			
			SerializedObject serializedTree = null;
			
			List<long> managedReferenceIDs = new List<long>();

			foreach (SerializedProperty nodeProperty in nodeProperties)
			{
				if (!(nodeProperty.IsManagedReference() && nodeProperty.ManagedReferenceIsOfType(typeof(Node))))
					throw new ArgumentException
					(
						nameof(nodeProperties),
						"A Serialized Property in " +
						nameof(nodeProperties) +
						" does not represent a Behavior Tree Node."
					);
				
				if (serializedTree == null)
					serializedTree = nodeProperty.serializedObject;
				
				else if (nodeProperty.serializedObject != serializedTree)
					throw new ArgumentException
					(
						nameof(nodeProperties),
						nameof(nodeProperties) +
						"Contains Serialized Properties representing Nodes from different Behavior Trees."
					);
				
				managedReferenceIDs.Add(nodeProperty.managedReferenceId);
			}

			if (managedReferenceIDs.Count > 0)
			{
				SerializedProperty nodesProperty = serializedTree.FindProperty(nameof(Nodes));

				for (int i=(nodesProperty.arraySize-1); i>=0; i--)
				{
					SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(i);

					if (managedReferenceIDs.Contains(nodeProperty.managedReferenceId))
						if (!nodeProperty.DeleteCommand())
							return false;
				}

				serializedTree.ApplyModifiedProperties();
			}

			return true;
		}
#endif
	}
}
