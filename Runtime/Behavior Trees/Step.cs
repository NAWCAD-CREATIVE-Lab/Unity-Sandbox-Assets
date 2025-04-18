using System;
using System.Collections;
using System.Collections.Generic;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	[type: Serializable]
	public class Step
	{
		public List<EventToListenFor> EventsToListenFor = new List<EventToListenFor>();
		
		public bool CompleteOnFirstEvent = false;

		public List<EventToInvoke> EventsToInvoke = new List<EventToInvoke>();

		public ListenerNode CleanCloneAsNodes
		{
			get
			{
				InvokerNode invokerNode = new InvokerNode();
				
				invokerNode.EventsToInvoke = EventsToInvoke;

				invokerNode = invokerNode.CleanCloneWithoutNextNode;
				
				ListenerNode listenerNode = new ListenerNode();

				listenerNode.BranchOnCompletion = false;

				listenerNode.CompleteOnFirstEvent = CompleteOnFirstEvent;

				listenerNode.EventsToListenFor = new List<EventToListenForWithBranch>();
				if (EventsToListenFor != null)
					foreach (EventToListenFor eventToListenFor in EventsToListenFor)
						if (eventToListenFor != null)
							listenerNode.EventsToListenFor.Add(new EventToListenForWithBranch(eventToListenFor));

				listenerNode = listenerNode.CleanCloneWithoutNextNodes;

				listenerNode.NextNode = invokerNode;
				
				return listenerNode;
			}
		}

		public static ListenerNode CleanCloneListAsNodes(List<Step> steps)
		{
			if (steps==null)
				return null;

			List<Step> nullCheckedSteps = new List<Step>();

			foreach (Step step in steps)
				if (step != null)
					nullCheckedSteps.Add(step);
			
			if (nullCheckedSteps.Count == 0)
				return null;
			
			ListenerNode lastCreatedNode = null;

			for (int i=(steps.Count-1); i>=0; i--)
			{
				ListenerNode newNode = steps[i].CleanCloneAsNodes;

				(newNode.NextNode as InvokerNode).NextNode = lastCreatedNode;

				lastCreatedNode = newNode;
			}

			return lastCreatedNode;
		}
	}
}