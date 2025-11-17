// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
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
		A listener for SandboxEvents that can reside in the asset folder
	*/
#if UNITY_EDITOR
	[CreateAssetMenu(fileName = "Event Listener", menuName = "NAWCAD CREATIVE Lab/Sandbox Assets/Event Listener")]
#endif
	public class AssetListener : ScriptableObject, Listener
	{
		/**
			The SandboxEvent being listened for
		*/
		[field: SerializeField] SandboxEvent EventToListenFor	= null;
		public					SandboxEvent Event				{ get { return EventToListenFor; } }

		/**
			The Object that is listenening, returns this
		*/
		public UnityEngine.Object ListeningObject { get { return this; } }

		/**
			If enabled, the target argument of the event being listened to will
			be passed along to whatever actions are set in this listener.

			Disabled by default.
		*/
		[field: SerializeField] bool TargetPassThrough = false;

		/**
			If this list is null, the listener will work normally
			
			If this list is not null, the listener will only take action from
			the SandboxEvent if the target of the invocation is in the list
		*/
		[field: SerializeField] List<UnityEngine.Object> RestrictToTargets = new List<UnityEngine.Object>();

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
		[field: SerializeField] UnityEvent Action = null;
		
		/**
			The action that will be taken if TargetPassThrough is true
		*/
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

		/**
			The SandboxEvent that will be invoked as an action if LinkEvent is
			true
		*/
		[field: SerializeField] SandboxEvent LinkedEvent = null;

#if UNITY_EDITOR
		/**
			A custom editor for AssetListener.
		*/
		[type: CustomEditor(typeof(AssetListener))]
		class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				serializedObject.Update();

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AssetListener.EventToListenFor)));
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AssetListener.LinkEvent)));

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AssetListener.TargetPassThrough)));
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AssetListener.RestrictToTargets)));

				if (serializedObject.FindProperty(nameof(AssetListener.LinkEvent)).boolValue)
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AssetListener.LinkedEvent)));
				else
				{
					if (serializedObject.FindProperty(nameof(AssetListener.TargetPassThrough)).boolValue)
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AssetListener.ActionWithTarget)));
					else
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AssetListener.Action)));
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

		bool TargetPassesFilter(UnityEngine.Object target)
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
