using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Serialization;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	[UxmlElement]
	public partial class TreeGraphView : GraphView
	{
		public const string UssGuid = "1be56c8321208c245be26dc7f4beb315";
		
		SerializedObject currentSerializedTree = null;

		TreeGraphViewEntryNode currentEntryNode = null;

		Vector2 localMousePosition = Vector2.zero;
		
		public TreeGraphView()
		{
			Insert(0, new GridBackground());

			this.AddManipulator(new ContentZoomer());
			this.AddManipulator(new ContentDragger());
			this.AddManipulator(new SelectionDragger());
			this.AddManipulator(new RectangleSelector());
			
			styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(UssGuid)));

			graphViewChanged += OnGraphViewChanged;

			RegisterCallback<MouseDownEvent>
			(
				mouseDownEvent => localMousePosition =
					(
						mouseDownEvent.localMousePosition -
						new Vector2(viewTransform.position.x, viewTransform.position.y)
					) / scale
			);
		}

		public void PopulateView(SerializedObject serializedTree)
		{
			if (serializedTree == null)
				throw new ArgumentNullException(nameof(serializedTree));
			
			if (!(serializedTree.targetObject is BehaviorTree))
				throw new ArgumentException
					(nameof(serializedTree), nameof(serializedTree) + " does not represent a Behavior Tree.");
			
			graphViewChanged -= OnGraphViewChanged;
			DeleteElements(graphElements);
			graphViewChanged += OnGraphViewChanged;

			currentSerializedTree = null;
			currentEntryNode = null;

			SerializedProperty nodesProperty = serializedTree.FindProperty(nameof(BehaviorTree.Nodes));
			for (int i=0; i<nodesProperty.arraySize; i++)
			{
				SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(i);

				if (nodeProperty!=null && !nodeProperty.ManagedReferenceIsNull())
				{
					if (nodeProperty.ManagedReferenceIsOfType(typeof(ListenerNode)))
						AddElement(new TreeGraphViewListenerNode(this, nodeProperty));
					
					else if (nodeProperty.ManagedReferenceIsOfType(typeof(InvokerNode)))
						AddElement(new TreeGraphViewInvokerNode(this, nodeProperty));
				}
			}

			currentEntryNode = new TreeGraphViewEntryNode
			(
				serializedTree.FindProperty(nameof(BehaviorTree.RootNode)),
				serializedTree.FindProperty(nameof(BehaviorTree.EntryNodePosition))
			);

			AddElement(currentEntryNode);

			currentSerializedTree = serializedTree;
			
			ReDrawEdges();
		}

		public void ReDrawEdges()
		{
			if (currentSerializedTree != null)
			{
				graphViewChanged -= OnGraphViewChanged;
				
				foreach (Edge edge in edges)
				{
					edge.input.Disconnect(edge);
					edge.output.Disconnect(edge);
				}
				
				DeleteElements(edges);

				foreach (UnityEditor.Experimental.GraphView.Node node in nodes)
				{
					if (node is TreeGraphViewNode)
					{
						TreeGraphViewNode treeNodeView = node as TreeGraphViewNode;

						if (treeNodeView.NodeProperty.ManagedReferenceEquals(currentEntryNode.RootNodeProperty))
							AddElement(currentEntryNode.RootNodePort.ConnectTo(treeNodeView.PreviousNodePort));
					
						if (treeNodeView is TreeGraphViewInvokerNode)
						{
							TreeGraphViewInvokerNode invokerNodeView = treeNodeView as TreeGraphViewInvokerNode;

							SerializedProperty nextNodeReferenceProperty =
								invokerNodeView.NodeProperty.FindPropertyRelative(nameof(InvokerNode.NextNode));

							if (!nextNodeReferenceProperty.ManagedReferenceIsNull())
							{
								TreeGraphViewNode nextTreeNodeView = FindNode(nextNodeReferenceProperty);

								if (nextTreeNodeView != null)
									AddElement
										(invokerNodeView.NextNodePort.ConnectTo(nextTreeNodeView.PreviousNodePort));
							}
						}

						else if (treeNodeView is TreeGraphViewListenerNode)
						{
							TreeGraphViewListenerNode listenerNodeView = treeNodeView as TreeGraphViewListenerNode;

							if
							(
								listenerNodeView
									.NodeProperty
									.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion))
									.boolValue
							)
							{
								SerializedProperty eventsToListenForProperty =
									listenerNodeView.NodeProperty.FindPropertyRelative
										(nameof(ListenerNode.EventsToListenFor));
								
								for (int i=0; i<eventsToListenForProperty.arraySize; i++)
								{
									SerializedProperty eventToListenForProperty =
										eventsToListenForProperty.GetArrayElementAtIndex(i);
									
									SerializedProperty nextNodeReferenceProperty =
										eventToListenForProperty
										.FindPropertyRelative(nameof(EventToListenForWithBranch.NextNode));
									
									if (!nextNodeReferenceProperty.ManagedReferenceIsNull())
									{
										TreeGraphViewNode nextTreeNodeView = FindNode(nextNodeReferenceProperty);

										if (nextTreeNodeView != null)
											AddElement
											(
												listenerNodeView.GetPort(eventToListenForProperty).ConnectTo
													(nextTreeNodeView.PreviousNodePort)
											);
									}
								}
							}

							else
							{
								SerializedProperty nextNodeReferenceProperty =
									listenerNodeView
									.NodeProperty
									.FindPropertyRelative(nameof(ListenerNode.NextNode));
								
								if (!nextNodeReferenceProperty.ManagedReferenceIsNull())
								{
									TreeGraphViewNode nextTreeNodeView = FindNode(nextNodeReferenceProperty);

									if (nextTreeNodeView != null)
										AddElement
										(
											listenerNodeView.NextNodePort.ConnectTo(nextTreeNodeView.PreviousNodePort)
										);
								}
							}
						}
					}
				}

				graphViewChanged += OnGraphViewChanged;
			}
		}

		TreeGraphViewNode FindNode(SerializedProperty serializedProperty)
		{
			foreach (UnityEditor.Experimental.GraphView.Node graphViewNode in nodes)
			{
				if (graphViewNode is TreeGraphViewNode)
				{
					TreeGraphViewNode treeGraphViewNode = graphViewNode as TreeGraphViewNode;
					
					if (serializedProperty.ManagedReferenceEquals(treeGraphViewNode.NodeProperty))
						return treeGraphViewNode;
				}
			}

			return null;
		}

		override public void BuildContextualMenu(ContextualMenuPopulateEvent contextualMenuPopulateEvent)
		{
			if (currentSerializedTree != null)
			{
				contextualMenuPopulateEvent.menu.AppendAction
				(
					"Add new Listener",
					(dropdownMenuAction) =>
					{
						TreeGraphViewListenerNode listenerNode =
							new TreeGraphViewListenerNode(this, BehaviorTree.AddListenerNode(currentSerializedTree));
						
						AddElement(listenerNode);

						Rect position = listenerNode.GetPosition();

						position.position = localMousePosition;
						
						listenerNode.SetPosition(position);
					}
				);

				contextualMenuPopulateEvent.menu.AppendAction
				(
					"Add new Invoker",
					(dropdownMenuAction) =>
					{
						TreeGraphViewInvokerNode invokerNode =
							new TreeGraphViewInvokerNode(this, BehaviorTree.AddInvokerNode(currentSerializedTree));
						
						AddElement(invokerNode);

						Rect position = invokerNode.GetPosition();

						position.position = localMousePosition;
						
						invokerNode.SetPosition(position);
					}
				);
			}
		}

		GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
		{
			if (currentSerializedTree!=null)
			{
				if (graphViewChange.elementsToRemove!=null)
				{
					List<SerializedProperty> nodesToRemove = new List<SerializedProperty>();
					
					foreach (GraphElement graphElement in graphViewChange.elementsToRemove)
					{
						if (graphElement is TreeGraphViewNode)
							nodesToRemove.Add((graphElement as TreeGraphViewNode).NodeProperty);
						
						else if (graphElement is Edge)
						{
							Port output = (graphElement as Edge).output;

							TreeGraphViewNode previousViewNode = output.node as TreeGraphViewNode;

							if (previousViewNode is TreeGraphViewInvokerNode)
								previousViewNode
									.NodeProperty
									.FindPropertyRelative(nameof(InvokerNode.NextNode))
									.SetManagedReferenceNull();
							
							else if (previousViewNode is TreeGraphViewListenerNode)
							{
								if
								(
									previousViewNode
										.NodeProperty
										.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion))
										.boolValue
								)
									(previousViewNode as TreeGraphViewListenerNode)
										.GetEventToListenForProperty(output)
										.FindPropertyRelative(nameof(EventToListenForWithBranch.NextNode))
										.SetManagedReferenceNull();
								
								else
									previousViewNode
										.NodeProperty
										.FindPropertyRelative(nameof(ListenerNode.NextNode))
										.SetManagedReferenceNull();
							}
						}
					}

					if (nodesToRemove.Count > 0)
						BehaviorTree.RemoveNodes(nodesToRemove);
				}
				
				if (graphViewChange.edgesToCreate != null)
				{
					foreach (Edge edge in graphViewChange.edgesToCreate)
					{
						if (edge.input.node is TreeGraphViewNode)
						{
							SerializedProperty nextNodeProperty = (edge.input.node as TreeGraphViewNode).NodeProperty;
							
							if (edge.output.node is TreeGraphViewEntryNode)
								(edge.output.node as TreeGraphViewEntryNode)
									.RootNodeProperty
									.SetManagedReference(nextNodeProperty);
							
							else if (edge.output.node is TreeGraphViewNode)
							{
								TreeGraphViewNode previousViewNode = edge.output.node as TreeGraphViewNode;

								if (previousViewNode is TreeGraphViewInvokerNode)
									previousViewNode
										.NodeProperty
										.FindPropertyRelative(nameof(InvokerNode.NextNode))
										.SetManagedReference(nextNodeProperty);
								
								else if (previousViewNode is TreeGraphViewListenerNode)
								{
									if
									(
										previousViewNode
											.NodeProperty
											.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion))
											.boolValue
									)
										(previousViewNode as TreeGraphViewListenerNode)
											.GetEventToListenForProperty(edge.output)
											.FindPropertyRelative(nameof(EventToListenForWithBranch.NextNode))
											.SetManagedReference(nextNodeProperty);
									
									else
										previousViewNode
											.NodeProperty
											.FindPropertyRelative(nameof(ListenerNode.NextNode))
											.SetManagedReference(nextNodeProperty);
								}
							}
						}
					}
				}
			}
			
			currentSerializedTree.ApplyModifiedProperties();

			if (currentSerializedTree != null)
				PopulateView(currentSerializedTree);
			
			return graphViewChange;
		}

		override public List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
		{
			List<Port> compatiblePorts = new List<Port>();

			foreach (Port endPort in ports)
				if (endPort.direction!=startPort.direction && endPort.node!=startPort.node)
					compatiblePorts.Add(endPort);
			
			return compatiblePorts;
		}
	}
}