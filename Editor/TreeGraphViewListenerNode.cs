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
	public class TreeGraphViewListenerNode : TreeGraphViewNode
	{
		public readonly Port NextNodePort;

		List<(SerializedProperty, VisualElement, Port)> bindings = new List<(SerializedProperty, VisualElement, Port)>();

		public TreeGraphViewListenerNode(TreeGraphView treeGraphView, SerializedProperty listenerProperty)
			: base(treeGraphView, listenerProperty, "Listener")
		{
			if (!(listenerProperty.ManagedReferenceIsOfType(typeof(ListenerNode))))
				throw new ArgumentException
				(
					nameof(listenerProperty),
					nameof(listenerProperty) + " does not represent a Behavior Tree Listener Node."
				);
			
			NextNodePort =
				InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
			NextNodePort.portName = null;
			outputContainer.Add(NextNodePort);

			Toggle completeAfterFirstEventToggle = new Toggle();
			completeAfterFirstEventToggle.text = "Complete On First Event";
			completeAfterFirstEventToggle.BindProperty
				(listenerProperty.FindPropertyRelative(nameof(ListenerNode.CompleteOnFirstEvent)));
			
			Toggle branchOnCompletionToggle = new Toggle();
			branchOnCompletionToggle.text = "Branch On Completion";
			branchOnCompletionToggle.BindProperty
				(listenerProperty.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion)));
			branchOnCompletionToggle.RegisterValueChangedCallback
			(
				(ChangeEvent<bool> changeEvent) =>
				{
					NextNodePort.SetEnabled(!changeEvent.newValue);

					completeAfterFirstEventToggle.SetEnabled(!changeEvent.newValue);

					RefreshEvents();
				}
			);

			Button addEventButton = new Button();
			addEventButton.text = "Add Event";
			addEventButton.clicked += () => AddEvent();

			mainContainer.Add(branchOnCompletionToggle);
			mainContainer.Add(completeAfterFirstEventToggle);

			branchOnCompletionToggle.PlaceInFront(titleContainer);
			completeAfterFirstEventToggle.PlaceInFront(branchOnCompletionToggle);

			extensionContainer.Add(addEventButton);
			
			RefreshEvents();
			
			RefreshExpandedState();
		}

		void RefreshEvents()
		{
			foreach ((SerializedProperty, VisualElement, Port) binding in bindings)
				binding.Item2.RemoveFromHierarchy();
			
			bindings.Clear();
			
			SerializedProperty eventsToListenForProperty =
				NodeProperty.FindPropertyRelative(nameof(ListenerNode.EventsToListenFor));
			
			for (int i=0; i<eventsToListenForProperty.arraySize; i++)
				AddEvent(eventsToListenForProperty.GetArrayElementAtIndex(i));
			
			TreeGraphView.ReDrawEdges();
			
			RefreshExpandedState();
		}

		void AddEvent(SerializedProperty eventToListenForProperty = null)
		{
			if (eventToListenForProperty == null)
			{
				SerializedProperty eventsToListenForProperty =
					NodeProperty.FindPropertyRelative(nameof(ListenerNode.EventsToListenFor));
				
				eventToListenForProperty = eventsToListenForProperty.GetArrayElementAtIndex
					(eventsToListenForProperty.arraySize++);
				
				eventToListenForProperty.managedReferenceValue = new EventToListenForWithBranch();

				NodeProperty.serializedObject.ApplyModifiedProperties();
			}
			
			VisualElement eventContainer = new VisualElement();
			eventContainer.style.flexDirection = FlexDirection.Row;

			(SerializedProperty, VisualElement, Port) binding =
				(eventToListenForProperty, eventContainer, null);

			Button removeButton = new Button();
			removeButton.text = "-";
			removeButton.clicked += () =>
			{
				eventToListenForProperty.DeleteCommand();

				NodeProperty.serializedObject.ApplyModifiedProperties();

				RefreshEvents();
			};
			eventContainer.Add(removeButton);

			ObjectField sandboxEventField = new ObjectField();
			sandboxEventField.objectType = typeof(SandboxEvent);
			sandboxEventField.allowSceneObjects = false;
			sandboxEventField.BindProperty
				(eventToListenForProperty.FindPropertyRelative(nameof(EventToListenFor.Event)));
			eventContainer.Add(sandboxEventField);
			
			if (NodeProperty.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion)).boolValue)
			{
				Port port =
					InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
				port.portName = null;
				eventContainer.Add(port);
				binding.Item3 = port;
				outputContainer.Add(eventContainer);
			}

			else
				extensionContainer.Add(eventContainer);
			
			bindings.Add(binding);
		}

		public SerializedProperty GetEventToListenForProperty(Port port)
		{
			if (port == null)
				throw new ArgumentNullException(nameof(port));
			
			foreach ((SerializedProperty, VisualElement, Port) binding in bindings)
				if (binding.Item3 == port)
					return binding.Item1;
			
			throw new ArgumentException(nameof(port), "Port was not found in this node.");
		}

		public Port GetPort(SerializedProperty eventToListenForProperty)
		{
			if (eventToListenForProperty == null)
				throw new ArgumentNullException(nameof(eventToListenForProperty));
			
			if
			(
				!
				(
					eventToListenForProperty.IsManagedReference() &&
					eventToListenForProperty.ManagedReferenceIsOfType(typeof(EventToListenForWithBranch))
				)
			)
				throw new ArgumentException
				(
					nameof(eventToListenForProperty),
					nameof(eventToListenForProperty) + " does not represent an Event To Listen For With Branch."
				);
			
			foreach ((SerializedProperty, VisualElement, Port) binding in bindings)
				if (binding.Item1.managedReferenceId == eventToListenForProperty.managedReferenceId)
					return binding.Item3;
			
			throw new ArgumentException(nameof(eventToListenForProperty), "Event was not found in this node.");
		}
	}
}