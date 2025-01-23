using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace CREATIVE.SandboxAssets
{
	public enum InputActionStage
	{
		Started,
		Performed,
		Cancelled
	}
	
	/**
		This component listens for a particular Input Action from the
		project-wide Input Actions and links it to a Sandbox Event.
	*/
	public class InputActionProcessor : MonoBehaviour
	{
		/**
			A reference to the InputAction that should be listened for.
		*/
		[field: SerializeField]
		private InputActionReference Action;
		private InputActionReference registeredAction;

		/**
			Which stage of the InputAction should be listened for.

			Performed is the most commonly used stage. It happens once when a
			button is pressed.

			Started and Cancelled can be used to detect when a button starts and
			stops being held down.
		*/
		[field: SerializeField]
		private InputActionStage ActionStage = InputActionStage.Performed;
		private InputActionStage registeredActionStage;

		/**
			The SandboxEvent that is invoked by the InputAction
		*/
		[field: SerializeField]
		private SandboxEvent Event = null;
		private SandboxEvent registeredEvent;

		private bool registered = false;

		void Start()		=> ReRegister();
		void OnValidate()	=> ReRegister();
		void OnEnable()		=> ReRegister();

		void OnDisable()	=> UnRegister();
		void OnDestroy()	=> UnRegister();

		private void ReRegister()
		{
			UnRegister();

			registeredAction = Action;
			registeredActionStage = ActionStage;
			registeredEvent = Event;

			if (Application.isPlaying && isActiveAndEnabled && registeredAction!=null && registeredEvent!=null)
			{
				if (registeredActionStage == InputActionStage.Started)
					Action.action.started += Invoke;
				
				if (registeredActionStage == InputActionStage.Performed)
					Action.action.performed += Invoke;
				
				if (registeredActionStage == InputActionStage.Cancelled)
					Action.action.canceled += Invoke;

				registeredEvent.AddInvoker(gameObject);

				registered = true;
			}
		}

		private void UnRegister()
		{
			if (registered)
			{
				if (registeredActionStage == InputActionStage.Started)
					registeredAction.action.started -= Invoke;
				
				if (registeredActionStage == InputActionStage.Performed)
					registeredAction.action.performed -= Invoke;
				
				if (registeredActionStage == InputActionStage.Cancelled)
					registeredAction.action.canceled -= Invoke;
				
				registeredEvent.DropInvoker(gameObject);

				registered = false;
			}
		}

		private void Invoke(InputAction.CallbackContext context) => registeredEvent.Invoke(gameObject);
	}
}