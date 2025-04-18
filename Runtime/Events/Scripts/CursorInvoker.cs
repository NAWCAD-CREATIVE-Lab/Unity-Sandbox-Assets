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

namespace CREATIVE.SandboxAssets.Events
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
	[type: RequireComponent(typeof(Camera))]
	[type: RequireComponent(typeof(PhysicsRaycaster))]
	public class CursorInvoker : MonoBehaviour
	{
		/**
			The input that will indicate the cursor has moved.

			Raycasts to check for focus on a new object will only be performed
			after these actions.
		*/
		[field: SerializeField] List<InputActionReference> CursorMoveActions			= new List<InputActionReference>();
								List<InputActionReference> registeredCursorMoveActions	= new List<InputActionReference>();
		
		/**
			The input that will be understood as "interacting" with an object in
			the scene.
		*/
		[field: SerializeField]	InputActionReference InteractAction				= null;
								InputActionReference registeredInteractAction	= null;
		
		/**
			This event will be invoked every time the user performs the interact
			action while looking (mousing over) a detectable GameObject.
			
			That GameObject will be supplied as the target of the event.
		*/
		[field: SerializeField]	SandboxEvent InteractEvent				= null;
								SandboxEvent registeredInteractEvent	= null;

		/**
			This event will be invoked every time the user starts looking at a
			detectable GameObject.

			That Gameobject will be supplied as the target of the event.

			The event will not be invoked again for the same object until the
			user looks away from, and back at, the object.
		*/
		[field: SerializeField]	SandboxEvent LookEvent				= null;
								SandboxEvent registeredLookEvent	= null;

		/**
			The object the user is currently looking at.
		*/
		[field: SerializeField] GameObject CurrentlyLooking = null;

#if UNITY_EDITOR
		event Action repaintEditor;
		
		/**
			Custom editor for the SandboxEventCursorInvoker Monobehaviour 
		*/
		[type: CustomEditor(typeof(CursorInvoker))]
		class Editor : UnityEditor.Editor
		{
			void OnEnable()		=> (target as CursorInvoker).repaintEditor += Repaint;
			void OnDisable()	=> (target as CursorInvoker).repaintEditor -= Repaint;
			
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
		}
#endif

		bool registered = false;

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
				if (InteractEvent == null)
					throw new InvalidOperationException("InteractEvent Must be populated prior to start of scene.");
				
				if (LookEvent == null)
					throw new InvalidOperationException("ObjectLookEvent Must be populated prior to start of scene.");
					
				if (Application.isPlaying && isActiveAndEnabled)
				{
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
					
					registeredInteractEvent = InteractEvent;
					registeredInteractEvent.AddInvoker(gameObject);

					registeredLookEvent = LookEvent;
					registeredLookEvent.AddInvoker(gameObject);
					
					registeredInteractAction = InteractAction;
					registeredInteractAction.action.performed += Interact;
					
					registered = true;
				}
			}
		}

		void UnRegister()
		{
			if (registeredInteractEvent != null)
			{
				registeredInteractEvent.DropInvoker(gameObject);
				registeredInteractEvent = null;
			}

			if (registeredLookEvent != null)
			{
				registeredLookEvent.DropInvoker(gameObject);
				registeredLookEvent = null;
			}

			if (registeredInteractAction != null)
			{
				registeredInteractAction.action.performed -= Interact;
				registeredInteractAction = null;
			}

			foreach (InputActionReference inputActionReference in registeredCursorMoveActions)
				inputActionReference.action.performed -= Look;
			registeredCursorMoveActions.Clear();
			
			registered = false;
		}

		void Interact(InputAction.CallbackContext context)
		{
			if (registered && Application.isPlaying && isActiveAndEnabled && CurrentlyLooking!=null)
				registeredInteractEvent.Invoke(gameObject, CurrentlyLooking);
		}

		void Look(InputAction.CallbackContext context)
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
				registeredLookEvent.Invoke(gameObject, newCurrentlyLooking);
			
			// Set the new object the user is looking at
			CurrentlyLooking = newCurrentlyLooking;

#if UNITY_EDITOR
			if (repaintEditor != null)
				repaintEditor();
#endif
		}
	}
}