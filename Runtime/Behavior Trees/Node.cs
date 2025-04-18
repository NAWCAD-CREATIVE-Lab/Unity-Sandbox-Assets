using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	[type: Serializable]
	public abstract class Node
	{
#if UNITY_EDITOR
		[field: HideInInspector]
		public Vector2 Position = Vector2.zero;
#endif

		public static List<Node> CleanCloneList(List<Node> nodes)
		{
			List<Node> clone = new List<Node>();

			List<Node> nullCheckedNodes = new List<Node>();

			if (nodes != null)
			{
				foreach (Node node in nodes)
				{
					if (node != null)
					{
						nullCheckedNodes.Add(node);
						
						if (node is InvokerNode)
							clone.Add((node as InvokerNode).CleanCloneWithoutNextNode);
						
						if (node is ListenerNode)
							clone.Add((node as ListenerNode).CleanCloneWithoutNextNodes);
					}
				}
			}

			for (int i=0; i<clone.Count; i++)
			{
				if (clone[i] is InvokerNode)
				{
					InvokerNode invokerNode = nullCheckedNodes[i] as InvokerNode;

					InvokerNode invokerNodeClone = clone[i] as InvokerNode;

					if (invokerNode.NextNode != null)
					{
						if (!nullCheckedNodes.Contains(invokerNode.NextNode))
							throw new ArgumentException
							(
								nameof(nodes),
								nameof(nodes) +
								" contains an Invoker Node that references another Node not in this list"
							);
						
						invokerNodeClone.NextNode = clone[nullCheckedNodes.IndexOf(invokerNode.NextNode)];
					}
				}
				
				if (clone[i] is ListenerNode)
				{
					ListenerNode listenerNode = nullCheckedNodes[i] as ListenerNode;

					ListenerNode listenerNodeClone = clone[i] as ListenerNode;

					if (listenerNode.NextNode != null)
					{
						if (!nullCheckedNodes.Contains(listenerNode.NextNode))
							throw new ArgumentException
							(
								nameof(nodes),
								nameof(nodes) +
								" contains an Listener Node that references another Node not in this list"
							);
						
						listenerNodeClone.NextNode = clone[nullCheckedNodes.IndexOf(listenerNode.NextNode)];
					}

					for (int j=0; j<listenerNodeClone.EventsToListenFor.Count; j++)
					{
						EventToListenForWithBranch eventToListenForWithBranchClone =
							listenerNodeClone.EventsToListenFor[j];
						
						EventToListenForWithBranch eventToListenForWithBranch = listenerNode.EventsToListenFor.Find
						(
							(eventToListenForWithBranchCandidate) =>
							(new EventToListenFor(eventToListenForWithBranchCandidate)).Equals
									(new EventToListenFor(eventToListenForWithBranchClone))
						);

						if (eventToListenForWithBranch.NextNode != null)
						{
							if (!nullCheckedNodes.Contains(eventToListenForWithBranch.NextNode))
								throw new ArgumentException
								(
									nameof(nodes),
									nameof(nodes) +
									" contains an Listener Node in that, in an Event branch, " + 
									"references another Node not in this list"
								);
							
							eventToListenForWithBranchClone.NextNode =
								clone[nullCheckedNodes.IndexOf(eventToListenForWithBranch.NextNode)];
						}
					}
				}
			}

			return clone;
		}
	}
}