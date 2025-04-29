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
		
		public ListenerNode ListenerNode
		{
			get
			{
				if (ReadOnly)
					return Node as ListenerNode;
				
				return NodeProperty.managedReferenceValue as ListenerNode;
			}
		}

		readonly List<(SerializedProperty, VisualElement, Port)> editableEventInfoList;

		readonly List<(EventToListenForWithBranch, VisualElement, Port)> readOnlyEventInfoList;

		public TreeGraphViewListenerNode(TreeGraphView treeGraphView, ListenerNode listenerNode, bool active=false)
			: base(treeGraphView, listenerNode, active? "Listener (Active)" : "Listener")
		{
			editableEventInfoList = null;

			readOnlyEventInfoList = new List<(EventToListenForWithBranch, VisualElement, Port)>();

			NextNodePort =
				InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
			
			NextNodePort.SetEnabled(false);
			
			setup();
		}

		public TreeGraphViewListenerNode(TreeGraphView treeGraphView, SerializedProperty listenerProperty)
			: base(treeGraphView, listenerProperty, "Listener")
		{
			if (!(listenerProperty.ManagedReferenceIsOfType(typeof(ListenerNode))))
				throw new ArgumentException
				(
					nameof(listenerProperty),
					nameof(listenerProperty) + " does not represent a Behavior Tree Listener Node."
				);
			
			editableEventInfoList = new List<(SerializedProperty, VisualElement, Port)>();

			readOnlyEventInfoList = null;
			
			NextNodePort =
				InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
			
			setup();
		}

		void setup()
		{
			NextNodePort.portName = null;
			outputContainer.Add(NextNodePort);

			Toggle completeAfterFirstEventToggle = new Toggle();
			completeAfterFirstEventToggle.text = "Complete On First Event";
			
			Toggle branchOnCompletionToggle = new Toggle();
			branchOnCompletionToggle.text = "Branch On Completion";

			mainContainer.Add(branchOnCompletionToggle);
			mainContainer.Add(completeAfterFirstEventToggle);

			branchOnCompletionToggle.PlaceInFront(titleContainer);
			completeAfterFirstEventToggle.PlaceInFront(branchOnCompletionToggle);

			if (ReadOnly)
			{
				completeAfterFirstEventToggle.value = ListenerNode.CompleteOnFirstEvent;
				completeAfterFirstEventToggle.SetEnabled(false);
				
				branchOnCompletionToggle.value = ListenerNode.BranchOnCompletion;
				branchOnCompletionToggle.SetEnabled(false);
			}

			else
			{
				completeAfterFirstEventToggle.BindProperty
					(NodeProperty.FindPropertyRelative(nameof(ListenerNode.CompleteOnFirstEvent)));
				
				branchOnCompletionToggle.BindProperty
					(NodeProperty.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion)));
				
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
				extensionContainer.Add(addEventButton);
			}
			
			RefreshEvents();
			
			RefreshExpandedState();
		}

		void RefreshEvents()
		{
			if (ReadOnly)
			{
				foreach((EventToListenForWithBranch, VisualElement, Port) eventInfo in readOnlyEventInfoList)
					eventInfo.Item2.RemoveFromHierarchy();
				
				readOnlyEventInfoList.Clear();

				foreach (EventToListenForWithBranch eventToListenFor in ListenerNode.EventsToListenFor)
					AddEvent(eventToListenFor);
			}

			else
			{
				foreach ((SerializedProperty, VisualElement, Port) eventInfo in editableEventInfoList)
					eventInfo.Item2.RemoveFromHierarchy();
				
				editableEventInfoList.Clear();

				SerializedProperty eventsToListenForProperty =
					NodeProperty.FindPropertyRelative(nameof(ListenerNode.EventsToListenFor));
				
				for (int i=0; i<eventsToListenForProperty.arraySize; i++)
					AddEvent(eventsToListenForProperty.GetArrayElementAtIndex(i));
			}
			
			treeGraphView.ReDrawEdges();
			
			RefreshExpandedState();
		}

		void AddEvent(EventToListenForWithBranch eventToListenFor)
		{
			VisualElement eventContainer = new VisualElement();
			eventContainer.style.flexDirection = FlexDirection.Row;

			(EventToListenForWithBranch, VisualElement, Port) eventInfo =
				(eventToListenFor, eventContainer, null);

			ObjectField sandboxEventField = new ObjectField();
			sandboxEventField.objectType = typeof(SandboxEvent);
			sandboxEventField.allowSceneObjects = false;
			sandboxEventField.value = eventToListenFor.Event;
			sandboxEventField.SetEnabled(false);

			eventContainer.Add(sandboxEventField);
			
			if (ListenerNode.BranchOnCompletion)
			{
				Port port =
					InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
				port.portName = null;
				port.SetEnabled(false);
				eventContainer.Add(port);
				eventInfo.Item3 = port;
				outputContainer.Add(eventContainer);
			}

			else
				extensionContainer.Add(eventContainer);
			
			readOnlyEventInfoList.Add(eventInfo);
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

			(SerializedProperty, VisualElement, Port) eventInfo =
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
				(eventToListenForProperty.FindPropertyRelative(nameof(EventToListenForWithBranch.Event)));
			eventContainer.Add(sandboxEventField);
			
			if (NodeProperty.FindPropertyRelative(nameof(ListenerNode.BranchOnCompletion)).boolValue)
			{
				Port port =
					InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
				port.portName = null;
				eventContainer.Add(port);
				eventInfo.Item3 = port;
				outputContainer.Add(eventContainer);
			}

			else
				extensionContainer.Add(eventContainer);
			
			editableEventInfoList.Add(eventInfo);
		}

		public SerializedProperty GetEventToListenForProperty(Port port)
		{
			if (ReadOnly)
				throw new InvalidOperationException
				(
					"Something tried to get the Serialized Property for an Event To Listen For " + 
					"on a Read Only Tree Graph View Listener Node"
				);
			
			if (port == null)
				throw new ArgumentNullException(nameof(port));
			
			foreach ((SerializedProperty, VisualElement, Port) eventInfo in editableEventInfoList)
				if (eventInfo.Item3 == port)
					return eventInfo.Item1;
			
			throw new ArgumentException(nameof(port), "Port was not found in this node.");
		}

		public Port GetPort(SerializedProperty eventToListenForProperty)
		{
			if (ReadOnly)
				throw new InvalidOperationException
				(
					"Something tried to get the Port for an Event To Listen For " + 
					"using a Serialized Property reference " +
					"on a Read Only Tree Graph View Listener Node"
				);
			
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
			
			foreach ((SerializedProperty, VisualElement, Port) eventInfo in editableEventInfoList)
				if (eventInfo.Item1.managedReferenceId == eventToListenForProperty.managedReferenceId)
					return eventInfo.Item3;
			
			throw new ArgumentException(nameof(eventToListenForProperty), "Event was not found in this node.");
		}

		public Port GetPort(EventToListenForWithBranch eventToListenFor)
		{
			if (!ReadOnly)
				throw new InvalidOperationException
				(
					"Something tried to get the Port for an Event To Listen For " + 
					"using an Event To Listen For object " +
					"on an editable Tree Graph View Listener Node"
				);
			
			if (eventToListenFor == null)
				throw new ArgumentNullException(nameof(eventToListenFor));
			
			foreach ((EventToListenForWithBranch, VisualElement, Port) eventInfo in readOnlyEventInfoList)
				if (eventInfo.Item1.Equals(eventToListenFor))
					return eventInfo.Item3;
			
			throw new ArgumentException(nameof(eventToListenFor), "Event was not found in this node.");
		}
	}
}