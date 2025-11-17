// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CREATIVE.SandboxAssets.Events
{
	/**
		This class calls various SandboxEvents when the GameObject's collider
		interacts with other colliders in various ways.
	*/
	[RequireComponent(typeof(Collider))]
	public class ColliderInvoker : MonoBehaviour
	{
		/**
			The SandboxEvent that is invoked when this GameObject's collider
			starts to intersect with another collider.
		*/
		[field: SerializeField]	SandboxEvent CollisionEnterEvent			= null;
								SandboxEvent registeredCollisionEnterEvent	= null;
		
		/**
			The SandboxEvent that is invoked when this GameObject's collider
			stops intersecting with another collider.
		*/
		[field: SerializeField]	SandboxEvent CollisionExitEvent				= null;
								SandboxEvent registeredCollisionExitEvent	= null;

		/**
			The SandboxEvent that is invoked when this GameObject's collider
			starts to intersect with another collider, and one of the colliders
			is a trigger.
		*/
		[field: SerializeField]	SandboxEvent TriggerEnterEvent				= null;
								SandboxEvent registeredTriggerEnterEvent	= null;

		/**
			The SandboxEvent that is invoked when this GameObject's collider
			stops intersecting with another collider, and one of the colliders
			is a trigger.
		*/
		[field: SerializeField]	SandboxEvent TriggerExitEvent				= null;
								SandboxEvent registeredTriggerExitEvent		= null;

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
			}
		}

		void UnRegister()
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