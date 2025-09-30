using System;
using System.Collections.Generic;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

#if UNITY_EDITOR
	using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets.Items
{
	/**
		This component collects a set of ItemContainer objects into a group,
		allowing items to be added and removed as needed, and as space allows.
	*/
	public class ItemContainerGroup : MonoBehaviour
	{
		/**
			A container class for the SandboxEvent fields that allow an
			ItemContainerGroup to communicate with other ItemContainer
			components.

			Strictly for readability purposes in the inspector.
		*/
		[Serializable]
		class ContainerEventsGroup
		{
			/**
				If this ItemContainerGroup is the primary ItemContainerGroup, it
				will listen to this SandboxEvent.

				It may be invoked with an ItemReference as a target. This
				indicates a request that the SandboxItem referenced be moved
				from its current ItemContainer to the primary
				ItemContainerGroup.

				It may be invoked with an ItemContainer as a target. This
				indicates a request that the first populated SandboxItem in the 
				primary ItemContainerGroup be moved to the target ItemContainer.

				These requests may not actually be granted. For a SandboxItem to
				move from an ItemContainer to an ItemContainerGroup, the
				ItemContainer must be populated and the ItemContainerGroup must
				have at least one un-populated ItemContainer. For a SandboxItem
				to move from an ItemContainerGroup to an ItemContainer, the
				ItemContainerGroup must be populated by at least one SandboxItem
				and the ItemContainer must be un-populated. If this is not the
				case, the invocation of this SandboxEvent will simply have no
				effect. 
			*/
			[field: SerializeField] public SandboxEvent PrimaryContainerGroupRequestEvent;
		}

		/**
			The SandboxEvent fields necessary for communication with other
			ItemContainer and ItemContainerGroup components.
		*/
		[field: SerializeField] ContainerEventsGroup ContainerEvents;
		DelegateListener primaryContainerGroupRequestListener;

		/**
			Whether this is the primary ItemContainerGroup.

			There should be only one primary ItemContainerGroup per scene. 
		*/
		[field: SerializeField] bool Primary;
		bool registeredPrimary;

		/**
			The ItemContainer objects that should be considered a part of this
			group.
		*/
		[field: SerializeField] ItemContainer[] Containers;
		List<ItemContainer> registeredContainers;

#if UNITY_EDITOR
		/**
			A custom editor for an ItemContainerGroup.
		*/
		[type: CustomEditor(typeof(ItemContainerGroup))]
		class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				serializedObject.Update();
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ItemContainerGroup.ContainerEvents)));

				EditorGUILayout.Space();
				EditorGUILayout.Space();

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ItemContainerGroup.Primary)));
				
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ItemContainerGroup.Containers)));

				serializedObject.ApplyModifiedProperties();
			}
		}
#endif

		bool registered = false;

		void OnEnable() => ReRegister();
		void Start() => ReRegister();

#if UNITY_EDITOR
		void OnValidate() => ReRegister();
#endif

		void OnDisable() => UnRegister();
		void OnDestroy() => UnRegister();

		void ReRegister()
		{
			UnRegister();

			if
			(
				Application.isPlaying && isActiveAndEnabled &&
				ContainerEvents != null && ContainerEvents.PrimaryContainerGroupRequestEvent != null &&
				Containers != null && Containers.Length > 0
			)
			{
				registeredPrimary = Primary;

				if (registeredPrimary)
				{
					primaryContainerGroupRequestListener = new DelegateListener
					(
						ContainerEvents.PrimaryContainerGroupRequestEvent,
						gameObject,
						processPrimaryContainerGroupRequest
					);
					primaryContainerGroupRequestListener.Enable();
				}

				registeredContainers = new List<ItemContainer>(Containers);

				registered = true;
			}
		}

		void UnRegister()
		{
			if (registered)
			{
				if (registeredPrimary)
					primaryContainerGroupRequestListener.Disable();

				registered = false;
			}
		}

		bool processPrimaryContainerGroupRequest(UnityEngine.Object target)
		{
			if (target == null || (!(target is ItemReference) && !(target is ItemContainer)))
				throw new InvalidOperationException
				(
					"Target of " + nameof(ContainerEvents.PrimaryContainerGroupRequestEvent) + " must be either " +
					" an " + nameof(ItemReference) + " or " +
					" an " + nameof(ItemContainer) + "."
				);

			if (target is ItemReference)
			{
				foreach (ItemContainer container in registeredContainers)
				{
					if (!container.Populated)
					{
						container.TryToContain(target as ItemReference);
						return false;
					}
				}
			}

			if (target is ItemContainer)
			{
				foreach (ItemContainer container in registeredContainers)
				{
					if (container.Populated)
					{
						(target as ItemContainer).TryToContain(container);
						return false;
					}
				}
			}

			return false;
		}
	}
}
