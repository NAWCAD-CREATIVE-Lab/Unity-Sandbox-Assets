using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets
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
	public class SandboxEventSequence : MonoBehaviour
	{
		[field: SerializeField]
		private List<Step> Steps;
		private List<Step.RegisteredStep> registeredSteps;

		[field: SerializeField]
		private int currentStepIndex;

		private bool registered = false;

		public bool Completed
		{
			get
			{
				if (!registered)
					return false;
				
				return currentStepIndex == registeredSteps.Count;
			}
		}
		
		[type: Serializable]
		public sealed class Step
		{
			[type: Serializable]
			public sealed class EventToListenFor
			{
				[field: SerializeField]
				public SandboxEvent Event;

				[field: SerializeField]
				public List<UnityEngine.Object> TargetFilter;
			}
			
			[field: SerializeField]
			public List<EventToListenFor> EventsToListenFor;
			
			[field: SerializeField]
			public bool WaitForAnyNotAll;

			[type: Serializable]
			public sealed class EventToInvoke
			{
				[field: SerializeField]
				public SandboxEvent Event;

				[field: SerializeField]
				public UnityEngine.Object Target;
			}

			[field: SerializeField]
			public List<EventToInvoke> EventsToInvoke;

			public RegisteredStep GetRegisteredStep
				(GameObject listeningInvokingObject, Action completionAction) =>
				new RegisteredStep(this, listeningInvokingObject, completionAction);

			public sealed class RegisteredStep
			{
				private readonly bool WaitForAnyNotAll;

				private readonly GameObject listeningInvokingObject;

				private readonly Action completionAction;
				
				private readonly List<SandboxEventDelegateListener> eventListeners;

				private readonly List<(SandboxEvent, UnityEngine.Object)> eventsToInvoke;

				public bool Started { get; private set; }

				public bool Completed { get; private set; }

				public RegisteredStep(Step step, GameObject listeningInvokingObject, Action completionAction)
				{
					if (step == null)
						throw new ArgumentNullException(nameof(step));
					
					if (listeningInvokingObject == null)
						throw new ArgumentNullException(nameof(listeningInvokingObject));
					
					if (completionAction == null)
						throw new ArgumentNullException(nameof(completionAction));
					
					eventListeners = new List<SandboxEventDelegateListener>();

					eventsToInvoke = new List<(SandboxEvent, UnityEngine.Object)>();
					
					if (step.EventsToListenFor != null)
					{
						foreach (Step.EventToListenFor eventToListenFor in step.EventsToListenFor)
						{
							if (eventToListenFor != null)
							{
								int newListenerIndex = eventListeners.Count;
								
								eventListeners.Add
								(
									new SandboxEventDelegateListener
									(
										eventToListenFor.Event,
										listeningInvokingObject,
										(UnityEngine.Object target) => HandleEvent(newListenerIndex),
										eventToListenFor.TargetFilter
									)
								);
							}
						}

						foreach (Step.EventToInvoke eventToInvoke in step.EventsToInvoke)
							if (eventToInvoke!=null && eventToInvoke.Event!=null)
								eventsToInvoke.Add((eventToInvoke.Event, eventToInvoke.Target));
					}
					
					if (step.EventsToListenFor.Count == 0)
						throw new InvalidOperationException
							("A step in a Sandbox Event Sequence has no events to listen for.");
					
					WaitForAnyNotAll = step.WaitForAnyNotAll;

					this.listeningInvokingObject = listeningInvokingObject;

					this.completionAction = completionAction;
					
					Started = false;

					Completed = false;
				}

				~RegisteredStep() => UnRegister();

				public void Begin()
				{
					if (!Started && !Completed)
					{
						foreach (SandboxEventDelegateListener listener in eventListeners)
							listener.Enable();
						
						foreach((SandboxEvent, UnityEngine.Object) eventToInvoke in eventsToInvoke)
							eventToInvoke.Item1.AddInvoker(listeningInvokingObject);
						
						Started = true;
					}
				}

				private bool HandleEvent(int listenerIndex)
				{
					if (Started && !Completed)
					{
						eventListeners[listenerIndex] = null;
						
						if (WaitForAnyNotAll)
							Complete();

						else
						{
							foreach (SandboxEventDelegateListener listener in eventListeners)
								if (listener != null)
									return true;
							
							Complete();
						}

						return true;
					}

					return false;
				}

				private void Complete()
				{
					if (Started && !Completed)
					{
						foreach ((SandboxEvent, UnityEngine.Object) eventToInvoke in eventsToInvoke)
							eventToInvoke.Item1.Invoke(listeningInvokingObject, eventToInvoke.Item2);
						
						UnRegister();

						completionAction();

						Completed = true;
					}
				}

				public void UnRegister()
				{
					if (Started && !Completed)
					{
						foreach (SandboxEventDelegateListener listener in eventListeners)
							if (listener != null)
								listener.Disable();
						
						foreach ((SandboxEvent, UnityEngine.Object) eventToInvoke in eventsToInvoke)
							eventToInvoke.Item1.DropInvoker(listeningInvokingObject);

						Started = false;
					}
				}
			}
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(SandboxEventSequence))]
		public class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				serializedObject.Update();

				SandboxEventSequence sandboxEventSequence = target as SandboxEventSequence;

				if (Application.isPlaying)
				{
					if (sandboxEventSequence.Completed)
						EditorGUILayout.LabelField("Sequence Completed");
					else
						EditorGUILayout.LabelField
							("Current Step Index: " + serializedObject.FindProperty(nameof(currentStepIndex)).intValue);
					
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

		void Update()
		{
			if (registered && !Completed && registeredSteps[currentStepIndex].Started==false)
				registeredSteps[currentStepIndex].Begin();
		}

		private void ReRegister()
		{
			UnRegister();

			if (Application.isPlaying && isActiveAndEnabled && Steps!=null && Steps.Count>0)
			{
				registeredSteps = new List<Step.RegisteredStep>();

				foreach (Step step in Steps)
					registeredSteps.Add(step.GetRegisteredStep(gameObject, IncrementStep));

				currentStepIndex = 0;

				registered = true;
			}
		}

		private void UnRegister()
		{
			if (registered && !Completed && registeredSteps[currentStepIndex].Started==true)
				registeredSteps[currentStepIndex].UnRegister();
			
			registered = false;
		}

		private void IncrementStep()
		{
			if (registered)
				currentStepIndex++;
		}
	}
}