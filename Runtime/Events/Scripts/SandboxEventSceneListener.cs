using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets
{
	/**
		A Sandbox Event Listener that can be attached to a GameObject.
	*/
	public class SandboxEventSceneListener : MonoBehaviour
	{
		/**
			If enabled, the user will be able to set a list of Events to be
			listened to, instead of just one.

			Any one of these events being invoked will trigger whatever actions
			are set in this listener.

			Disabled by default.
		*/
		[field: SerializeField]
		private bool MultipleEvents = false;

		[field: SerializeField]
		private SandboxEvent Event;
		
		[field: SerializeField]
		private List<SandboxEvent> Events;
		
		/**
			If enabled, the target argument of the event being listened to will
			be passed along to whatever actions are set in this listener.

			Disabled by default.
		*/
		[field: SerializeField]
		private bool TargetPassThrough = false;

		[field: SerializeField]
		private List<UnityEngine.Object> RestrictToTargets;

		[field: SerializeField]
		private UnityEvent Action;
		
		[field: SerializeField]
		private UnityEvent<UnityEngine.Object> ActionWithTarget;

		/**
			If enabled, the user will be allowed to set a second event that will
			be invoked automatically after the event being listened to is
			invoked, essentially linking the two events together.

			If disabled, the user will be allowed to set actions to be taken
			when the event is invoked through a UnityEvent interface in the
			inspector.

			Disabled by default.
		*/
		[field: SerializeField]
		private bool LinkEvent = false;

		[field: SerializeField]
		private SandboxEvent LinkedEvent;

#if UNITY_EDITOR
		/**
			A custom editor for the SandboxEventSceneListener.
		*/
		[CustomEditor(typeof(SandboxEventSceneListener))]
		private class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				SandboxEventSceneListener listener = target as SandboxEventSceneListener;
				
				serializedObject.Update();
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventSceneListener.MultipleEvents)));
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventSceneListener.LinkEvent)));

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventSceneListener.TargetPassThrough)));

				if (listener.MultipleEvents)
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventSceneListener.Events)));
				else
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventSceneListener.Event)));
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventSceneListener.RestrictToTargets)));

				if (listener.LinkEvent)
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventSceneListener.LinkedEvent)));
				else
				{
					if (listener.TargetPassThrough)
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventSceneListener.ActionWithTarget)));
					else
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventSceneListener.Action)));
				}
				
				serializedObject.ApplyModifiedProperties();
			}
		}
#endif

		private SandboxEvent linkedEvent = null;

		private List<SandboxEventDelegateListener> listeners = new List<SandboxEventDelegateListener>();

		private bool listening = false;

		void Start()		=> ReRegister();
		void OnValidate()	=> ReRegister();
		void OnEnable()		=> ReRegister();

		void OnDisable()	=> UnRegister();
		void OnDestroy()	=> UnRegister();

		private void ReRegister()
		{
			UnRegister();

			SandboxEventDelegateListener listener = null;

			if (Application.isPlaying && isActiveAndEnabled)
			{
				if (MultipleEvents)
				{
					if (Events!=null && Events.Count!=0)
					{
						foreach (SandboxEvent sandboxEvent in Events)
						{
							if (sandboxEvent!=null)
							{
								listener = CreateListener(sandboxEvent);
								
								listeners.Add(listener);

								listener.Enable();

								listening = true;
							}
						}
					}
				}

				else if (Event!=null)
				{
					listener = CreateListener(Event);
					
					listeners.Add(listener);

					listener.Enable();

					listening = true;
				}

				if (listening && LinkEvent)
				{
					linkedEvent = LinkedEvent;
					linkedEvent.AddInvoker(gameObject);
				}
			}
		}

		private void UnRegister()
		{
			if (listening==true)
			{
				foreach (SandboxEventDelegateListener listener in listeners)
					listener.Disable();
				
				listeners.Clear();

				if (linkedEvent!=null)
				{
					linkedEvent.DropInvoker(gameObject);
					linkedEvent = null;
				}

				listening = false;
			}
		}

		private SandboxEventDelegateListener CreateListener(SandboxEvent sandboxEvent)
		{
			if (LinkEvent)
			{
				if (TargetPassThrough)
					return new SandboxEventDelegateListener
					(
						sandboxEvent, gameObject,
						(target)=> { LinkedEvent.Invoke(gameObject, target); return false; },
						RestrictToTargets
					);
				
				else
					return new SandboxEventDelegateListener
					(
						sandboxEvent, gameObject,
						(target)=> { LinkedEvent.Invoke(gameObject); return false; },
						RestrictToTargets
					);
			}

			else
			{
				if (TargetPassThrough)
					return new SandboxEventDelegateListener
					(
						sandboxEvent, gameObject,
						(target)=> { ActionWithTarget.Invoke(target); return false; },
						RestrictToTargets
					);
				
				else
					return new SandboxEventDelegateListener
					(
						sandboxEvent, gameObject,
						(target)=> { Action.Invoke(); return false; },
						RestrictToTargets
					);
			}
		}
	}
}