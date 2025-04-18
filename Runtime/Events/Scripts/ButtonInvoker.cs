using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets.Events
{
	/**
		This class inherits from the Button class in order to invoke a
		SandboxEvent (or multiple) whenever the button is pressed.
	*/
	public class ButtonInvoker : Button
	{
		/**
			Indicates whether the button should invoke a single SandboxEvent or
			Multiple, when pressed.
		*/
		[field: SerializeField] bool MultipleEvents				= false;
								bool registeredMultipleEvents	= false;
		
		/**
			The SandboxEvent to invoke when the button is pressed, if
			MultipleEvents is false.
		*/
		[field: SerializeField] SandboxEvent Event = null;

		/**
			A list of SandboxEvents to invoke when the button is pressed, if
			MultipleEvents is true.
		*/
		[field: SerializeField]	List<SandboxEvent> Events = new List<SandboxEvent>();
		
		List<SandboxEvent> registeredEvents = new List<SandboxEvent>();

#if UNITY_EDITOR
		/**
			Custom editor for the SandboxEvent Button Invoker.

			Shows the default Button script editor, but adds the SandboxEvent
			field (or list of fields if 'Multiple Events' is checked).
		*/
		[type: CustomEditor(typeof(ButtonInvoker))]
		class Editor : UnityEditor.UI.ButtonEditor
		{
			override public void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				serializedObject.Update();

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ButtonInvoker.MultipleEvents)));

				if (serializedObject.FindProperty(nameof(ButtonInvoker.MultipleEvents)).boolValue)
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ButtonInvoker.Events)));
				else
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ButtonInvoker.Event)));

				serializedObject.ApplyModifiedProperties();
			}
		}
#endif

		bool registered;

		override protected void OnEnable()		{ base.OnEnable();		ReRegister();	}
		override protected void Start()			{ base.OnEnable();		ReRegister();	}

#if UNITY_EDITOR
		override protected void OnValidate()	{ base.OnValidate();	ReRegister();	}
#endif

		override protected void OnDisable()		{ base.OnDisable();		UnRegister();	}
		override protected void OnDestroy()		{ base.OnDestroy();		UnRegister();	}

		void ReRegister()
		{
			UnRegister();

			if (Application.isPlaying && isActiveAndEnabled)
			{
				registeredMultipleEvents = MultipleEvents;
				
				registeredEvents = new List<SandboxEvent>();
				
				if (registeredMultipleEvents)
				{
					foreach (SandboxEvent sandboxEvent in Events)
						if (sandboxEvent != null)
							registeredEvents.Add(sandboxEvent);
				}

				else if (Event != null)
					registeredEvents.Add(Event);
				
				foreach (SandboxEvent sandboxEvent in registeredEvents)
					sandboxEvent.AddInvoker(gameObject);
				
				registered = true;
			}
		}

		void UnRegister()
		{
			foreach (SandboxEvent sandboxEvent in registeredEvents)
				sandboxEvent.DropInvoker(gameObject);
			
			registeredEvents.Clear();

			registered = false;
		}

		override public void OnPointerClick(PointerEventData eventData)
		{
			base.OnPointerClick(eventData);

			if (eventData.button==PointerEventData.InputButton.Left && registered)
				foreach (SandboxEvent sandboxEvent in registeredEvents)
					sandboxEvent.Invoke(gameObject);
		}
	}
}