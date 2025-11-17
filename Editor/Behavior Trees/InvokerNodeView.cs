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
		A visual representation of either an InvokerNode or InvokerNode.Record
		object, inheriting from NodeView.
	*/
	public class InvokerNodeView : NodeView<InvokerNode.Record, InvokerNode>
	{
		Port nextNodePort;

		/*
			Each event this node will invoke gets displayed as a reference
			inside a VisualElement and stored in this list.

			If this node is not read-only, each VisualElement will also contain
			a button to remove the event from the node.
		*/
		readonly List<VisualElement> eventContainers = new List<VisualElement>();

		/**
			An implementation of INodeViewFactory that creates InvokerNodeView
			objects.
		*/
		public class Factory : INodeViewFactory
		{
			/**
				A default string that can optionally be used as title for
				InvokerNodeView objects, if none is provided by the underlying
				node.
			*/
			public string DefaultTitle { get => "Invoker"; }

			/**
				Returns an InvokerNodeView object that cannot be edited, and
				that represents a given InvokerNode.Record object using a given
				GraphView.Node.

				@param graphViewNode The GraphView.Node that will represent
				the InvokerNode.Record.

				@param node The InvokerNode.Record that will be represented.

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


				if (!(node is InvokerNode.Record))
					throw new ArgumentException(nameof(node), nameof(node) + " is not an Invoker Node");

				return new InvokerNodeView(visualElement, node as InvokerNode.Record, title, active);
			}

			/**
				Returns an InvokerNodeView object that can be edited from the
				GraphView, and that represents a given serialized InvokerNode
				using a given GraphView.Node.

				@param graphViewNode The GraphView.Node that will represent
				the serialized InvokerNode.

				@param nodeProperty The serialized InvokerNode that will be
				represented.

				@param title The title that will be displayed on the
				GraphView.Node.

				@param clearPort A function that can be called to disconnect the
				edge (if one exists) from a given output Port on the created
				InvokerNodeView.

				@param drawEdges A function that can be called to connect
				the necessary edges from all output ports on the created
				InvokerNodeView.
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

				if (!nodeProperty.ManagedReferenceIsOfType(typeof(InvokerNode)))
					throw new ArgumentException
					(
						nameof(nodeProperty),
						nameof(nodeProperty) + " is not a Managed Reference of type Invoker Node."
					);

				return new InvokerNodeView(visualElement, nodeProperty, title, clearPort, drawEdges);
			}

			/**
				Adds a new InvokerNode to the given serialized BehaviorTree and
				returns an InvokerNodeView object that is editable from the
				GraphView, and that represents that InvokerNode.

				@param graphViewNode The GraphView.Node that will represent the
				serialized InvokerNode.

				@param behaviorTree The serialized BehaviorTree that will
				contain the new InvokerNode.

				@param title The title that will be displayed on the
				GraphView.Node.

				@param clearPort A function that can be called to disconnect the
				edge (if one exists) from a given output Port on the created
				InvokerNodeView.

				@param drawEdges A function that can be called to connect
				the necessary edges from all output ports on the created
				InvokerNodeView.
			*/
			public INodeView<Node.IRecord<Node>, Node> CreateNew
			(
				GraphView.Node visualElement,
				SerializedObject behaviorTree,
				string title,
				Action<Port> clearPort,
				Action<GraphView.Node> drawEdges
			)
				=> new InvokerNodeView
				(
					visualElement,
					TreeSerialization.AddNode(behaviorTree, new InvokerNode()),
					title,
					clearPort,
					drawEdges
				);
		}

		/*
			Constructor for a read-only node view, backed by an InvokerNode
			record.
		*/
		InvokerNodeView
		(
			GraphView.Node visualElement,
			InvokerNode.Record node,
			string title,
			bool active
		)
			: base(visualElement, node, title, active)
				=> setup();


		/*
			Constructor for an editable node view, backed by a serialized
			InvokerNode.
		*/
		InvokerNodeView
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
				GraphViewNode.InstantiatePort
					(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));

			nextNodePort.portName = null;
			GraphViewNode.outputContainer.Add(nextNodePort);

			if (ReadOnly)
				nextNodePort.SetEnabled(false);

			else
			{
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
				foreach (EventToInvoke.Record eventToInvoke in Node.EventsToInvoke)
					AddEvent(eventToInvoke);

			else
			{
				SerializedProperty eventsToInvokeProperty =
					NodeProperty.FindPropertyRelative(nameof(InvokerNode.EventsToInvoke));

				for (int i = 0; i < eventsToInvokeProperty.arraySize; i++)
					AddEvent(eventsToInvokeProperty.GetArrayElementAtIndex(i));
			}

			GraphViewNode.RefreshExpandedState();
		}

		/*
			This is called whenever an event is added or removed from the node,
			and the event list needs to be cleared and re-populated.
		*/
		void RefreshEvents()
		{
			foreach (VisualElement eventContainer in eventContainers)
				eventContainer.RemoveFromHierarchy();

			eventContainers.Clear();

			LoadEvents();

			DrawEdges();
		}

		/*
			Does one of three things:

				- When called in read-only mode with eventToInvoke set, displays
				  that event in a new read-only object field.
				
				- When called in editable mode with eventToInvokeProperty set,
				  displays that event in a new editable object field, with a
				  button to remove it.
				
				- When called in editable mode with no parameters, adds a new
				  event to invoke to the serialized node, and displays the new
				  event in an editable object field, with a button to remove it. 
			
			Should not be called with both parameters set.

			Should not be called in read-only mode with no parameters, or with
			eventToInvokeProperty set.

			Should not be called in editable mode with eventToInvoke set.
		*/
		void AddEvent(EventToInvoke.Record eventToInvoke = null, SerializedProperty eventToInvokeProperty = null)
		{
			VisualElement eventContainer = new VisualElement();
			eventContainer.style.flexDirection = FlexDirection.Row;

			ObjectField sandboxEventField = new ObjectField();
			sandboxEventField.objectType = typeof(SandboxEvent);
			sandboxEventField.allowSceneObjects = false;

			SerializedProperty inlineEventToInvokeProperty = eventToInvokeProperty;

			if (ReadOnly)
			{
				sandboxEventField.value = eventToInvoke.Event;
				sandboxEventField.SetEnabled(false);
			}

			else
			{
				if (inlineEventToInvokeProperty == null)
				{
					SerializedProperty eventsToInvokeProperty =
						NodeProperty.FindPropertyRelative(nameof(InvokerNode.EventsToInvoke));

					inlineEventToInvokeProperty = eventsToInvokeProperty.GetArrayElementAtIndex
						(eventsToInvokeProperty.arraySize++);

					inlineEventToInvokeProperty.managedReferenceValue = new EventToInvoke();

					NodeProperty.serializedObject.ApplyModifiedProperties();
				}

				Button removePortButton = new Button();
				removePortButton.text = "-";
				removePortButton.clicked += () =>
				{
					inlineEventToInvokeProperty.DeleteCommand();

					NodeProperty.serializedObject.ApplyModifiedProperties();

					RefreshEvents();
				};
				eventContainer.Add(removePortButton);

				sandboxEventField.BindProperty
					(inlineEventToInvokeProperty.FindPropertyRelative(nameof(EventToInvoke.Event)));
			}

			eventContainer.Add(sandboxEventField);

			GraphViewNode.extensionContainer.Add(eventContainer);

			eventContainers.Add(eventContainer);
		}
		void AddEvent(SerializedProperty eventToInvokeProperty) => AddEvent(null, eventToInvokeProperty);

		/**
			If the given node view represents the node that the InvokerNode
			underlying this InvokerNodeView will evaluate next, this function
			returns the output Port of this InvokerNodeView.

			Otherwise, it returns an empty Enumerable.
		*/
		override public IEnumerable<Port> GetConnectedOutputPorts(INodeView<Node.IRecord<Node>, Node> nextNodeView)
		{
			if (nextNodeView == null)
				throw new ArgumentNullException(nameof(nextNodeView));

			if (ReadOnly != nextNodeView.ReadOnly)
				throw new ArgumentException
				(
					nameof(nextNodeView),
					nameof(nextNodeView) + " has a Read-Only status different from this Invoker Node View."
				);

			if
			(
				(ReadOnly && nextNodeView.RepresentsNode(Node.NextNode)) ||
				(
					!ReadOnly &&
					nextNodeView.RepresentsNodeProperty(NodeProperty.FindPropertyRelative(nameof(InvokerNode.NextNode)))
				)
			)
				return new List<Port>() { nextNodePort };

			return new List<Port>();
		}

		/**
			Modifies the serialized InvokerNode underlying this InvokerNodeView
			so that the next node it evaluates will be the one underlying the
			given node view.

			@param outputPort Irrelevant, since an InvokerNodeView will only
			ever have one output port.
		*/
		override public void SetConnectionForOutputPort
			(Port outputPort, INodeView<Node.IRecord<Node>, Node> nextNodeView)
		{
			if (ReadOnly)
				throw ReadOnlyException;

			if (outputPort == null)
				throw new ArgumentNullException(nameof(outputPort));

			if (outputPort != nextNodePort)
				throw new ArgumentException
				(
					nameof(outputPort),
					nameof(outputPort) + " does not belong to this Invoker Node View."
				);

			if (nextNodeView == null)
				throw new ArgumentNullException(nameof(nextNodeView));

			if (ReadOnly != nextNodeView.ReadOnly)
				throw new ArgumentException
				(
					nameof(nextNodeView),
					nameof(nextNodeView) + " has a Read-Only status different from this Invoker Node View."
				);

			nextNodeView.SetManagedReferenceToNodeProperty
				(NodeProperty.FindPropertyRelative(nameof(InvokerNode.NextNode)));
		}

		/**
			Modifies the serialized InvokerNode underlying this InvokerNodeView
			so that it does not go on to evaluate another node.

			@param outputPort Irrelevant, since an InvokerNodeView will only
			ever have one output port.
		*/
		override public void ClearConnectionForOutputPort(Port outputPort)
		{
			if (ReadOnly)
				throw ReadOnlyException;

			if (outputPort == null)
				throw new ArgumentNullException(nameof(outputPort));

			if (outputPort != nextNodePort)
				throw new ArgumentException
				(
					nameof(outputPort),
					nameof(outputPort) + " does not belong to this Invoker Node View."
				);

			NodeProperty.FindPropertyRelative(nameof(InvokerNode.NextNode)).SetManagedReferenceNull();

			NodeProperty.serializedObject.ApplyModifiedProperties();
		}
	}
}