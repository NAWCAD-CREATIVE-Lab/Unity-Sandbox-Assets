using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets
{
	/**
		This class invokes events when the cursor passes over, and interacts
		with, objects in the scene.

		UI and physics raycasts are performed on every frame from the center of
		the cursor position and a Sandbox Event is invoked if the user begins
		looking at something new.

		Both world-space and screen-space UI panels will always be detected, but
		3D objects will only be detected if they have a collider component.

		A seperate Sandbox Event is invoked if the user performs the interact
		action while looking at an object.
	*/
	[RequireComponent(typeof(Camera))]
	[RequireComponent(typeof(PhysicsRaycaster))]
	public class SandboxEventCursorInvoker : MonoBehaviour
	{
		/**
			The input that will indicate the cursor has moved.

			Raycasts to check for focus on a new object will only be performed
			after these actions.
		*/
		[field: SerializeField]
		private List<InputActionReference> CursorMoveActions;
		private List<InputActionReference> registeredCursorMoveActions;
		
		/**
			The input that will be understood as "interacting" with an object in
			the scene.
		*/
		[field: SerializeField]
		private InputActionReference InteractAction;
		private InputActionReference registeredInteractAction;
		
		/**
			This event will be invoked every time the user performs the interact
			action while looking (mousing over) a detectable GameObject.
			
			That GameObject will be supplied as the target of the event.
		*/
		[field: SerializeField]
		private SandboxEvent InteractEvent;
		private SandboxEvent registeredInteractEvent;

		/**
			This event will be invoked every time the user starts looking at a
			detectable GameObject.

			That Gameobject will be supplied as the target of the event.

			The event will not be invoked again for the same object until the
			user looks away from, and back at, the object.
		*/
		[field: SerializeField]
		private SandboxEvent LookEvent;
		private SandboxEvent registeredLookEvent;

		/**
			The object the user is currently looking at.
		*/
		[field: SerializeField]
		private GameObject CurrentlyLooking;

#if UNITY_EDITOR
		/**
			Custom editor for the SandboxEventCursorInvoker Monobehaviour 
		*/
		[CustomEditor(typeof(SandboxEventCursorInvoker))]
		public class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				serializedObject.Update();

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CursorMoveActions)));

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(InteractAction)));

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(InteractEvent)));

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LookEvent)));
				
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CurrentlyLooking)));
				EditorGUI.EndDisabledGroup();

				serializedObject.ApplyModifiedProperties();
			}

			public override bool RequiresConstantRepaint()
			{
				return true;
			}
		}
#endif

		private bool registered = false;

		void Start()		=> ReRegister();
		void OnValidate()	=> ReRegister();
		void OnEnable()		=> ReRegister();

		void OnDisable()	=> UnRegister();
		void OnDestroy()	=> UnRegister();

		private void ReRegister()
		{
			UnRegister();

			if (Application.isPlaying && isActiveAndEnabled)
			{
				if (InteractEvent == null)
					throw new InvalidOperationException("InteractEvent Must be populated prior to start of scene.");
				
				if (LookEvent == null)
					throw new InvalidOperationException("ObjectLookEvent Must be populated prior to start of scene.");
					
				if (Application.isPlaying && isActiveAndEnabled)
				{
					registeredCursorMoveActions = new List<InputActionReference>();

					foreach (InputActionReference inputActionReference in CursorMoveActions)
					{
						if (inputActionReference!=null)
						{
							registeredCursorMoveActions.Add(inputActionReference);
							inputActionReference.action.performed += Look;
						}
					}

					if (registeredCursorMoveActions.Count == 0)
						throw new InvalidOperationException
							("CursorMoveActions must be populated prior to the start of the scene.");
					
					InteractEvent.AddInvoker(gameObject);

					LookEvent.AddInvoker(gameObject);
					
					InteractAction.action.performed += Interact;
					
					registered = true;
				}
			}
		}

		private void UnRegister()
		{
			if (registered)
			{
				InteractEvent.DropInvoker(gameObject);

				LookEvent.DropInvoker(gameObject);
				
				InteractAction.action.performed -= Interact;

				foreach (InputActionReference inputActionReference in CursorMoveActions)
					inputActionReference.action.performed -= Look;
				
				registered = false;
			}
		}

		private void Interact(InputAction.CallbackContext context)
		{
			if (registered && Application.isPlaying && isActiveAndEnabled && CurrentlyLooking!=null)
				InteractEvent.Invoke(gameObject, CurrentlyLooking);
		}

		private void Look(InputAction.CallbackContext context)
		{
			List<RaycastResult> raycastResults = new List<RaycastResult>();

			/*
				Raycast to get a list of objects the user is looking at
				This script requires a PhysicsRaycaster, so both UI elements and
				colliders will be detected.
			*/
			EventSystem.current.RaycastAll
			(
				new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() },
				raycastResults
			);

			GameObject newCurrentlyLooking = null;
			int depth = int.MinValue;
			
			//Get the closest object
			foreach (RaycastResult result in raycastResults)
			{
				if (result.depth > depth)
				{
					depth = result.depth;
					newCurrentlyLooking = result.gameObject;
				}
			}

			/*
				If the user isn't looking at the same object they were
				last frame, or started looking at a new object, invoke
				ObjectLookEvent.
			*/
			if
			(
				isActiveAndEnabled &&
				newCurrentlyLooking != null &&
				newCurrentlyLooking != CurrentlyLooking
			)
				LookEvent.Invoke(gameObject, newCurrentlyLooking);
			
			// Set the new object the user is looking at
			CurrentlyLooking = newCurrentlyLooking;
		}
	}
}