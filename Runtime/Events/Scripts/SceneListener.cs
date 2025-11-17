// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets.Events
{
	/**
		A Sandbox Event Listener that can be attached to a GameObject.
	*/
	public class SceneListener : MonoBehaviour
	{
		/**
			If enabled, the user will be able to set a list of Events to be
			listened to, instead of just one.

			Any one of these events being invoked will trigger whatever actions
			are set in this listener.

			Disabled by default.
		*/
		[field: SerializeField] bool MultipleEvents = false;

		[field: SerializeField] SandboxEvent Event = null;
		
		[field: SerializeField] List<SandboxEvent> Events = new List<SandboxEvent>();
		
		/**
			If enabled, the target argument of the event being listened to will
			be passed along to whatever actions are set in this listener.

			Disabled by default.
		*/
		[field: SerializeField] bool TargetPassThrough = false;

		[field: SerializeField] List<UnityEngine.Object> RestrictToTargets = new List<UnityEngine.Object>();

		[field: SerializeField] UnityEvent Action = null;
		
		[field: SerializeField] UnityEvent<UnityEngine.Object> ActionWithTarget = null;

		/**
			If enabled, the user will be allowed to set a second event that will
			be invoked automatically after the event being listened to is
			invoked, essentially linking the two events together.

			If disabled, the user will be allowed to set actions to be taken
			when the event is invoked through a UnityEvent interface in the
			inspector.

			Disabled by default.
		*/
		[field: SerializeField] bool LinkEvent = false;

		[field: SerializeField]	SandboxEvent LinkedEvent			= null;
								SandboxEvent registeredLinkedEvent	= null;

		private List<DelegateListener> registeredListeners = new List<DelegateListener>();

#if UNITY_EDITOR
		/**
			A custom editor for the SceneListener.
		*/
		[type: CustomEditor(typeof(SceneListener))]
		class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				serializedObject.Update();
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SceneListener.MultipleEvents)));
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SceneListener.LinkEvent)));

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SceneListener.TargetPassThrough)));

				if (serializedObject.FindProperty(nameof(SceneListener.MultipleEvents)).boolValue)
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SceneListener.Events)));
				else
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SceneListener.Event)));
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SceneListener.RestrictToTargets)));

				if (serializedObject.FindProperty(nameof(SceneListener.LinkEvent)).boolValue)
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SceneListener.LinkedEvent)));
				else
				{
					if (serializedObject.FindProperty(nameof(SceneListener.TargetPassThrough)).boolValue)
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SceneListener.ActionWithTarget)));
					else
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SceneListener.Action)));
				}
				
				serializedObject.ApplyModifiedProperties();
			}
		}
#endif

		void OnEnable() => ReRegister();
		void Start()	=> ReRegister();

#if UNITY_EDITOR
		void OnValidate() => ReRegister();
#endif
		
		void OnDisable()	=> UnRegister();
		void OnDestroy()	=> UnRegister();

		void ReRegister()
		{
			UnRegister();

			if (Application.isPlaying && isActiveAndEnabled)
			{
				DelegateListener registeredListener = null;
				
				if (MultipleEvents)
				{
					if (Events.Count > 0)
					{
						foreach (SandboxEvent sandboxEvent in Events)
						{
							if (sandboxEvent!=null)
							{
								registeredListener = CreateListener(sandboxEvent);
								registeredListener.Enable();

								registeredListeners.Add(registeredListener);
							}
						}
					}
				}

				else if (Event != null)
				{
					registeredListener = CreateListener(Event);
					registeredListener.Enable();

					registeredListeners.Add(registeredListener);
				}

				if (LinkEvent && LinkedEvent != null)
				{
					registeredLinkedEvent = LinkedEvent;
					registeredLinkedEvent.AddInvoker(gameObject);
				}
			}
		}

		private void UnRegister()
		{
			foreach (DelegateListener registeredListener in registeredListeners)
				registeredListener.Disable();
			
			registeredListeners.Clear();

			if (registeredLinkedEvent!=null)
			{
				registeredLinkedEvent.DropInvoker(gameObject);
				registeredLinkedEvent = null;
			}
		}

		private DelegateListener CreateListener(SandboxEvent sandboxEvent)
		{
			if (LinkEvent)
			{
				if (TargetPassThrough)
					return new DelegateListener
					(
						sandboxEvent, gameObject,
						(target)=> { LinkedEvent.Invoke(gameObject, target); return false; },
						RestrictToTargets
					);
				
				else
					return new DelegateListener
					(
						sandboxEvent, gameObject,
						(target)=> { LinkedEvent.Invoke(gameObject); return false; },
						RestrictToTargets
					);
			}

			else
			{
				if (TargetPassThrough)
					return new DelegateListener
					(
						sandboxEvent, gameObject,
						(target)=> { ActionWithTarget.Invoke(target); return false; },
						RestrictToTargets
					);
				
				else
					return new DelegateListener
					(
						sandboxEvent, gameObject,
						(target)=> { Action.Invoke(); return false; },
						RestrictToTargets
					);
			}
		}
	}
}