// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using CREATIVE.SandboxAssets.BehaviorTrees;

using Node = CREATIVE.SandboxAssets.BehaviorTrees.Node;

namespace CREATIVE.SandboxAssets.Editor.BehaviorTrees
{
	/**
		For each Node type or Node record type handled by this assembly, this
		class serves as a lookup table for the appropriate INodeViewFactory
		object that can be used to create a representative INodeView
		object.

		The private tables in this class should be updated whenever a new Node
		type is added to the assembly.
	*/
	static public class NodeViewFactoryLookup
	{
		static readonly ReadOnlyDictionary<Type, Type> recordTypeIndex =
			new ReadOnlyDictionary<Type, Type>
			(
				new Dictionary<Type, Type>()
				{
					{ typeof(InvokerNode),      typeof(InvokerNode.Record)  },
					{ typeof(ListenerNode),     typeof(ListenerNode.Record) }
				}
			);

		static readonly ReadOnlyDictionary<Type, INodeViewFactory> factoryIndex =
			new ReadOnlyDictionary<Type, INodeViewFactory>
			(
				new Dictionary<Type, INodeViewFactory>()
				{
					{ typeof(InvokerNode.Record),   new InvokerNodeView.Factory()   },
					{ typeof(ListenerNode.Record),  new ListenerNodeView.Factory()  },
				}
			);

		/**
			Returns the appropriate INodeViewFactory object used to create
			INodeView objects representative of the given Node record type.
		*/
		public static INodeViewFactory FromRecordType(Type recordType)
		{
			if (recordType == null)
				throw new ArgumentNullException(nameof(recordType));

			if (!typeof(Node.IRecord<Node>).IsAssignableFrom(recordType))
				throw new ArgumentException
				(
					nameof(recordType),
					nameof(recordType) +
					" is not a subtype of " + typeof(Node.IRecord<Node>) + "."
				);

			if (!factoryIndex.ContainsKey(recordType))
				throw new ArgumentException
				(
					nameof(recordType),
					nameof(recordType) +
					" is not a node record subtype from which this assembly knows how to create a node view."
				);

			return factoryIndex[recordType];
		}

		/**
			Returns the appropriate INodeViewFactory object used to create
			INodeView objects representative of the given Node type.
		*/
		public static INodeViewFactory FromNodeType(Type nodeType)
		{
			if (nodeType == null)
				throw new ArgumentNullException(nameof(nodeType));

			if (!typeof(Node).IsAssignableFrom(nodeType))
				throw new ArgumentException
				(
					nameof(nodeType),
					nameof(nodeType) +
					" is not a subtype of " + typeof(Node) + "."
				);

			if (!recordTypeIndex.ContainsKey(nodeType))
				throw new ArgumentException
				(
					nameof(nodeType),
					nameof(nodeType) +
					" is not a node subtype from which this assembly knows how to create a node view."
				);

			return factoryIndex[recordTypeIndex[nodeType]];
		}

		/**
			Returns an enumerable of all the INodeViewFactory objects this class
			contains.
		*/
		public static IEnumerable<INodeViewFactory> All { get => factoryIndex.Values; }
	}
}