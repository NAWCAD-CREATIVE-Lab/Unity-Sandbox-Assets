using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using CREATIVE.SandboxAssets.BehaviorTrees;

namespace CREATIVE.SandboxAssets.Editor.BehaviorTrees
{
	/**
		Utility methods for modifying serialized BehaviorTree objects.
	*/
	public class TreeSerialization
	{
		static Dictionary<SerializedObject, HashSet<SerializedProperty>> deletionQueues =
			new Dictionary<SerializedObject, HashSet<SerializedProperty>>();

		/**
			Adds a given Node to a given SerializedObject representing a
			BehaviorTree.
		*/
		public static SerializedProperty AddNode(SerializedObject serializedTree, Node newNode)
		{
			if (serializedTree == null)
				throw new ArgumentNullException(nameof(serializedTree));

			if (newNode == null)
				throw new ArgumentNullException(nameof(newNode));

			if (!(serializedTree.targetObject is BehaviorTree))
				throw new ArgumentException
					(nameof(serializedTree), nameof(serializedTree) + " does not represent a Behavior Tree.");

			SerializedProperty nodesProperty = serializedTree.FindProperty(nameof(BehaviorTree.Nodes));

			SerializedProperty newNodeProperty = nodesProperty.GetArrayElementAtIndex(nodesProperty.arraySize++);

			newNodeProperty.managedReferenceValue = newNode;

			serializedTree.ApplyModifiedProperties();

			return newNodeProperty;
		}

		/**
			Removes a given serialized Node from its serialized BehaviorTree.

			Cannot be used sequentially for multiple SerializedProperty objects,
			as SerializedProperty objects contain hard-coded array indices that
			will become invalid if earlier array items are removed.
			QueueNodeForRemoval should be used instead.
		*/
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

		/**
			Adds the given serialized Node to an internal static queue to be
			deleted from the containing serialized BehaviorTree at some point in
			the future.
		*/
		public static void QueueNodeForRemoval(SerializedProperty nodeProperty)
		{
			if (nodeProperty == null)
				throw new ArgumentNullException(nameof(nodeProperty));

			if (!(nodeProperty.IsManagedReference() && nodeProperty.ManagedReferenceIsOfType(typeof(Node))))
				throw new ArgumentException
					(nameof(nodeProperty), nameof(nodeProperty) + " does not represent a Behavior Tree Node.");

			if (!deletionQueues.ContainsKey(nodeProperty.serializedObject))
				deletionQueues.Add(nodeProperty.serializedObject, new HashSet<SerializedProperty>());

			deletionQueues[nodeProperty.serializedObject].Add(nodeProperty);
		}

		/**
			Deletes all serialized Nodes from the given serialized BehaviorTree
			that have been previously queued using QueueNodeForRemoval.
		*/
		public static bool RemoveQueuedNodes(SerializedObject serializedTree)
		{
			if (serializedTree == null)
				throw new ArgumentNullException(nameof(serializedTree));
			
			if (!(serializedTree.targetObject is BehaviorTree))
				throw new ArgumentException
					(nameof(serializedTree), nameof(serializedTree) + " does not represent a Behavior Tree.");

			if (deletionQueues.ContainsKey(serializedTree))
			{
				if (deletionQueues[serializedTree].Count > 0)
				{
					List<long> managedReferenceIDs = new List<long>();
					foreach (SerializedProperty nodeProperty in deletionQueues[serializedTree])
						managedReferenceIDs.Add(nodeProperty.managedReferenceId);

					SerializedProperty nodesProperty = serializedTree.FindProperty(nameof(BehaviorTree.Nodes));

					for (int i = (nodesProperty.arraySize - 1); i >= 0; i--)
					{
						SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(i);

						if (managedReferenceIDs.Contains(nodeProperty.managedReferenceId))
							if (!nodeProperty.DeleteCommand())
								return false;
					}

					serializedTree.ApplyModifiedProperties();
				}

				deletionQueues.Remove(serializedTree);
			}

			return true;
		}
	}
}