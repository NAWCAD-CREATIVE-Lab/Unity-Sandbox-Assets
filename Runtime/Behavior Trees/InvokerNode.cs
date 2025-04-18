using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	[type: Serializable]
	public class InvokerNode : Node
	{
		[field: SerializeReference]
		public Node NextNode = null;
		
		[field: SerializeReference]
		public List<EventToInvoke> EventsToInvoke = new List<EventToInvoke>();

		public InvokerNode CleanCloneWithoutNextNode
		{
			get
			{
				InvokerNode copy = new InvokerNode();

				copy.EventsToInvoke = new List<EventToInvoke>();
				if (EventsToInvoke != null)
				{
					foreach (EventToInvoke eventToInvoke in EventsToInvoke)
					{
						EventToInvoke cleanEventToInvoke = eventToInvoke.CleanClone;

						if (cleanEventToInvoke != null)
						{
							if (copy.EventsToInvoke.Contains(cleanEventToInvoke))
								throw new InvalidOperationException("An Invoker Node has duplicate Events to Invoke.");
							
							copy.EventsToInvoke.Add(cleanEventToInvoke);
						}
					}
				}
				
				return copy;
			}
		}
	}
}