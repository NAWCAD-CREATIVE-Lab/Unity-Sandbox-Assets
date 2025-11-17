// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Serialization;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

using CREATIVE.SandboxAssets.BehaviorTrees;

using Node				= CREATIVE.SandboxAssets.BehaviorTrees.Node;
using GraphView			= UnityEditor.Experimental.GraphView;

namespace CREATIVE.SandboxAssets.Editor.BehaviorTrees
{
	/**
		A child class of GraphView that displays and allows editing of
		BehaviorTree objects by representing nodes as INodeView objects.

		Can be populated in an editable mode for serialized BehaviorTree
		objects, as well as a read-only mode for BehaviorTreeRunner objects in
		an active scene.

		Handles all connections and disconnections between INodeView objects,
		and flows any changes to individual INodeView objects down to them.
	*/
	[UxmlElement]
	public partial class TreeView : GraphView.GraphView
	{
		/*
			There doesn't seem to be a way to add USS elements to a
			VisualElement through code.

			This file exists just to do basic visual configuration for the
			GraphView grid.

			It's referenced by GUID so it can be freely renamed.
		*/
		const string UssGuid = "1be56c8321208c245be26dc7f4beb315";

		/*
			These manipulators are only added to the GraphView if it isn't
			read-only. They're created here just to avoid creating them every
			time the GraphView is populated with an editable BehaviorTree.
		*/
		readonly IManipulator selectionDragger = new SelectionDragger();
		readonly IManipulator rectangleSelector = new RectangleSelector();

		bool populated = false;

		bool readOnly = false;

		SerializedObject currentSerializedTree = null;

		BehaviorTreeRunner currentBehaviorTreeRunner = null;

		readonly Dictionary<GraphElement, INodeView<Node.IRecord<Node>, Node>> currentTreeNodes =
			new Dictionary<GraphElement, INodeView<Node.IRecord<Node>, Node>>();

		EntryView currentEntryView = null;

		Vector2 localMousePosition = Vector2.zero;

		public TreeView()
		{
			Insert(0, new GridBackground());

			this.AddManipulator(new ContentZoomer());
			this.AddManipulator(new ContentDragger());

			styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(UssGuid)));

			RegisterCallback<MouseDownEvent>(UpdateLocalMousePosition);

			graphViewChanged += OnGraphViewChanged;

			Selection.selectionChanged += PopulateSelection;

			Undo.undoRedoPerformed += PopulateSelection;
		}

		~TreeView()
		{
			UnregisterCallback<MouseDownEvent>(UpdateLocalMousePosition);

			graphViewChanged -= OnGraphViewChanged;

			Selection.selectionChanged -= PopulateSelection;

			Undo.undoRedoPerformed -= PopulateSelection;
		}

		void UpdateLocalMousePosition(MouseDownEvent mouseDownEvent)
		{
			localMousePosition =
			(
				mouseDownEvent.localMousePosition -
				new Vector2(viewTransform.position.x, viewTransform.position.y)
			) / scale;
		}

		/**
			Populates the view if a BehaviorTree object (or BehaviorTreeRunner
			GameObject) is currently selected.
		*/
		public void PopulateSelection()
		{
			UnityEngine.Object selectedObject = Selection.activeObject;

			if (selectedObject != null)
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

			currentEntryView = null;

			currentTreeNodes.Clear();

			graphViewChanged -= OnGraphViewChanged;
			DeleteElements(graphElements);
			graphViewChanged += OnGraphViewChanged;

			if (currentBehaviorTreeRunner != null)
			{
				currentBehaviorTreeRunner.BehaviorTreeActivated -= PopulateSelection;

				currentBehaviorTreeRunner.BehaviorTreeDeactivated -= EditBehaviorTreeInRunner;

				currentBehaviorTreeRunner.ActiveBehaviorTreeCurrentNodeChanged -= PopulateSelection;

				currentBehaviorTreeRunner = null;
			}

			currentSerializedTree = null;

			populated = false;
		}

		/*
			Populate the GraphView using a BehaviorTreeRunner.

			If the tree is actively running, the GraphView is populated with a
			read-only Graph.

			If the tree is not actively running, the GraphView is populated with
			a the serialized tree assigned to the BehaviorTreeRunner.
		*/
		void Populate(BehaviorTreeRunner behaviorTreeRunner)
		{
			if (behaviorTreeRunner.BehaviorTreeIsSet)
			{
				UnPopulate();

				currentBehaviorTreeRunner = behaviorTreeRunner;

				currentBehaviorTreeRunner.BehaviorTreeActivated += PopulateSelection;

				currentBehaviorTreeRunner.BehaviorTreeDeactivated += EditBehaviorTreeInRunner;

				currentBehaviorTreeRunner.ActiveBehaviorTreeCurrentNodeChanged += PopulateSelection;

				if (currentBehaviorTreeRunner.BehaviorTreeIsActive)
				{
					INodeView<Node.IRecord<Node>, Node> rootNodeView = null;

					foreach (Node.IRecord<Node> node in currentBehaviorTreeRunner.ActiveBehaviorTree)
					{
						GraphView.Node graphViewNode = new GraphView.Node();

						INodeViewFactory factory =
							NodeViewFactoryLookup.FromRecordType(node.GetType());

						INodeView<Node.IRecord<Node>, Node> nodeView;

						if (node == currentBehaviorTreeRunner.ActiveBehaviorTreeCurrentNode)
							nodeView = factory.CreateReadOnly(graphViewNode, node, factory.DefaultTitle, true);

						else
							nodeView = factory.CreateReadOnly(graphViewNode, node, factory.DefaultTitle, false);

						AddElement(graphViewNode);

						currentTreeNodes.Add(graphViewNode, nodeView);

						if (node == currentBehaviorTreeRunner.ActiveBehaviorTree.RootNode)
							rootNodeView = nodeView;
					}

					currentEntryView = new EntryView
						(currentBehaviorTreeRunner.ActiveBehaviorTreeEntryNodePosition);

					AddElement(currentEntryView);

					Edge rootEdge = currentEntryView.RootNodePort.ConnectTo(rootNodeView.PreviousNodePort);

					rootEdge.SetEnabled(false);

					AddElement(rootEdge);

					readOnly = true;

					foreach (GraphElement graphElement in currentTreeNodes.Keys)
						DrawEdges(graphElement);

					populated = true;
				}

				else
					EditBehaviorTreeInRunner();
			}
		}

		void EditBehaviorTreeInRunner() =>
			Populate(new SerializedObject(currentBehaviorTreeRunner.InactiveBehaviorTree));

		/*
			Populate the GraphView with an editable serialized BehaviorTree
		*/
		void Populate(SerializedObject serializedTree)
		{
			if (serializedTree == null)
				throw new ArgumentNullException(nameof(serializedTree));

			if (!(serializedTree.targetObject is BehaviorTree))
				throw new ArgumentException
					(nameof(serializedTree), nameof(serializedTree) + " does not represent a Behavior Tree.");

			UnPopulate();

			SerializedProperty rootNodeProperty = serializedTree.FindProperty(nameof(BehaviorTree.RootNode));

			INodeView<Node.IRecord<Node>, Node> rootNodeView = null;

			SerializedProperty nodesProperty = serializedTree.FindProperty(nameof(BehaviorTree.Nodes));
			for (int i = 0; i < nodesProperty.arraySize; i++)
			{
				SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(i);

				if (nodeProperty!=null && !nodeProperty.ManagedReferenceIsNull())
				{
					GraphView.Node graphViewNode = new GraphView.Node();

					INodeViewFactory factory =
						NodeViewFactoryLookup.FromNodeType(nodeProperty.GetManagedReferenceType());

					INodeView<Node.IRecord<Node>, Node> nodeView =
						factory.CreateEditable(graphViewNode, nodeProperty, factory.DefaultTitle, ClearPort, DrawEdges);

					currentTreeNodes.Add(graphViewNode, nodeView);

					AddElement(graphViewNode);

					if (!rootNodeProperty.ManagedReferenceIsNull() && rootNodeProperty.ManagedReferenceEquals(nodeProperty))
						rootNodeView = nodeView;
				}
			}

			currentEntryView = new EntryView
			(
				rootNodeProperty,
				serializedTree.FindProperty(nameof(BehaviorTree.EntryNodePosition))
			);

			AddElement(currentEntryView);

			if (rootNodeView != null)
				AddElement(currentEntryView.RootNodePort.ConnectTo(rootNodeView.PreviousNodePort));
			
			currentSerializedTree = serializedTree;

			this.AddManipulator(selectionDragger);
			this.AddManipulator(rectangleSelector);

			readOnly = false;

			foreach (GraphElement graphElement in currentTreeNodes.Keys)
				DrawEdges(graphElement);

			populated = true;
		}

		/*
			Can be called by a node in order to clear an edge that may be
			connected from a given output port, if some of the connections have
			changed.
		*/
		void ClearPort(Port port)
		{
			graphViewChanged -= OnGraphViewChanged;
			DeleteElements(port.connections);
			graphViewChanged += OnGraphViewChanged;
		}

		/*
			Can be called by a node in order to display edges for all output
			ports on that node.

			Presumes that it is being called either at initial setup, or after
			all output ports have been cleared.
		*/
		void DrawEdges(GraphElement graphElement)
		{
			graphViewChanged -= OnGraphViewChanged;

			foreach (INodeView<Node.IRecord<Node>, Node> nextNodeView in currentTreeNodes.Values)
			{
				foreach (Port nextNodePort in currentTreeNodes[graphElement].GetConnectedOutputPorts(nextNodeView))
				{
					Edge edge = nextNodeView.PreviousNodePort.ConnectTo(nextNodePort);

					if (readOnly)
						edge.SetEnabled(false);

					AddElement(edge);
				}
			}

			graphViewChanged += OnGraphViewChanged;
		}

		/*
			Populates the right-click menu
		*/
		override public void BuildContextualMenu(ContextualMenuPopulateEvent contextualMenuPopulateEvent)
		{
			if (populated)
			{
				contextualMenuPopulateEvent.menu.AppendAction
				(
					"Unload Current Tree",
					(dropdownMenuAction) => UnPopulate()
				);

				if (!readOnly)
				{
					foreach (INodeViewFactory factory in NodeViewFactoryLookup.All)
					{
						INodeViewFactory inlineFactory = factory;

						contextualMenuPopulateEvent.menu.AppendAction
						(
							"Add new " + inlineFactory.DefaultTitle,
							(dropdownMenuAction) =>
							{
								if (populated && !readOnly)
								{
									GraphView.Node graphViewNode = new GraphView.Node();

									INodeView<Node.IRecord<Node>, Node> nodeView =
										inlineFactory.CreateNew
										(
											graphViewNode,
											currentSerializedTree,
											inlineFactory.DefaultTitle,
											ClearPort,
											DrawEdges
										);

									currentTreeNodes.Add(graphViewNode, nodeView);

									AddElement(graphViewNode);

									nodeView.SetPosition(localMousePosition);
								}
							}
						);
					}
				}
			}

			else
				contextualMenuPopulateEvent.menu.AppendAction
				(
					"Load Selection",
					(dropdownMenuAction) => PopulateSelection()
				);
		}

		/*
			Reacts to changes to the BehaviorTree being currently displayed.

			If the GraphView is in read-only mode, this method intercepts the
			changes and ensures nothing happens.

			Otherwise, it flows changes down to the affected node views to allow
			them to edit the underlying serialized nodes.
		*/
		GraphViewChange OnGraphViewChanged(GraphViewChange viewChange)
		{
			if (populated)
			{
				// Never remove the EntryView
				if (viewChange.elementsToRemove != null)
					viewChange.elementsToRemove.Remove(currentEntryView);

				List<INodeView<Node.IRecord<Node>, Node>> nodeViewsToRemove
					= new List<INodeView<Node.IRecord<Node>, Node>>();

				List<Edge> edgesToRemove = new List<Edge>();

				/*
					Seperate elementsToRemove into Nodes and edges.

					Presumes elementsToRemove only contains INodeView objects
					added by this instance, or edges.

					If that's not the case, soemthing has gone horribly wrong
					anyway.
				*/
				if (!readOnly && viewChange.elementsToRemove != null)
				{
					foreach (GraphElement element in viewChange.elementsToRemove)
					{
						if (currentTreeNodes.ContainsKey(element))
							nodeViewsToRemove.Add(currentTreeNodes[element]);

						else
							edgesToRemove.Add(element as Edge);
					}
				}

				/*
					Clear out everythig in the viewChange.

					If the GraphView is read-only, then nothing should be
					changed.

					If nodes are about to be removed, the whole Graph should be
					re-drawn, since that would be simpler than trying to figure
					out which nodes might output to deleted nodes and modifying
					them.
				*/
				if (readOnly || nodeViewsToRemove.Count > 0)
				{
					if (viewChange.edgesToCreate != null)
						viewChange.edgesToCreate.Clear();

					if (viewChange.elementsToRemove != null)
						viewChange.elementsToRemove.Clear();

					if (viewChange.movedElements != null)
						viewChange.movedElements.Clear();

					viewChange.moveDelta = Vector2.zero;
				}

				/*
					Edit serialized tree to remove deleted nodes, then re-draw
					the whole graph.
				*/
				if (nodeViewsToRemove.Count > 0)
				{
					foreach (INodeView<Node.IRecord<Node>, Node> node in nodeViewsToRemove)
						node.QueueNodePropertyForDeletion();

					nodeViewsToRemove[0].DeleteQueuedProperties();

					PopulateSelection();
				}

				/*
					Handle the rest of the changes individually, if the graph
					doesnt need to be re-drawn.
				*/
				else
				{
					// Creating edges
					if (viewChange.edgesToCreate != null)
					{
						foreach (Edge edge in viewChange.edgesToCreate)
						{
							INodeView<Node.IRecord<Node>, Node> nextNodeView = currentTreeNodes[edge.input.node];

							/*
								If this edge is actually just setting a new root
								node, do that.
							*/
							if (edge.output.node == currentEntryView)
								currentEntryView.SetRootNode(nextNodeView);

							// Otherwise, set the reference in the output node
							else
								currentTreeNodes[edge.output.node].SetConnectionForOutputPort
									(edge.output, nextNodeView);
						}
					}

					// Removing edges
					foreach (Edge edge in edgesToRemove)
					{
						/*
							If this edge removal is actually just nulling out
							the root node, then do that.
						*/
						if (edge.output.node == currentEntryView)
							currentEntryView.ClearRootNode();

						// Otherwise, null out the reference in the output node
						else
							currentTreeNodes[edge.output.node].ClearConnectionForOutputPort(edge.output);
					}

					// Update serialized positions for all moved nodes.
					if (viewChange.movedElements != null)
						foreach (GraphElement element in viewChange.movedElements)
							if (currentTreeNodes.ContainsKey(element))
								currentTreeNodes[element].SaveVisualElementPosition();
				}
			}

			/*
				Pass up the (possibly modified) viewChange so the parent
				GraphView can actually change how the graph looks
			*/
			return viewChange;
		}

		/*
			This boilerplate is here so that a node can't connect to itself, an
			an output port can't connect to an output port, and in input port
			can't connect to input port.
		*/
		override public List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
		{
			List<Port> compatiblePorts = new List<Port>();

			foreach (Port endPort in ports)
				if (endPort.direction != startPort.direction && endPort.node != startPort.node)
					compatiblePorts.Add(endPort);

			return compatiblePorts;
		}
	}
}