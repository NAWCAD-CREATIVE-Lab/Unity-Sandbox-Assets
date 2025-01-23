using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets
{
	/**
		This class:
			- Provides a method to quit the application
			- Quits the application when a given InputAction is performed,
			but only when the application is running in Debug mode
			- Allows the visibility of the cursor to be toggled
			- Uses a SandboxObject reference as a Singleton in order to restrict
			some code to only run on one instance
			- Enables the GameObject's EventSystem and InputSystemUIInputModule
			components on Start, but only for the instance that has claimed the
			singleton
	*/
	[RequireComponent(typeof(EventSystem))]
	[RequireComponent(typeof(InputSystemUIInputModule))]
	public class ApplicationController : MonoBehaviour
	{
		private static ApplicationController Singleton = null;
		private static readonly object singletonPadlock = new object();

		/**
			This ApplicationController will be set to any one of the
			ApplicationController instances in the scene. It will be the only
			one allowed to invoke the SceneStarted event.
			
			Any ApplicationController will still be able to Quit the application
			and change the cursor state.
		*/
		public ApplicationController ApplicationControllerInCharge
		{
			get
			{
				lock (singletonPadlock)
					if (Singleton == null)
						Singleton = this;
				
				return Singleton;
			}
		}
		
		/**
			ApplicationController invokes this event when the scene starts for
			the benefit of any other script that needs to know.
		*/
		[field: SerializeField]
		private SandboxEvent SceneStarted;

		/**
			The InputAction that triggers the application to close, if Unity is
			running in a debug build.
		*/
		[field: SerializeField]
		private InputActionReference DebugQuitAction;

		/**
			Public read-only property that gets the status of the cursor.

			Can be called from any ApplicationController instance, regardless
			of which one is in control.
		*/
		public bool CursorEnabled
		{
			get
			{
				if (ApplicationControllerInCharge!=this)
					return ApplicationControllerInCharge.CursorEnabled;
				
				return cursorEnabled;
			}
		}

#if UNITY_EDITOR
		/**
			Custom Editor for the ApplicationController.

			Shows a greyed-out checkbox indidcating whether the cursor is visible,
			in addition to the regular public fields. 
		*/
		[CustomEditor(typeof(ApplicationController))]
		public class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				ApplicationController applicationController = target as ApplicationController;
				
				serializedObject.Update();
				
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField
				(
					"Main",
					applicationController.ApplicationControllerInCharge,
					typeof(ApplicationController),
					true
				);
				EditorGUI.EndDisabledGroup();

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(applicationController.SceneStarted)));

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(applicationController.DebugQuitAction)));

				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.Toggle("Cursor Enabled", applicationController.CursorEnabled);
				EditorGUI.EndDisabledGroup();
				
				serializedObject.ApplyModifiedProperties();
			}
		}
#endif

		private bool cursorEnabled;

		void Start()
		{
			if (Application.isPlaying)
			{
				if (ApplicationControllerInCharge == this)
				{
					if (SceneStarted==null)
						throw new InvalidOperationException
						(
							"The SceneStarted SandboxEvent reference in " +
							"ApplicationController must be populated in order for " +
							"the script to function correctly."
						);
					
					GetComponent<EventSystem>().enabled					= true;
					GetComponent<InputSystemUIInputModule>().enabled	= true;
					
					SetCursorEnabled(false);

					if (DebugQuitAction != null)
					{
						DebugQuitAction.action.performed += (context) => { if (Debug.isDebugBuild) Quit(); };
						DebugQuitAction.asset.Enable();
					}

					SceneStarted.AddInvoker(gameObject);

					SceneStarted.Invoke(gameObject);

					SceneStarted.DropInvoker(gameObject);
				}

				else
				{
					GetComponent<EventSystem>().enabled					= false;
					GetComponent<InputSystemUIInputModule>().enabled	= false;
				}
			}
		}

		void Update()
		{
			if (ApplicationControllerInCharge==this)
				SetCursorEnabled(CursorEnabled);
		}

		/**
			Enables or disables of the cursor, and changes the cursor visibility
			and lock state accordingly

			If the cursor is disabled, it will also be invisible and confined to
			the application window.

			If the cursor is enabled, it will be visible and free to move off of
			the application window.
		*/
		public void SetCursorEnabled(bool enabled)
		{
			if (ApplicationControllerInCharge==this)
			{
				if (enabled)
				{
					cursorEnabled = true;
					
					Cursor.visible = true;

					Cursor.lockState = CursorLockMode.None;
				}

				else
				{
					cursorEnabled = false;
					
					Cursor.visible = false;

					Cursor.lockState = CursorLockMode.Locked;
				}
			}

			else
				ApplicationControllerInCharge.SetCursorEnabled(enabled);
		}

		/**
			Quit the application, or exit play mode in the Unity editor.
		*/
		public void Quit()
		{
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
			#endif
			
			Application.Quit();
		}
	}
}