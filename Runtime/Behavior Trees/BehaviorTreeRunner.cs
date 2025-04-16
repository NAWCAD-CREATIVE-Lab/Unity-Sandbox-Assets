using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	public class BehaviorTreeRunner : MonoBehaviour
	{
		public bool Registered { get; private set; } = false;
		
		public BehaviorTree BehaviorTree;
		private BehaviorTree registeredBehaviorTree = null;

		private Node currentNode = null;

		private List<SandboxEvent> registeredEventsToInvoke = new List<SandboxEvent>();

		private List<DelegateListener> registeredEventListeners = new List<DelegateListener>();

		private Dictionary<ListenerNode, Dictionary<EventToListenFor, bool>> registeredListenerStatusIndex =
			new Dictionary<ListenerNode, Dictionary<EventToListenFor, bool>>();

		void Start()		=> ReRegister();
		void OnValidate()	=> ReRegister();
		void OnEnable()		=> ReRegister();

		void OnDisable()	=> UnRegister();
		void OnDestroy()	=> UnRegister();

		private void ReRegister()
		{
			UnRegister();

			if (Application.isPlaying && isActiveAndEnabled && this.BehaviorTree!=null)
			{
				try
				{
					registeredBehaviorTree = ScriptableObject.CreateInstance(typeof(BehaviorTree)) as BehaviorTree;
					
					registeredBehaviorTree.CleanCloneFrom(this.BehaviorTree);

					Dictionary<EventToListenFor, Dictionary<ListenerNode, EventToListenFor>>
						delegateListenerSetupInfo =
							new Dictionary<EventToListenFor, Dictionary<ListenerNode, EventToListenFor>>();
					
					foreach (Node node in registeredBehaviorTree.Nodes)
					{
						if (node is InvokerNode)
						{
							InvokerNode invokerNode = node as InvokerNode;
							
							foreach (EventToInvoke eventToInvoke in (node as InvokerNode).EventsToInvoke)
							{
								eventToInvoke.Event.AddInvoker(gameObject);

								if (!registeredEventsToInvoke.Contains(eventToInvoke.Event))
									registeredEventsToInvoke.Add(eventToInvoke.Event);
							}
						}

						if (node is ListenerNode)
						{
							ListenerNode listenerNode = node as ListenerNode;

							registeredListenerStatusIndex.Add(listenerNode, new Dictionary<EventToListenFor, bool>());

							foreach
							(
								EventToListenForWithBranch eventToListenForWithBranch in listenerNode.EventsToListenFor
							)
							{
								EventToListenFor eventToListenFor = new EventToListenFor(eventToListenForWithBranch);
								
								registeredListenerStatusIndex[listenerNode].Add(eventToListenFor, false);
								
								foreach(EventToListenFor eventToListenForSegment in eventToListenFor.TargetSegements)
								{
									if (!delegateListenerSetupInfo.ContainsKey(eventToListenForSegment))
										delegateListenerSetupInfo.Add
											(eventToListenForSegment, new Dictionary<ListenerNode, EventToListenFor>());
									
									delegateListenerSetupInfo[eventToListenForSegment].Add
										(listenerNode, eventToListenFor);
								}
							}
						}
					}

					foreach (EventToListenFor eventToListenFor in delegateListenerSetupInfo.Keys)
					{
						Dictionary<ListenerNode, EventToListenFor> listeningNodeInfoIndex =
							delegateListenerSetupInfo[eventToListenFor];
						
						registeredEventListeners.Add
						(
							new DelegateListener
							(
								eventToListenFor.Event,
								gameObject,
								(target) => HandleEvent(listeningNodeInfoIndex),
								eventToListenFor.TargetFilter
							)
						);
					}

					foreach (DelegateListener delegateListener in registeredEventListeners)
						delegateListener.Enable();
					
					Registered = true;
				}

				catch (Exception e)
				{
					UnRegister();

					throw e;
				}

				currentNode = registeredBehaviorTree.RootNode;
				
				EvaluateCurrentNode();
			}
		}

		private void UnRegister()
		{
			if (registeredBehaviorTree != null)
			{
				Destroy(registeredBehaviorTree);
				registeredBehaviorTree = null;
			}

			currentNode = null;

			foreach (SandboxEvent sandboxEvent in registeredEventsToInvoke)
				sandboxEvent.DropInvoker(gameObject);
			registeredEventsToInvoke.Clear();

			foreach (DelegateListener delegateListener in registeredEventListeners)
				delegateListener.Disable();
			registeredEventListeners.Clear();

			Registered = false;
		}

		private void EvaluateCurrentNode()
		{
			if (currentNode != null)
			{
				if (currentNode is InvokerNode)
				{
					InvokerNode invokerNode = currentNode as InvokerNode;

					foreach (EventToInvoke eventToInvoke in invokerNode.EventsToInvoke)
						eventToInvoke.Event.Invoke(gameObject, eventToInvoke.Target);
					
					currentNode = invokerNode.NextNode;

					EvaluateCurrentNode();
				}

				if (currentNode is ListenerNode)
				{
					ListenerNode listenerNode = currentNode as ListenerNode;

					if (listenerNode.EventsToListenFor.Count == 0)
					{
						currentNode = listenerNode.NextNode;

						EvaluateCurrentNode();
					}
				}
			}
		}

		private bool HandleEvent(Dictionary<ListenerNode, EventToListenFor> listeningNodeInfoIndex)
		{
			if (currentNode!=null && currentNode is ListenerNode)
			{
				ListenerNode listenerNode = currentNode as ListenerNode;

				if (listeningNodeInfoIndex.ContainsKey(currentNode as ListenerNode))
				{
					EventToListenFor eventToListenFor = listeningNodeInfoIndex[listenerNode];

					Dictionary<EventToListenFor, bool> eventListenerStatusTable =
						registeredListenerStatusIndex[listenerNode];

					eventListenerStatusTable[eventToListenFor] = true;

					if (!listenerNode.BranchOnCompletion && !listenerNode.CompleteOnFirstEvent)
						foreach (bool registeredListenerStatus in eventListenerStatusTable.Values)
							if (!registeredListenerStatus)
								return false;
						
					foreach
					(
						EventToListenFor eventToListenForIterator in
						new List<EventToListenFor>(eventListenerStatusTable.Keys)
					)
						eventListenerStatusTable[eventToListenForIterator] = false;
					
					EventToListenForWithBranch eventToListenForWithBranch =
						listenerNode.EventsToListenFor.Find
						(
							(eventToListenForWithBranchIterator) =>
							(new EventToListenFor(eventToListenForWithBranchIterator)).Equals(eventToListenFor)
						);

					currentNode =
						listenerNode.BranchOnCompletion?
							eventToListenForWithBranch.NextNode :
							listenerNode.NextNode;

					EvaluateCurrentNode();
				}
			}

			return false;
		}
	}
}