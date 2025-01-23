using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CREATIVE.SandboxAssets
{
	/**
		This class calls various SandboxEvents when the GameObject's collider
		interacts with other colliders in various ways.
	*/
	[RequireComponent(typeof(Collider))]
	public class SandboxEventColliderInvoker : MonoBehaviour
	{
		/**
			The SandboxEvent that is invoked when this GameObject's collider
			starts to intersect with another collider.
		*/
		[field: SerializeField]
		private SandboxEvent CollisionEnterEvent = null;
		private SandboxEvent registeredCollisionEnterEvent;
		
		/**
			The SandboxEvent that is invoked when this GameObject's collider
			stops intersecting with another collider.
		*/
		[field: SerializeField]
		private SandboxEvent CollisionExitEvent = null;
		private SandboxEvent registeredCollisionExitEvent;

		/**
			The SandboxEvent that is invoked when this GameObject's collider
			starts to intersect with another collider, and one of the colliders
			is a trigger.
		*/
		[field: SerializeField]
		private SandboxEvent TriggerEnterEvent = null;
		private SandboxEvent registeredTriggerEnterEvent;

		/**
			The SandboxEvent that is invoked when this GameObject's collider
			stops intersecting with another collider, and one of the colliders
			is a trigger.
		*/
		[field: SerializeField]
		private SandboxEvent TriggerExitEvent = null;
		private SandboxEvent registeredTriggerExitEvent;

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
				if (CollisionEnterEvent != null)
				{
					CollisionEnterEvent.AddInvoker(gameObject);
					registeredCollisionEnterEvent = CollisionEnterEvent;
				}
				
				if (CollisionExitEvent != null)
				{
					CollisionExitEvent.AddInvoker(gameObject);
					registeredCollisionExitEvent = CollisionExitEvent;
				}
				
				if (TriggerEnterEvent != null)
				{
					TriggerEnterEvent.AddInvoker(gameObject);
					registeredTriggerEnterEvent = TriggerEnterEvent;
				}
				
				if (TriggerExitEvent != null)
				{
					TriggerExitEvent.AddInvoker(gameObject);
					registeredTriggerExitEvent = TriggerExitEvent;
				}
				
				registered = true;
			}
		}

		private void UnRegister()
		{
			if (registered)
			{
				if (registeredCollisionEnterEvent != null)
				{
					registeredCollisionEnterEvent.DropInvoker(gameObject);
					registeredCollisionEnterEvent = null;
				}
				
				if (registeredCollisionExitEvent != null)
				{
					registeredCollisionExitEvent.DropInvoker(gameObject);
					registeredCollisionExitEvent = null;
				}
				
				if (registeredTriggerEnterEvent != null)
				{
					registeredTriggerEnterEvent.DropInvoker(gameObject);
					registeredTriggerEnterEvent = null;
				}
				
				if (registeredTriggerExitEvent != null)
				{
					registeredTriggerExitEvent.DropInvoker(gameObject);
					registeredTriggerExitEvent = null;
				}
				
				registered = false;
			}
		}
		
		void OnCollisionEnter(Collision collisionInfo)
		{
			if (registeredCollisionEnterEvent != null)
				registeredCollisionEnterEvent.Invoke(gameObject, collisionInfo.gameObject);
		}

		void OnCollisionExit(Collision collisionInfo)
		{
			if (registeredCollisionExitEvent != null)
				registeredCollisionExitEvent.Invoke(gameObject, collisionInfo.gameObject);
		}

		void OnTriggerEnter(Collider other)
		{
			if (registeredTriggerEnterEvent != null)
				registeredTriggerEnterEvent.Invoke(gameObject, other.gameObject);
		}

		void OnTriggerExit(Collider other)
		{
			if (registeredTriggerExitEvent != null)
				registeredTriggerExitEvent.Invoke(gameObject, other.gameObject);
		}
	}
}