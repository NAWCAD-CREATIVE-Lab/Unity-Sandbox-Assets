using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	/**
		This script allows for programming a linear interactive sequence by
		breaking it into steps with Sandbox Events.

		Each step consists of a list of Sandbox Events to listen for and a list
		of Sandbox Events to invoke after the first list has been satisfied.

		The first list can be satisfied either by having all the contained
		events invoked, or having any one of the contained events invoked. The mode
		can be swapped individually for each step.

		The events to listen to can each optionally be filtered by targets,
		and the events to invoke can optionally include targets. 
	*/
	public class Sequence : MonoBehaviour
	{
		[field: SerializeField]
		private List<Step> Steps;

		public bool Registered { get; private set; }

		public int CurrentStep { get; private set; }

		private ListenerNode currentNode = null;

		private Dictionary<ListenerNode, Dictionary<EventToListenFor, bool>> registeredListenerStatusIndex =
			new Dictionary<ListenerNode, Dictionary<EventToListenFor, bool>>();

		private List<SandboxEvent> registeredEventsToInvoke = new List<SandboxEvent>();

		private List<DelegateListener> registeredEventListeners = new List<DelegateListener>();

#if UNITY_EDITOR
		private event Action repaintEditor;
		
		[CustomEditor(typeof(Sequence))]
		public class Editor : UnityEditor.Editor
		{
			void OnEnable() { (target as Sequence).repaintEditor += Repaint; }
			void OnDisable() { (target as Sequence).repaintEditor -= Repaint; }
			
			public override void OnInspectorGUI()
			{
				serializedObject.Update();

				Sequence sequence = target as Sequence;

				if (Application.isPlaying && sequence.Registered)
				{
					if (sequence.CurrentStep == 0)
						EditorGUILayout.LabelField("Sequence Completed");
					else
						EditorGUILayout.LabelField
							("Current Step: " + sequence.CurrentStep);
					
					EditorGUILayout.Space(20);
				}

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Steps)));

				serializedObject.ApplyModifiedProperties();
			}
		}
#endif

		void Start()		=> ReRegister();
		void OnValidate()	=> ReRegister();
		void OnEnable()		=> ReRegister();

		void OnDisable()	=> UnRegister();
		void OnDestroy()	=> UnRegister();

		private void ReRegister()
		{
			UnRegister();

			if (Application.isPlaying && isActiveAndEnabled && Steps!=null)
			{
				try
				{
					currentNode = Step.CleanCloneListAsNodes(Steps);

					if (currentNode == null)
						CurrentStep = 0;

					else
					{
						Dictionary<EventToListenFor, Dictionary<ListenerNode, EventToListenFor>> delegateListenerSetupInfo =
							new Dictionary<EventToListenFor, Dictionary<ListenerNode, EventToListenFor>>();

						ListenerNode listenerNodeIterator = currentNode;
						while (listenerNodeIterator != null)
						{
							registeredListenerStatusIndex.Add(listenerNodeIterator, new Dictionary<EventToListenFor, bool>());

							foreach (EventToListenForWithBranch eventToListenForWithBranch in listenerNodeIterator.EventsToListenFor)
							{
								EventToListenFor eventToListenFor = new EventToListenFor(eventToListenForWithBranch);

								registeredListenerStatusIndex[listenerNodeIterator][eventToListenFor] = false;

								foreach (EventToListenFor eventToListenForSegment in eventToListenFor.TargetSegements)
								{
									if (!delegateListenerSetupInfo.ContainsKey(eventToListenForSegment))
										delegateListenerSetupInfo.Add(eventToListenForSegment, new Dictionary<ListenerNode, EventToListenFor>());
									
									delegateListenerSetupInfo[eventToListenForSegment].Add(listenerNodeIterator, eventToListenFor);
								}
							}

							InvokerNode invokerNode = listenerNodeIterator.NextNode as InvokerNode;

							foreach (EventToInvoke eventToInvoke in invokerNode.EventsToInvoke)
							{
								eventToInvoke.Event.AddInvoker(gameObject);

								registeredEventsToInvoke.Add(eventToInvoke.Event);
							}

							listenerNodeIterator = invokerNode.NextNode as ListenerNode;
						}

						foreach (EventToListenFor eventToListenFor in delegateListenerSetupInfo.Keys)
						{
							Dictionary<ListenerNode, EventToListenFor> listeningNodeInfoIndex = delegateListenerSetupInfo[eventToListenFor];
							
							registeredEventListeners.Add
							(
								new DelegateListener
								(
									eventToListenFor.Event,
									gameObject,
									(target) => HandleEvent(listeningNodeInfoIndex),
									eventToListenFor.TargetFilter
								)
							);
						}

						foreach (DelegateListener registeredEventListener in registeredEventListeners)
							registeredEventListener.Enable();

						CurrentStep = 1;
					}

					Registered = true;
				}

				catch (Exception e)
				{
					UnRegister();

					throw e;
				}
			}
		}

		private void UnRegister()
		{
			CurrentStep = -1;

			currentNode = null;

			registeredListenerStatusIndex.Clear();
			
			foreach (SandboxEvent sandboxEvent in registeredEventsToInvoke)
				sandboxEvent.DropInvoker(gameObject);
			registeredEventsToInvoke.Clear();

			foreach (DelegateListener delegateListener in registeredEventListeners)
				delegateListener.Disable();
			registeredEventListeners.Clear();

			Registered = false;
		}

		private bool HandleEvent(Dictionary<ListenerNode, EventToListenFor> listeningNodeInfoIndex)
		{
			if (currentNode!=null && listeningNodeInfoIndex.ContainsKey(currentNode))
			{
				EventToListenFor eventToListenFor = listeningNodeInfoIndex[currentNode];
				
				registeredListenerStatusIndex[currentNode][eventToListenFor] = true;

				if (!currentNode.CompleteOnFirstEvent)
					foreach
						(EventToListenFor eventToListenForIterator in registeredListenerStatusIndex[currentNode].Keys)
						if (!registeredListenerStatusIndex[currentNode][eventToListenForIterator])
							return false;
				
				InvokerNode invokerNode = currentNode.NextNode as InvokerNode;

				foreach (EventToInvoke eventToInvoke in invokerNode.EventsToInvoke)
					eventToInvoke.Event.Invoke(gameObject, eventToInvoke.Target);
				
				currentNode = invokerNode.NextNode as ListenerNode;
				
				if (currentNode != null)
					CurrentStep++;
				
				else CurrentStep = 0;

#if UNITY_EDITOR
				if (repaintEditor != null)
					repaintEditor();
#endif
			}

			return false;
		}
	}
}