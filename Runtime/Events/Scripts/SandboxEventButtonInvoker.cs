using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets
{
	/**
		This class inherits from the Button class in order to invoke a
		SandboxEvent (or multiple) whenever the button is pressed.
	*/
	public class SandboxEventButtonInvoker : Button
	{
		/**
			Indicates whether the button should invoke a single SandboxEvent or
			Multiple, when pressed.
		*/
		[field: SerializeField]
		private bool MultipleEvents = false;
		
		/**
			The SandboxEvent to invoke when the button is pressed, if
			MultipleEvents is false.
		*/
		[field: SerializeField]
		private SandboxEvent Event;

		/**
			A list of SandboxEvents to invoke when the button is pressed, if
			MultipleEvents is true.
		*/
		[field: SerializeField]
		private List<SandboxEvent> Events;

#if UNITY_EDITOR
		/**
			Custom editor for the SandboxEvent Button Invoker.

			Shows the default Button script editor, but adds the SandboxEvent
			field (or list of fields if 'Multiple Events' is checked).
		*/
		[CustomEditor(typeof(SandboxEventButtonInvoker))]
		private class Editor : UnityEditor.UI.ButtonEditor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				SandboxEventButtonInvoker sandboxEventButtonInvoker = target as SandboxEventButtonInvoker;

				serializedObject.Update();

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventButtonInvoker.MultipleEvents)));

				if (sandboxEventButtonInvoker.MultipleEvents)
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventButtonInvoker.Events)));
				else
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEventButtonInvoker.Event)));

				serializedObject.ApplyModifiedProperties();
			}
		}
#endif

		private List<SandboxEvent> registeredEvents = null;

		protected override void OnEnable()		{ base.OnEnable();		ReRegister();	}
		protected override void Start()			{ base.OnEnable();		ReRegister();	}
		protected override void OnValidate()	{ base.OnValidate();	ReRegister();	}
		
		protected override void OnDisable()		{ base.OnDisable();		UnRegister();	}
		protected override void OnDestroy()		{ base.OnDestroy();		UnRegister();	}

		private void ReRegister()
		{
			UnRegister();

			if (Application.isPlaying && isActiveAndEnabled)
			{
				registeredEvents = new List<SandboxEvent>();
				
				if (MultipleEvents)
				{
					foreach (SandboxEvent sandboxEvent in Events)
						if (sandboxEvent != null)
							registeredEvents.Add(sandboxEvent);
				}

				else if (Event != null)
					registeredEvents.Add(Event);
				
				if (registeredEvents.Count > 0)
					foreach (SandboxEvent sandboxEvent in registeredEvents)
						sandboxEvent.AddInvoker(gameObject);
				
				else
					registeredEvents = null;
			}
		}

		private void UnRegister()
		{
			if (registeredEvents != null)
			{
				foreach (SandboxEvent sandboxEvent in registeredEvents)
					sandboxEvent.DropInvoker(gameObject);
				
				registeredEvents = null;
			}
		}

		public override void OnPointerClick(PointerEventData eventData)
		{
			base.OnPointerClick(eventData);

			if (eventData.button==PointerEventData.InputButton.Left && registeredEvents!=null)
				foreach (SandboxEvent sandboxEvent in registeredEvents)
					sandboxEvent.Invoke(gameObject);
		}
	}
}