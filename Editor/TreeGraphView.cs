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
		const string UssGuid = "1be56c8321208c245be26dc7f4beb315";

		readonly IManipulator selectionDragger	= new SelectionDragger();
		readonly IManipulator rectangleSelector	= new RectangleSelector();

		bool populated = false;

		bool readOnly = false;
		
		SerializedObject currentSerializedTree = null;

		BehaviorTreeRunner currentBehaviorTreeRunner = null;

		BehaviorTree currentBehaviorTree = null;

		TreeGraphViewEntryNode currentEntryNode = null;

		Vector2 localMousePosition = Vector2.zero;
		
		public TreeGraphView()
		{
			Insert(0, new GridBackground());

			this.AddManipulator(new ContentZoomer());
			this.AddManipulator(new ContentDragger());
			
			styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(UssGuid)));

			RegisterCallback<MouseDownEvent>(UpdateLocalMousePosition);

			graphViewChanged += OnGraphViewChanged;

			Selection.selectionChanged += PopulateSelection;

			currentBehaviorTree = ScriptableObject.CreateInstance(typeof(BehaviorTree)) as BehaviorTree;
		}

		~TreeGraphView()
		{
			UnregisterCallback<MouseDownEvent>(UpdateLocalMousePosition);
			
			graphViewChanged -= OnGraphViewChanged;
			
			Selection.selectionChanged -= PopulateSelection;

			ScriptableObject.DestroyImmediate(currentBehaviorTree);
		}

		void UpdateLocalMousePosition(MouseDownEvent mouseDownEvent)
		{
			localMousePosition =
			(
				mouseDownEvent.localMousePosition -
				new Vector2(viewTransform.position.x, viewTransform.position.y)
			) / scale;
		}

		public void PopulateSelection()
		{
			UnityEngine.Object selectedObject = Selection.activeObject;

			if (selectedObject!=null)
			{
				if (selectedObject is BehaviorTree)
					Populate(new SerializedObject(selectedObject));
				
				else if (selectedObject is GameObject)
				{
					BehaviorTreeRunner behaviorTreeRunner =
						(selectedObject as GameObject).GetComponent<BehaviorTreeRunner>();
					
					if (behaviorTreeRunner != null)
						Populate(behaviorTreeRunner);
				}
			}
		}

		void UnPopulate()
		{
			this.RemoveManipulator(selectionDragger);
			this.RemoveManipulator(rectangleSelector);
			
			graphViewChanged -= OnGraphViewChanged;
			DeleteElements(graphElements);
			graphViewChanged += OnGraphViewChanged;

			if (currentBehaviorTreeRunner != null)
			{
				currentBehaviorTreeRunner.BehaviorTreeActivated -= RefreshBehaviorTreeInRunner;

				currentBehaviorTreeRunner.BehaviorTreeDeactivated -= EditBehaviorTreeInRunner;
				
				currentBehaviorTreeRunner.ActiveBehaviorTreeCurrentNodeChanged -= RefreshBehaviorTreeInRunner;

				currentBehaviorTreeRunner = null;
			}

			currentSerializedTree = null;
			
			currentEntryNode = null;

			populated = false;
		}

		void RefreshBehaviorTreeInRunner() => Populate(currentBehaviorTreeRunner);

		void Populate(BehaviorTreeRunner behaviorTreeRunner)
		{
			if (behaviorTreeRunner.BehaviorTreeIsSet)
			{
				UnPopulate();
				
				currentBehaviorTreeRunner = behaviorTreeRunner;

				currentBehaviorTreeRunner.BehaviorTreeActivated += RefreshBehaviorTreeInRunner;

				currentBehaviorTreeRunner.BehaviorTreeDeactivated += EditBehaviorTreeInRunner;
				
				currentBehaviorTreeRunner.ActiveBehaviorTreeCurrentNodeChanged += RefreshBehaviorTreeInRunner;
				
				if (currentBehaviorTreeRunner.BehaviorTreeIsActive)
				{
					Node currentNode;
					
					currentBehaviorTreeRunner.CloneActiveBehaviorTree(currentBehaviorTree, out currentNode);

					foreach (Node node in currentBehaviorTree.Nodes)
					{
						if (node is ListenerNode)
						{
							if (node == currentNode)
								AddElement(new TreeGraphViewListenerNode(this, node as ListenerNode, true));

							else
								AddElement(new TreeGraphViewListenerNode(this, node as ListenerNode));
						}
						
						else if (node is InvokerNode)
							AddElement(new TreeGraphViewInvokerNode(this, node as InvokerNode));
					}

					currentEntryNode = new TreeGraphViewEntryNode
					(
						currentBehaviorTree.RootNode,
						currentBehaviorTree.EntryNodePosition
					);

					AddElement(currentEntryNode);

					readOnly = true;

					populated = true;
				
					ReDrawEdges();
				}

				else
					EditBehaviorTreeInRunner();
			}
		}

		void EditBehaviorTreeInRunner() => Populate(currentBehaviorTreeRunner.InactiveBehaviorTree);

		void Populate(SerializedObject serializedTree)
		{
			if (serializedTree == null)
				throw new ArgumentNullException(nameof(serializedTree));
			
			if (!(serializedTree.targetObject is BehaviorTree))
				throw new ArgumentException
					(nameof(serializedTree), nameof(serializedTree) + " does not represent a Behavior Tree.");
			
			UnPopulate();

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

			this.AddManipulator(selectionDragger);
			this.AddManipulator(rectangleSelector);

			populated = true;

			readOnly = false;
			
			ReDrawEdges();
		}

		public void ReDrawEdges()
		{
			if (populated)
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

						if
						(
							currentEntryNode.RootNodeIsSet &&
							(
								(
									readOnly &&
									treeNodeView.Node==currentEntryNode.RootNode
								) ||
								(
									!readOnly &&
									treeNodeView.NodeProperty.ManagedReferenceEquals(currentEntryNode.RootNodeProperty)
								)
							)
						)
							AddElement(currentEntryNode.RootNodePort.ConnectTo(treeNodeView.PreviousNodePort));
					
						if (treeNodeView is TreeGraphViewInvokerNode)
						{
							TreeGraphViewInvokerNode invokerNodeView = treeNodeView as TreeGraphViewInvokerNode;

							if (readOnly)
							{
								if (invokerNodeView.InvokerNode.NextNode != null)
								{
									TreeGraphViewNode nextTreeNodeView = FindNode(invokerNodeView.InvokerNode.NextNode);

									if (nextTreeNodeView != null)
										AddElement
											(invokerNodeView.NextNodePort.ConnectTo(nextTreeNodeView.PreviousNodePort));
								}
							}

							else
							{
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
						}

						else if (treeNodeView is TreeGraphViewListenerNode)
						{
							TreeGraphViewListenerNode listenerNodeView = treeNodeView as TreeGraphViewListenerNode;

							if (readOnly)
							{
								if (listenerNodeView.ListenerNode.BranchOnCompletion)
								{
									foreach
									(
										EventToListenForWithBranch eventToListenFor in
										listenerNodeView.ListenerNode.EventsToListenFor
									)
									{
										if (eventToListenFor.NextNode != null)
										{
											TreeGraphViewNode nextTreeNodeView = FindNode(eventToListenFor.NextNode);

											if (nextTreeNodeView != null)
												AddElement
												(
													listenerNodeView.GetPort(eventToListenFor).ConnectTo
														(nextTreeNodeView.PreviousNodePort)
												);
										}
									}
								}

								else
								{
									if (listenerNodeView.ListenerNode.NextNode != null)
									{
										TreeGraphViewNode nextTreeNodeView =
											FindNode(listenerNodeView.ListenerNode.NextNode);
										
										if (nextTreeNodeView != null)
											AddElement
											(
												listenerNodeView.NextNodePort.ConnectTo
													(nextTreeNodeView.PreviousNodePort)
											);
									}
								}
							}

							else
							{
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
												listenerNodeView.NextNodePort.ConnectTo
													(nextTreeNodeView.PreviousNodePort)
											);
									}
								}
							}
						}
					}
				}

				if (readOnly)
					foreach (Edge edge in edges)
						edge.SetEnabled(false);

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

		TreeGraphViewNode FindNode(Node node)
		{
			foreach (UnityEditor.Experimental.GraphView.Node graphViewNode in nodes)
			{
				if (graphViewNode is TreeGraphViewNode)
				{
					TreeGraphViewNode treeGraphViewNode = graphViewNode as TreeGraphViewNode;

					if (node == treeGraphViewNode.Node)
						return treeGraphViewNode;
				}
			}

			return null;
		}

		override public void BuildContextualMenu(ContextualMenuPopulateEvent contextualMenuPopulateEvent)
		{
			if (populated && !readOnly)
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
			if (populated)
			{
				if (readOnly)
				{
					if (graphViewChange.edgesToCreate != null)
						graphViewChange.edgesToCreate.Clear();

					if (graphViewChange.elementsToRemove != null)
						graphViewChange.elementsToRemove.Clear();
					
					if (graphViewChange.movedElements != null)
						graphViewChange.movedElements.Clear();
					
					graphViewChange.moveDelta = Vector2.zero;
				}
			
				else
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
								SerializedProperty nextNodeProperty =
									(edge.input.node as TreeGraphViewNode).NodeProperty;
								
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

					currentSerializedTree.ApplyModifiedProperties();

					if (currentSerializedTree != null)
						Populate(currentSerializedTree);
				}
			}
			
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