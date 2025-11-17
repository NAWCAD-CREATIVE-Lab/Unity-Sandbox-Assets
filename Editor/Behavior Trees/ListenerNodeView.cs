// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using CREATIVE.SandboxAssets.Events;

using CREATIVE.SandboxAssets.BehaviorTrees;

using Node		= CREATIVE.SandboxAssets.BehaviorTrees.Node;
using GraphView	= UnityEditor.Experimental.GraphView;

namespace CREATIVE.SandboxAssets.Editor.BehaviorTrees
{
	/**
		A visual representation of either an ListenerNode or ListenerNode.Record
		object, inheriting from NodeView.
	*/
	public class ListenerNodeView : NodeView<ListenerNode.Record, ListenerNode>
	{
		Port nextNodePort;

		List<(SerializedProperty, VisualElement, Port)> editableEventInfoList;

		List<(EventToListenFor.Record, VisualElement, Port)> readOnlyEventInfoList;

		/**
			An implementation of INodeViewFactory that creates ListenerNodeView
			objects.
		*/
		public class Factory : INodeViewFactory
		{
			/**
				A default string that can optionally be used as title for
				ListenerNodeView objects, if none is provided by the underlying
				node.
			*/
			public string DefaultTitle { get => "Listener"; }

			/**
				Returns an ListenerNodeView object that cannot be edited, and
				that represents a given ListenerNode.Record object using a given
				GraphView.Node.

				@param graphViewNode The GraphView.Node that will represent
				the ListenerNode.Record.

				@param node The ListenerNode.Record that will be represented.

				@param title The title that will be displayed on the
				GraphView.Node.

				@param active Whether or not the given node is currently being
				evaluated.
			*/
			public INodeView<Node.IRecord<Node>, Node> CreateReadOnly
			(
				GraphView.Node visualElement,
				Node.IRecord<Node> node,
				string title,
				bool active
			)
			{
				if (node == null)
					throw new ArgumentNullException(nameof(node));


				if (!(node is ListenerNode.Record))
					throw new ArgumentException(nameof(node), nameof(node) + " is not an Listener Node");

				return new ListenerNodeView(visualElement, node as ListenerNode.Record, title, active);
			}

			/**
				Returns an ListenerNodeView object that can be edited from the
				GraphView, and that represents a given serialized ListenerNode
				using a given GraphView.Node.

				@param graphViewNode The GraphView.Node that will represent
				the serialized ListenerNode.

				@param nodeProperty The serialized ListenerNode that will be
				represented.

				@param title The title that will be displayed on the
				GraphView.Node.

				@param clearPort A function that can be called to disconnect the
				edge (if one exists) from a given output Port on the created
				ListenerNodeView.

				@param drawEdges A function that can be called to connect
				the necessary edges from all output ports on the created
				ListenerNodeView.
			*/
			public INodeView<Node.IRecord<Node>, Node> CreateEditable
			(
				GraphView.Node visualElement,
				SerializedProperty nodeProperty,
				string title,
				Action<Port> clearPort,
				Action<GraphView.Node> drawEdges
			)
			{
				if (nodeProperty == null)
					throw new ArgumentNullException(nameof(nodeProperty));

				if (!nodeProperty.ManagedReferenceIsOfType(typeof(ListenerNode)))
					throw new ArgumentException
					(
						nameof(nodeProperty),
						nameof(nodeProperty) + " is not a Managed Reference of type Listener Node."
					);

				return new ListenerNodeView(visualElement, nodeProperty, title, clearPort, drawEdges);
			}

			/**
				Adds a new ListenerNode to the given serialized BehaviorTree and
				returns an ListenerNodeView object that is editable from the
				GraphView, and that represents that ListenerNode.

				@param graphViewNode The GraphView.Node that will represent the
				serialized ListenerNode.

				@param behaviorTree The serialized BehaviorTree that will
				contain the new ListenerNode.

				@param title The title that will be displayed on the
				GraphView.Node.

				@param clearPort A function that can be called to disconnect the
				edge (if one exists) from a given output Port on the created
				ListenerNodeView.

				@param drawEdges A function that can be called to connect
				the necessary edges from all output ports on the created
				ListenerNodeView.
			*/
			public INodeView<Node.IRecord<Node>, Node> CreateNew
			(
				GraphView.Node visualElement,
				SerializedObject behaviorTree,
				string title,
				Action<Port> clearPort,
				Action<GraphView.Node> drawEdges
			)
				=> new ListenerNodeView
					(visualElement, TreeSerialization.AddNode(behaviorTree, new ListenerNode()), title, clearPort, drawEdges);
		}

		/*
			Constructor for a read-only node view, backed by an ListenerNode
			record.
		*/
		ListenerNodeView
		(
			GraphView.Node visualElement,
			ListenerNode.Record node,
			string title,
			bool active
		)
			: base(visualElement, node, title, active)
				=> setup();

		/*
			Constructor for an editable node view, backed by a serialized
			ListenerNode.
		*/
		ListenerNodeView
		(
			GraphView.Node visualElement,
			SerializedProperty nodeProperty,
			string title,
			Action<Port> clearPort,
			Action<GraphView.Node> drawEdges
		)
			: base(visualElement, nodeProperty, title, clearPort, drawEdges)
				=> setup();

		void setup()
		{
			nextNodePort =
				GraphViewNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));

			nextNodePort.portName = null;
			GraphViewNode.outputContainer.Add(nextNodePort);

			Toggle completeAfterFirstEventToggle = new Toggle();
			completeAfterFirstEventToggle.text = "Complete On First Event";

			Toggle branchOnCompletionToggle = new Toggle();
			branchOnCompletionToggle.text = "Branch On Completion";

			GraphViewNode.mainContainer.Add(branchOnCompletionToggle);
			GraphViewNode.mainContainer.Add(completeAfterFirstEventToggle);

			branchOnCompletionToggle.PlaceInFront(GraphViewNode.titleContainer);
			completeAfterFirstEventToggle.PlaceInFront(branchOnCompletionToggle);

			if (ReadOnly)
			{
				editableEventInfoList = null;

				readOnlyEventInfoList = new List<(EventToListenFor.Record, VisualElement, Port)>();

				nextNodePort.SetEnabled(false);

				completeAfterFirstEventToggle.SetEnabled(false);

				branchOnCompletionToggle.value = Node.BranchOnCompletion;
				branchOnCompletionToggle.SetEnabled(false);
			}

			else
			{
				editableEventInfoList = new List<(SerializedProperty, VisualElement, Port)>();

				readOnlyEventInfoList = null;

				completeAfterFirstEventToggle.BindProperty
					(NodeProperty.FindPropertyRelative(nameof(ListenerNode.CompleteOnFirstEvent)));

				branchOnCompletionToggle.BindProperty
					(NodeProperty.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion)));

				branchOnCompletionToggle.RegisterValueChangedCallback
				(
					(ChangeEvent<bool> changeEvent) =>
					{
						nextNodePort.SetEnabled(!changeEvent.newValue);

						completeAfterFirstEventToggle.SetEnabled(!changeEvent.newValue);

						RefreshEvents();
					}
				);

				Button addEventButton = new Button();
				addEventButton.text = "Add Event";
				addEventButton.clicked += () => AddEvent();
				GraphViewNode.extensionContainer.Add(addEventButton);
			}

			LoadEvents();
		}

		/*
			This is only called directly to populate the event containers on
			first construction.
		*/
		void LoadEvents()
		{
			if (ReadOnly)
				foreach (EventToListenFor.Record eventToListenFor in Node.EventsToListenFor)
					AddEvent(eventToListenFor);

			else
			{
				SerializedProperty eventsToListenForProperty =
					NodeProperty.FindPropertyRelative(nameof(ListenerNode.EventsToListenFor));

				for (int i = 0; i < eventsToListenForProperty.arraySize; i++)
					AddEvent(eventsToListenForProperty.GetArrayElementAtIndex(i));
			}

			GraphViewNode.RefreshExpandedState();
		}

		/*
			This is called whenever an event is added or removed from the node,
			and the event list needs to be cleared and re-populated.
		*/
		void RefreshEvents()
		{
			ClearPort(nextNodePort);

			foreach ((SerializedProperty, VisualElement, Port) eventInfo in editableEventInfoList)
			{
				if (eventInfo.Item3 != null)
					ClearPort(eventInfo.Item3);

				eventInfo.Item2.RemoveFromHierarchy();
			}

			editableEventInfoList.Clear();

			LoadEvents();

			DrawEdges();
		}

		/*
			Does one of three things:

				- When called in read-only mode with eventToListenFor set,
				  displays that event in a new read-only object field.
				
				- When called in editable mode with eventToListenForProperty
				  set, displays that event in a new editable object field, with
				  a button to remove it.
				
				- When called in editable mode with no parameters, adds a new
				  event to listen for to the serialized node, and displays the
				  new event in an editable object field, with a button to remove
				  it.
			
			New event displays added will also contain a port to branch from,
			if the Node branches on completion.
			
			Should not be called with both parameters set.

			Should not be called in read-only mode with no parameters, or with
			eventToListenForProperty set.

			Should not be called in editable mode with eventToListenFor set.
		*/
		void AddEvent
			(EventToListenFor.Record eventToListenFor = null, SerializedProperty eventToListenForProperty = null)
		{
			VisualElement eventContainer = new VisualElement();
			eventContainer.style.flexDirection = FlexDirection.Row;

			ObjectField sandboxEventField = new ObjectField();
			sandboxEventField.objectType = typeof(SandboxEvent);
			sandboxEventField.allowSceneObjects = false;

			SerializedProperty inlineEventToListenForProperty = eventToListenForProperty;

			if (ReadOnly)
			{
				sandboxEventField.value = eventToListenFor.Event;
				sandboxEventField.SetEnabled(false);
			}

			else
			{
				if (inlineEventToListenForProperty == null)
				{
					SerializedProperty eventsToListenForProperty =
						NodeProperty.FindPropertyRelative(nameof(ListenerNode.EventsToListenFor));

					inlineEventToListenForProperty = eventsToListenForProperty.GetArrayElementAtIndex
						(eventsToListenForProperty.arraySize++);

					inlineEventToListenForProperty.managedReferenceValue = new EventToListenFor.WithBranch();

					NodeProperty.serializedObject.ApplyModifiedProperties();
				}

				Button removeButton = new Button();
				removeButton.text = "-";
				removeButton.clicked += () =>
				{
					inlineEventToListenForProperty.DeleteCommand();

					NodeProperty.serializedObject.ApplyModifiedProperties();

					RefreshEvents();
				};

				eventContainer.Add(removeButton);

				sandboxEventField.BindProperty
					(inlineEventToListenForProperty.FindPropertyRelative(nameof(EventToListenFor.WithBranch.Event)));
			}

			eventContainer.Add(sandboxEventField);

			Port port = null;
			if
			(
				(ReadOnly && Node.BranchOnCompletion) ||
				(!ReadOnly && NodeProperty.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion)).boolValue)
			)
			{
				port = GraphViewNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
				port.portName = null;
				eventContainer.Add(port);
				GraphViewNode.outputContainer.Add(eventContainer);
			}

			else
				GraphViewNode.extensionContainer.Add(eventContainer);

			if (ReadOnly)
				readOnlyEventInfoList.Add((eventToListenFor, eventContainer, port));

			else
				editableEventInfoList.Add((inlineEventToListenForProperty, eventContainer, port));
		}
		void AddEvent(SerializedProperty eventToListenForProperty) => AddEvent(null, eventToListenForProperty);

		/**
			Returns the ports that this ListenerNodeView uses as an output to
			connect to the given node view.

			Returns an empty Enumerable if this ListenerNodeView does not output
			to the given ListenerNodeView.
		*/
		override public IEnumerable<Port> GetConnectedOutputPorts(INodeView<Node.IRecord<Node>, Node> nextNode)
		{
			if (nextNode == null)
				throw new ArgumentNullException(nameof(nextNode));

			if (ReadOnly != nextNode.ReadOnly)
				throw new InvalidOperationException
					("A Read-Only Node View cannot be connected to an editable one.");

			List<Port> connectedPorts = new List<Port>();

			if (ReadOnly)
			{
				if (Node.BranchOnCompletion)
				{
					foreach ((EventToListenFor.Record, VisualElement, Port) eventInfo in readOnlyEventInfoList)
						if (nextNode.RepresentsNode(Node.EventsToListenForWithBranches[eventInfo.Item1]))
							connectedPorts.Add(eventInfo.Item3);
				}

				else if (nextNode.RepresentsNode(Node.NextNode))
					connectedPorts.Add(nextNodePort);
			}

			else
			{
				if (NodeProperty.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion)).boolValue)
				{
					foreach ((SerializedProperty, VisualElement, Port) eventInfo in editableEventInfoList)
						if
						(
							nextNode.RepresentsNodeProperty
								(eventInfo.Item1.FindPropertyRelative(nameof(EventToListenFor.WithBranch.NextNode)))
						)
							connectedPorts.Add(eventInfo.Item3);
				}

				else if
					(nextNode.RepresentsNodeProperty(NodeProperty.FindPropertyRelative(nameof(ListenerNode.NextNode))))
						connectedPorts.Add(nextNodePort);
			}

			return connectedPorts;
		}

		/**
			Overwrites the serialized Node reference that is represented
			by the given port with the serialized Node that is represented by
			the given node view.
		*/
		override public void SetConnectionForOutputPort(Port outputPort, INodeView<Node.IRecord<Node>, Node> nextNode)
		{
			if (ReadOnly)
				throw ReadOnlyException;

			if (outputPort == null)
				throw new ArgumentNullException(nameof(outputPort));

			if (nextNode == null)
				throw new ArgumentNullException(nameof(nextNode));

			if (ReadOnly != nextNode.ReadOnly)
				throw new ArgumentException
				(
					nameof(nextNode),
					nameof(nextNode) + " has a Read-Only status different from this Listener Node View."
				);

			SerializedProperty propertyToSet = null;

			if (NodeProperty.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion)).boolValue)
			{
				foreach ((SerializedProperty, VisualElement, Port) eventInfo in editableEventInfoList)
					if (eventInfo.Item3 == outputPort)
						propertyToSet = eventInfo.Item1.FindPropertyRelative(nameof(EventToListenFor.WithBranch.NextNode));
			}

			else if (nextNodePort == outputPort)
				propertyToSet = NodeProperty.FindPropertyRelative(nameof(ListenerNode.NextNode));

			if (propertyToSet == null)
				throw new ArgumentException
				(
					nameof(outputPort),
					nameof(outputPort) + " does not belong to this Listener Node View."
				);

			nextNode.SetManagedReferenceToNodeProperty(propertyToSet);
		}

		/**
			Nulls out the serialized Node reference that is represented by
			the given port.
		*/
		override public void ClearConnectionForOutputPort(Port outputPort)
		{
			if (ReadOnly)
				throw ReadOnlyException;

			if (outputPort == null)
				throw new ArgumentNullException(nameof(outputPort));

			SerializedProperty propertyToSet = null;

			if (NodeProperty.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion)).boolValue)
			{
				foreach ((SerializedProperty, VisualElement, Port) eventInfo in editableEventInfoList)
					if (eventInfo.Item3 == outputPort)
						propertyToSet = eventInfo.Item1.FindPropertyRelative(nameof(EventToListenFor.WithBranch.NextNode));
			}

			else if (nextNodePort == outputPort)
				propertyToSet = NodeProperty.FindPropertyRelative(nameof(ListenerNode.NextNode));

			if (propertyToSet == null)
				throw new ArgumentException
				(
					nameof(outputPort),
					nameof(outputPort) + " does not belong to this Listener Node View."
				);

			propertyToSet.SetManagedReferenceNull();

			NodeProperty.serializedObject.ApplyModifiedProperties();
		}
	}
}