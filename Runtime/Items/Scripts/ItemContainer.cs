using System;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets.Items
{
	/**
		This Component manages the instantiation, detection, and movement of
		instantiated SandboxItem objects in a scene.

		If the GameObject this script is attached to contains another GameObject
		that has an ItemReference script attached, this ItemContainer will be
		considered populated.

		The fields in this script can be used to:
		- Instantitate a SandboxItem in this container at the start of
		  a scene.
		- Move a SandboxItem from a different container to this one.
		- Request that another container move the item contained in this
		  container to itself.
	*/
	public class ItemContainer : MonoBehaviour
	{
		/**
			A container class for the SandboxEvent fields that allow an
			ItemContainer to communicate with other ItemContainer components.

			Strictly for readability purposes in the inspector.
		*/
		[Serializable]
		class ContainerEventsGroup
		{
			/**
				This SandboxEvent facilitates communication between a primary
				and secondary ItemContainer.
				
				If this ItemContainer is the primary ItemContainer, it will
				listen to this SandboxEvent. If this ItemContainer is not the
				primary ItemContainer, it may invoke this SandboxEvent.

				It may be invoked with an ItemReference as a target. This
				indicates a request that the SandboxItem referenced be moved
				from its current ItemContainer to the primary ItemContainer.

				It may be invoked with an ItemContainer as a target. This
				indicates a request that the SandboxItem in the primary
				ItemContainer be moved to the target ItemContainer.

				These requests may not actually be granted. For a SandboxItem to
				move from one ItemContainer to another, the source must be
				populated and the destination must be un-populated. If this is
				not the case, the invocation of this SandboxEvent will simply
				have no effect.

				Must not be null.
			*/
			[field: SerializeField] public SandboxEvent PrimaryContainerRequestEvent;

			/**
				This ItemContainer may invoke this SandboxEvent in order to
				communicate with the primary ItemContainerGroup.

				It may be invoked with an ItemReference as a target. This
				indicates a request that the SandboxItem referenced be moved
				from this ItemContainer to the primary ItemContainerGroup.

				It may be invoked with an ItemContainer as a target. This
				indicates a request that the first populated SandboxItem in the 
				primary ItemContainerGroup be moved to this ItemContainer.

				These requests may not actually be granted. For a SandboxItem to
				move from an ItemContainer to an ItemContainerGroup, the
				ItemContainer must be populated and the ItemContainerGroup must
				have at least one un-populated ItemContainer. For a SandboxItem
				to move from an ItemContainerGroup to an ItemContainer, the
				ItemContainerGroup must be populated by at least one SandboxItem
				and the ItemContainer must be un-populated. If this is not the
				case, the invocation of this SandboxEvent will simply have no
				effect.

				Must not be null.
			*/
			[field: SerializeField] public SandboxEvent PrimaryContainerGroupRequestEvent;

			/**
				This SandboxEvent will be invoked by this ItemContainer when an
				item is moved under it from a different ItemContainer.

				The target will be the SandboxItem contained.
			*/
			[field: SerializeField] public SandboxEvent ContainedEvent;
		}

		/**
			The SandboxEvent fields necessary for communication with other
			ItemContainer and ItemContainerGroup components.
		*/
		[field: SerializeField] ContainerEventsGroup ContainerEvents;

		/**
			Whether this is the primary ItemContainer.

			There should be only one primary ItemContainer per scene. 
		*/
		[field: SerializeField] bool Primary = false;

		/**
			The SandboxEvent that will be invoked when the user interacts with
			something in the scene.

			If this is the primary ItemContainer, and it is populated, and the
			contained SandboxItem has an InteractEvent set, the SandboxItem's
			InteractEvent will be subsequently invoked after every invocation of
			this one.
		*/
		[field: SerializeField] SandboxEvent InteractEvent;

		/**
			The SandboxItem that should be contained in this ItemContainer at
			the start of the scene.
		*/
		[field: SerializeField] SandboxItem ContainOnStart;

#if UNITY_EDITOR
		/**
			A custom editor for an ItemContainer.
		*/
		[type: CustomEditor(typeof(ItemContainer))]
		class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				serializedObject.Update();
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ItemContainer.ContainerEvents)));

				EditorGUILayout.Space();
				EditorGUILayout.Space();

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ItemContainer.Primary)));

				if (serializedObject.FindProperty(nameof(ItemContainer.Primary)).boolValue)
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ItemContainer.InteractEvent)));
				
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ItemContainer.ContainOnStart)));

				serializedObject.ApplyModifiedProperties();
			}
		}
#endif

		Record registeredRecord = null;

		bool registered = false;

		SandboxEvent containedItemInteractEvent = null;

		class Record
		{
			readonly GameObject Container;

			public readonly SandboxEvent ContainedEvent;

			public readonly SandboxEvent PrimaryContainerGroupRequestEvent;

			public readonly bool Primary;

			public readonly SandboxEvent PrimaryContainerRequestEvent;

			readonly DelegateListener PrimaryContainerRequestListener;

			readonly DelegateListener InteractListener;

			bool torndown = false;

			Record(GameObject container, SandboxEvent containedEvent, SandboxEvent primaryContainerGroupRequestEvent)
			{
				if (container == null)
					throw new ArgumentNullException(nameof(container));

				if (containedEvent == null)
					throw new ArgumentNullException(nameof(containedEvent));

				if (primaryContainerGroupRequestEvent == null)
					throw new ArgumentNullException(nameof(primaryContainerGroupRequestEvent));

				Container = container;

				ContainedEvent = containedEvent;
				ContainedEvent.AddInvoker(Container);

				PrimaryContainerGroupRequestEvent = primaryContainerGroupRequestEvent;
				PrimaryContainerGroupRequestEvent.AddInvoker(Container);
			}

			public Record
			(
				GameObject container,
				SandboxEvent containedEvent,
				SandboxEvent primaryContainerRequestEvent,
				DelegateListener.EventHandler primaryContainerRequestHandler,
				SandboxEvent primaryContainerGroupRequestEvent,
				SandboxEvent interactEvent,
				DelegateListener.EventHandler interactHandler
			) : this(container, containedEvent, primaryContainerGroupRequestEvent)
			{
				if (primaryContainerRequestEvent == null)
					throw new ArgumentNullException(nameof(primaryContainerRequestEvent));

				if (primaryContainerRequestHandler == null)
					throw new ArgumentNullException(nameof(primaryContainerRequestHandler));

				if (interactEvent == null)
					throw new ArgumentNullException(nameof(interactEvent));

				if (interactHandler == null)
					throw new ArgumentNullException(nameof(interactHandler));

				Primary = true;

				PrimaryContainerRequestEvent = null;

				PrimaryContainerRequestListener = new DelegateListener
				(
					primaryContainerRequestEvent,
					Container,
					primaryContainerRequestHandler
				);
				PrimaryContainerRequestListener.Enable();

				InteractListener = new DelegateListener
				(
					interactEvent,
					Container,
					interactHandler
				);
				InteractListener.Enable();
			}

			public Record
			(
				GameObject container,
				SandboxEvent containedEvent,
				SandboxEvent primaryContainerRequestEvent,
				SandboxEvent primaryContainerGroupRequestEvent
			)
				: this(container, containedEvent, primaryContainerGroupRequestEvent)
			{
				if (primaryContainerRequestEvent == null)
					throw new ArgumentNullException(nameof(primaryContainerRequestEvent));

				Primary = false;

				PrimaryContainerRequestEvent = primaryContainerRequestEvent;
				PrimaryContainerRequestEvent.AddInvoker(Container);

				PrimaryContainerRequestListener = null;

				InteractListener = null;
			}

			public void Teardown()
			{
				if (!torndown)
				{
					if (Primary)
					{
						PrimaryContainerRequestListener.Disable();
						InteractListener.Disable();
					}

					else
						PrimaryContainerRequestEvent.DropInvoker(Container);

					PrimaryContainerGroupRequestEvent.DropInvoker(Container);

					ContainedEvent.DropInvoker(Container);

					torndown = true;
				}
			}
		}

		InvalidOperationException notRegisteredException =
			new InvalidOperationException("This Behavior has not been registered.");

		void OnEnable() => ReRegister();
		void Start() => ReRegister();

#if UNITY_EDITOR
		void OnValidate() => ReRegister();
#endif

		void OnDisable() => UnRegister();
		void OnDestroy() => UnRegister();

		void ReRegister()
		{
			if
			(
				Application.isPlaying && isActiveAndEnabled &&
				ContainerEvents != null &&
				ContainerEvents.ContainedEvent != null &&
				ContainerEvents.PrimaryContainerRequestEvent != null &&
				ContainerEvents.PrimaryContainerGroupRequestEvent != null
			)
			{
				UnRegister();

				if (Primary && InteractEvent == null)
					throw new InvalidOperationException
						(nameof(InteractEvent) + " has not been set.");

				if (ContainOnStart != null && getContainedItem() != null)
					throw new InvalidOperationException
						(nameof(ContainOnStart) + " is set, but an Item is already contained.");

				if (Primary)
					registeredRecord = new Record
					(
						gameObject,
						ContainerEvents.ContainedEvent,
						ContainerEvents.PrimaryContainerRequestEvent, processPrimaryContainerRequest,
						ContainerEvents.PrimaryContainerGroupRequestEvent,
						InteractEvent, interact
					);

				else
					registeredRecord = new Record
					(
						gameObject,
						ContainerEvents.ContainedEvent,
						ContainerEvents.PrimaryContainerRequestEvent,
						ContainerEvents.PrimaryContainerGroupRequestEvent
					);

				if (ContainOnStart != null)
				{
					contain(ContainOnStart.CreateRecord());
					ContainOnStart = null;
				}

				registered = true;
			}
		}

		void UnRegister()
		{
			if (registered)
			{
				registeredRecord.Teardown();

				if (containedItemInteractEvent != null)
					containedItemInteractEvent.DropInvoker(gameObject);

				registered = false;
			}
		}

		/**
			Whether or not this container currently contains a SandboxItem.
		*/
		public bool Populated
		{
			get
			{
				if (!registered)
					throw notRegisteredException;

				return getContainedItem() != null;
			}
		}

		ItemReference getContainedItem()
		{
			foreach (Transform t in transform)
			{
				if (t != transform)
				{
					ItemReference item = t.GetComponentInChildren<ItemReference>();

					if (item != null)
					{
						item.NullCheckItem();

						return item;
					}
				}
			}

			return null;
		}

		void contain(SandboxItem.Record item)
		{
			if (transform is RectTransform)
				item.InstantiateAsIcon(transform as RectTransform);

			else
				item.InstantiateAsModel(transform);
		}

		void containDestroyAndNotify(ItemReference item)
		{
			if (getContainedItem() == null)
			{
				contain(item.ItemRecord);

				Destroy(item.gameObject);

				registeredRecord.ContainedEvent.Invoke(gameObject, item.ItemRecord.Item);
			}
		}

		/**
			This method attempts to move the given SandboxItem to this
			ItemContainer.

			It will do nothing if this ItemContainer is already populated.
		*/
		public void TryToContain(ItemReference item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			if (!registered)
				throw notRegisteredException;

			item.NullCheckItem();

			if (!Populated)
				containDestroyAndNotify(item);
		}

		/**
			This method attempts to move the SandboxItem contained by the given
			ItemContainer to this ItemContainer.

			It will do nothing if this ItemContainer is already populated, or if
			the given ItemContainer is un-populated.
		*/
		public void TryToContain(ItemContainer container)
		{
			if (container == null)
				throw new ArgumentNullException(nameof(container));

			if (!registered)
				throw notRegisteredException;

			if (container.Populated)
				TryToContain(container.getContainedItem());
		}

		/**
			Attempts to communicate with the primary ItemContainer in the scene.

			If this ItemContainer is populated, it will request that the
			contained SandboxItem be moved to the primary ItemContainer.

			If this ItemContainer is un-populated, it will request that an item
			in the primary ItemContainer be moved here.
		*/
		public void MakePrimaryContainerRequest()
		{
			if (!registered)
				throw notRegisteredException;

			ItemReference containedItem = getContainedItem();

			if (containedItem == null)
				registeredRecord.PrimaryContainerRequestEvent.Invoke(gameObject, this);
			else
				registeredRecord.PrimaryContainerRequestEvent.Invoke(gameObject, containedItem);
		}

		/**
			Attempts to communicate with the primary ItemContainerGroup in the
			scene.

			If this ItemContainer is populated, it will request that the
			contained SandboxItem be moved to the primary ItemContainerGroup.

			If this ItemContainer is un-populated, it will request that the
			first populated item in the primary ItemContainerGroup be moved
			here.
		*/
		public void MakePrimaryContainerGroupRequest()
		{
			if (!registered)
				throw notRegisteredException;

			ItemReference containedItem = getContainedItem();

			if (containedItem == null)
				registeredRecord.PrimaryContainerGroupRequestEvent.Invoke(gameObject, this);
			else
				registeredRecord.PrimaryContainerGroupRequestEvent.Invoke(gameObject, containedItem);
		}

		bool processPrimaryContainerRequest(UnityEngine.Object target)
		{
			if (target == null || (!(target is ItemReference) && !(target is ItemContainer)))
				throw new InvalidOperationException
				(
					"Target of " + nameof(ContainerEvents.PrimaryContainerRequestEvent) + " must be either " +
					" an " + nameof(ItemReference) + " or " +
					" an " + nameof(ItemContainer) + "."
				);

			ItemReference containedItem = getContainedItem();

			if (target is ItemReference && containedItem == null)
				TryToContain(target as ItemReference);

			else if (target is ItemContainer && containedItem != null)
				(target as ItemContainer).TryToContain(containedItem);

			updateInteractEvent();

			return false;
		}

		bool interact(UnityEngine.Object target)
		{
			updateInteractEvent();

			if (containedItemInteractEvent != null)
				containedItemInteractEvent.Invoke(gameObject, target);

			return false;
		}

		void updateInteractEvent()
		{
			ItemReference containedItem = getContainedItem();

			if
			(
				(containedItem == null && containedItemInteractEvent != null) ||
				(
					containedItem != null && containedItemInteractEvent != null &&
					containedItem.ItemRecord.InteractEvent != containedItemInteractEvent
				)
			)
			{
				containedItemInteractEvent.DropInvoker(gameObject);
				containedItemInteractEvent = null;
			}

			if (containedItem != null && containedItem.ItemRecord.InteractEvent != null)
			{
				containedItemInteractEvent = containedItem.ItemRecord.InteractEvent;
				containedItemInteractEvent.AddInvoker(gameObject);
			}
		}
	}
}