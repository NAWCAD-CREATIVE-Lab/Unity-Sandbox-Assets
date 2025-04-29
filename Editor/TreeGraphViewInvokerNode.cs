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
	public class TreeGraphViewInvokerNode : TreeGraphViewNode
	{
		public readonly Port NextNodePort;
		
		public InvokerNode InvokerNode
		{
			get
			{
				if (ReadOnly)
					return Node as InvokerNode;
				
				return NodeProperty.managedReferenceValue as InvokerNode;
			}
		}

		List<VisualElement> eventContainers = new List<VisualElement>();

		void setup()
		{
			NextNodePort.portName = null;
			outputContainer.Add(NextNodePort);

			if (!ReadOnly)
			{
				Button addEventButton = new Button();
				addEventButton.text = "Add Event";
				addEventButton.clicked += () => AddEvent();
				extensionContainer.Add(addEventButton);
			}

			RefreshEvents();

			RefreshExpandedState();
		}

		public TreeGraphViewInvokerNode(TreeGraphView treeGraphView, InvokerNode invokerNode)
			: base(treeGraphView, invokerNode, "Invoker")
		{
			NextNodePort =
				InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
			
			NextNodePort.SetEnabled(false);
			
			setup();
		}
		
		public TreeGraphViewInvokerNode(TreeGraphView treeGraphView, SerializedProperty invokerProperty)
			: base(treeGraphView, invokerProperty, "Invoker")
		{
			if (!(invokerProperty.ManagedReferenceIsOfType(typeof(InvokerNode))))
				throw new ArgumentException
				(
					nameof(invokerProperty),
					nameof(invokerProperty) + " does not represent a Behavior Tree Invoker Node."
				);
			
			NextNodePort =
				InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Node));
			
			setup();
		}

		void RefreshEvents()
		{
			foreach (VisualElement eventContainer in eventContainers)
				eventContainer.RemoveFromHierarchy();
			
			eventContainers.Clear();

			if (ReadOnly)
				foreach (EventToInvoke eventToInvoke in InvokerNode.EventsToInvoke)
					AddEvent(eventToInvoke);
			
			else
			{
				SerializedProperty eventsToInvokeProperty =
					NodeProperty.FindPropertyRelative(nameof(InvokerNode.EventsToInvoke));
				
				for (int i=0; i<eventsToInvokeProperty.arraySize; i++)
					AddEvent(eventsToInvokeProperty.GetArrayElementAtIndex(i));
			}
		}

		void AddEvent(EventToInvoke eventToInvoke)
		{
			VisualElement eventContainer = new VisualElement();
			eventContainer.style.flexDirection = FlexDirection.Row;

			ObjectField sandboxEventField = new ObjectField();
			sandboxEventField.objectType = typeof(SandboxEvent);
			sandboxEventField.allowSceneObjects = false;
			sandboxEventField.value = eventToInvoke.Event;
			sandboxEventField.SetEnabled(false);

			eventContainer.Add(sandboxEventField);

			extensionContainer.Add(eventContainer);

			eventContainers.Add(eventContainer);
		}

		void AddEvent(SerializedProperty eventToInvokeProperty = null)
		{
			if (eventToInvokeProperty == null)
			{
				SerializedProperty eventsToInvokeProperty =
					NodeProperty.FindPropertyRelative(nameof(InvokerNode.EventsToInvoke));
				
				eventToInvokeProperty = eventsToInvokeProperty.GetArrayElementAtIndex
					(eventsToInvokeProperty.arraySize++);
				
				eventToInvokeProperty.managedReferenceValue = new EventToInvoke();

				NodeProperty.serializedObject.ApplyModifiedProperties();
			}
			
			VisualElement eventContainer = new VisualElement();
			eventContainer.style.flexDirection = FlexDirection.Row;

			Button removePortButton = new Button();
			removePortButton.text = "-";
			removePortButton.clicked += () =>
			{
				eventToInvokeProperty.DeleteCommand();

				NodeProperty.serializedObject.ApplyModifiedProperties();

				RefreshEvents();
			};
			eventContainer.Add(removePortButton);
			
			ObjectField sandboxEventField = new ObjectField();
			sandboxEventField.objectType = typeof(SandboxEvent);
			sandboxEventField.allowSceneObjects = false;

			sandboxEventField.BindProperty(eventToInvokeProperty.FindPropertyRelative(nameof(EventToInvoke.Event)));
			
			eventContainer.Add(sandboxEventField);

			extensionContainer.Add(eventContainer);

			eventContainers.Add(eventContainer);
		}
	}
}