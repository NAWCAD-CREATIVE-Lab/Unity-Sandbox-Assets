using System;
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
		A listener for SandboxEvents that can reside in the asset folder
	*/
	[CreateAssetMenu(fileName = "Event Listener", menuName = "NAWCAD CREATIVE Lab/Sandbox Assets/Event Listener")]
	public class SandboxEventAssetListener : ScriptableObject, SandboxEventListener
	{
		/**
			The SandboxEvent being listened for
		*/
		public SandboxEvent Event { get; private set; }

		/**
			The Object that is listenening, returns this
		*/
		public UnityEngine.Object ListeningObject { get { return this; } }

		/**
			If enabled, the target argument of the event being listened to will
			be passed along to whatever actions are set in this listener.

			Disabled by default.
		*/
		[field: SerializeField]
		private bool TargetPassThrough = false;

		/**
			If this list is null, the listener will work normally
			
			If this list is not null, the listener will only take action from
			the SandboxEvent if the target of the invocation is in the list
		*/
		[field: SerializeField]
		private List<UnityEngine.Object> RestrictToTargets;

		/**
			Returns RestrictToTargets after filtering out null values and
			duplicates
		*/
		public IEnumerable<UnityEngine.Object> TargetFilter
		{
			get
			{
				List<UnityEngine.Object> tempFilter = null;
				
				if (RestrictToTargets!=null)
				{
					foreach (UnityEngine.Object obj in RestrictToTargets)
					{
						if (obj!=null)
						{
							if (tempFilter==null)
								tempFilter = new List<UnityEngine.Object>();
							
							if (!tempFilter.Contains(obj))
								tempFilter.Add(obj);
						}
					}
				}

				return tempFilter;
			}
		}
		
		/**
			The action that will be taken if TargetPassThrough is false
		*/
		[field: SerializeField]
		private UnityEvent Action;
		
		/**
			The action that will be taken if TargetPassThrough is true
		*/
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

		/**
			The SandboxEvent that will be invoked as an action if LinkEvent is
			true
		*/
		[field: SerializeField]
		private SandboxEvent LinkedEvent;

#if UNITY_EDITOR
		/**
			A custom editor for SandboxEventAssetListener.
		*/
		[CustomEditor(typeof(SandboxEventAssetListener))]
		public class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				SandboxEventAssetListener listener = target as SandboxEventAssetListener;
				
				serializedObject.Update();

				listener.Event = EditorGUILayout.ObjectField
				(
					"Event",
					listener.Event,
					typeof(SandboxEvent),
					false
				) as SandboxEvent;
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventAssetListener.LinkEvent)));

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventAssetListener.TargetPassThrough)));
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventAssetListener.RestrictToTargets)));

				if (listener.LinkEvent)
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventAssetListener.LinkedEvent)));
				else
				{
					if (listener.TargetPassThrough)
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventAssetListener.ActionWithTarget)));
					else
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventAssetListener.Action)));
				}
				
				serializedObject.ApplyModifiedProperties();
			}
		}
#endif

		public bool Invoke(UnityEngine.Object target)
		{
			if (TargetPassesFilter(target))
			{
				if (LinkEvent)
				{
					if (TargetPassThrough)
						LinkedEvent.Invoke(this, target);
					else
						LinkedEvent.Invoke(this);
				}

				else
				{
					if (TargetPassThrough)
						ActionWithTarget.Invoke(target);
					else
						Action.Invoke();
				}
			}

			return false;
		}

		private bool TargetPassesFilter(UnityEngine.Object target)
		{
			if (TargetFilter==null)
					return true;
			
			else
				foreach (UnityEngine.Object targetMatch in TargetFilter)
					if (target==targetMatch)
						return true;
			
			return false;
		}
	}
}
