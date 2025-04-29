using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	[type: Serializable]
	public class ListenerNode : Node
	{
		[field: SerializeReference]
		public Node NextNode = null;
		
		public bool BranchOnCompletion = false;
		
		public bool CompleteOnFirstEvent = false;
		
		[field: SerializeReference]
		public List<EventToListenForWithBranch> EventsToListenFor = new List<EventToListenForWithBranch>();

		public ListenerNode CleanCloneWithoutNextNodes
		{
			get
			{
				ListenerNode copy = new ListenerNode();

#if UNITY_EDITOR
				copy.Position = new Vector2() { x=Position.x, y=Position.y };
#endif

				copy.BranchOnCompletion = BranchOnCompletion;

				copy.CompleteOnFirstEvent = CompleteOnFirstEvent;

				copy.EventsToListenFor = new List<EventToListenForWithBranch>();
				if (EventsToListenFor != null)
				{
					foreach (EventToListenForWithBranch eventToListenFor in EventsToListenFor)
					{
						if (eventToListenFor != null)
						{
							EventToListenForWithBranch cleanEventToListenFor =
								eventToListenFor.CleanCloneWithoutNextNode;

							if (cleanEventToListenFor != null)
							{
								if (copy.EventsToListenFor.Contains(cleanEventToListenFor))
									throw new InvalidOperationException
										("A Listener Node has duplicate Events to Listen For.");
								
								copy.EventsToListenFor.Add(cleanEventToListenFor);
							}
						}
					}
				}
				
				return copy;
			}
		}
	}
}