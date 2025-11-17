// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	/**
		A MonoBehaviour that evaluates a given BehaviorTree in a scene.
	*/
	public class BehaviorTreeRunner : MonoBehaviour
	{
		/**
			Invoked immediately before a BehaviorTree begins to be evaluated.

			Usually on Enable, Start, or when a BehaviorTree is set mid-scene.
		*/
		public event Action BehaviorTreeActivated;

		/**
			Invoked after there are no more nodes in the BehaviorTree to
			evaluate, or when this MonoBehavior is Disabled or Destroyed.
		*/
		public event Action BehaviorTreeDeactivated;

		/**
			Invoked immediately before a new node in the BehaviorTree begins to
			be evaluated.
		*/
		public event Action ActiveBehaviorTreeCurrentNodeChanged;

		[field: SerializeField] BehaviorTree BehaviorTree;

		Graph registeredBehaviorTree;

		Action registeredBehaviorTreeTeardown;

		Node.IRecord<Node> registeredBehaviorTreeCurrentNode;

		Vector2 registeredBehaviorTreeEntryNodePosition;

		bool registered = false;

		void OnEnable() => ReRegister();
		void Start() => ReRegister();

#if UNITY_EDITOR
		void OnValidate() => ReRegister();
#endif

		void OnDisable() => UnRegister();
		void OnDestroy() => UnRegister();

		/**
			Whether or not a BehaviorTree object is set in the serialized field
			of this MonoBehavior.
		*/
		public bool BehaviorTreeIsSet { get { return BehaviorTree != null; } }

		/**
			The SerializedObject representing the BehaviorTree that is set in
			this MonoBehavior.

			Throws an exception if BehaviorTreeIsSet is false.
		*/
		public BehaviorTree InactiveBehaviorTree
		{
			get
			{
				if (!BehaviorTreeIsSet)
					throw new InvalidOperationException("Behavior Tree is not set.");

				return BehaviorTree;
			}
		}

		/**
			Whether or not a BehaviorTree is being currently evaluated.

			Returns false if BehaviorTreeIsSet is also false.
		*/
		public bool BehaviorTreeIsActive { get { return registered; } }

		/**
			The Graph object representing the BehaviorTree being currently evaluated.

			Throws an exception if BehaviorTreeIsActive is false.
		*/
		public Graph ActiveBehaviorTree
		{
			get
			{
				if (!BehaviorTreeIsActive)
					throw new InvalidOperationException("Behavior Tree is not active.");

				return registeredBehaviorTree;
			}
		}

		/**
			The Node.IRecord<Node> object representing the node that is being
			currently evaluated in the active BehaviorTree.

			Throws an exception if BehaviorTreeIsActive is false.
		*/
		public Node.IRecord<Node> ActiveBehaviorTreeCurrentNode
		{
			get
			{
				if (!BehaviorTreeIsActive)
					throw new InvalidOperationException("Behavior Tree is not active.");

				return registeredBehaviorTreeCurrentNode;
			}
		}

		/**
			The position at which the node pointing to the root node of the
			active BehaviorTree should be displayed.

			Only relevant in the Unity Editor.

			Throws an exception if BehaviorTreeIsActive is false.
		*/
		public Vector2 ActiveBehaviorTreeEntryNodePosition
		{
			get
			{
				if (!BehaviorTreeIsActive)
					throw new InvalidOperationException("Behavior Tree is not active.");

				return registeredBehaviorTreeEntryNodePosition;
			}
		}

		void UnRegister()
		{
			if (registered)
			{
				registeredBehaviorTreeTeardown();

				registered = false;
			}

			if (BehaviorTreeDeactivated != null)
				BehaviorTreeDeactivated();
		}

		void ReRegister()
		{
			UnRegister();

			if (Application.isPlaying && isActiveAndEnabled && BehaviorTreeIsSet)
			{
				registeredBehaviorTree = BehaviorTree.CreateGraph
				(
					gameObject,
					(currentNode) =>
					{
						registeredBehaviorTreeCurrentNode = currentNode;

						if (ActiveBehaviorTreeCurrentNodeChanged != null)
							ActiveBehaviorTreeCurrentNodeChanged();
					},
					UnRegister,
					out registeredBehaviorTreeTeardown
				);

				registeredBehaviorTreeCurrentNode = registeredBehaviorTree.RootNode;

				registeredBehaviorTreeEntryNodePosition = BehaviorTree.EntryNodePosition;

				registered = true;

				if (BehaviorTreeActivated != null)
					BehaviorTreeActivated();

				registeredBehaviorTree.Evaluate();
			}
		}
	}
}