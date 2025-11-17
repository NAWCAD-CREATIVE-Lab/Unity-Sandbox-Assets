// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets.Items
{
	/**
		Stores a reference to an Item.Record.

		Attached to an instantiated Item to facilitate future instantiation in a
		2D Icon or 3D Model form.
	*/
	public class ItemReference : MonoBehaviour
	{
		/**
			The record of an Item that can be used to instantiate more of the
			same Item.
		*/
		public SandboxItem.Record ItemRecord { get; private set; } = null;

#if UNITY_EDITOR
		event Action repaintEditor;
		
		/**
			A custom editor for an ItemReference.
		*/
		[type: CustomEditor(typeof(ItemReference))]
		class Editor : UnityEditor.Editor
		{
			void OnEnable()		=> (target as ItemReference).repaintEditor += Repaint;
			void OnDisable()	=> (target as ItemReference).repaintEditor -= Repaint;
			
			public override void OnInspectorGUI()
			{
				ItemReference itemReference = target as ItemReference;
				
				EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField("Item", itemReference.ItemRecord.Item, typeof(SandboxItem), false);
				EditorGUI.EndDisabledGroup();
			}
		}
#endif

		/**
			Sets the Item record stored in this ItemReference.
		*/
		public void SetItem(SandboxItem.Record itemRecord)
		{
			if (itemRecord == null)
				throw new ArgumentNullException(nameof(itemRecord));

			ItemRecord = itemRecord;

#if UNITY_EDITOR
				if (repaintEditor != null)
					repaintEditor();
#endif
		}

		/**
			Throw an exception if the Item record stored in this ItemReference
			is null.
		*/
		public void NullCheckItem()
		{
			if (ItemRecord == null)
				throw new InvalidOperationException
					("An " + nameof(ItemReference) + " does not contain a valid " + nameof(ItemRecord));
		}
	}
}