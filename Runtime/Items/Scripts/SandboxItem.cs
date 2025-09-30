using System;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

namespace CREATIVE.SandboxAssets.Items
{
	/**
		A wrapper class to bind together:
			- A 3D model of a portable item
			- Its representative 2D icon
			- An event that can be invoked when the item is used to interact
			  with something
	*/
	[CreateAssetMenu(fileName = "Item", menuName = "NAWCAD CREATIVE Lab/Sandbox Assets/Item")]
	public class SandboxItem : ScriptableObject
	{
		/**
			The 2D Icon that represents this Item.

			Should not be null.
		*/
		[field: SerializeField]	RectTransform Icon;

		/**
			The 3D Model that represents this Item.

			Should not be null.
		*/
		[field: SerializeField]	Transform Model;

		/**
			The SandboxEvent that should be invoked when this item is used
			as a tool.

			Can be null if this item does not represent a usable tool.
		*/
		[field: SerializeField]	SandboxEvent InteractEvent;

		/**
			A class used to wrap and pass around the fields that define an
			item while ensuring they won't be modified.
		*/
		public class Record
		{
			/**
				The original Item object that this record was created from.

				Will never be null.
			*/
			public readonly SandboxItem Item;

			/**
				The 2D Icon that represents this Item.

				Will never be null.
			*/
			public readonly RectTransform Icon;

			/**
				The 3D Model that represents this Item.

				Will never be null.
			*/
			public readonly Transform Model;

			/**
				The SandboxEvent that should be invoked when this item is used
				as a tool.

				Could be null if this item does not represent a usable tool.
			*/
			public readonly SandboxEvent InteractEvent;

			public Record(SandboxItem item)
			{
				if (item == null)
					throw new ArgumentNullException(nameof(item));
				
				if (item.Icon == null)
					throw new ArgumentException(nameof(item), nameof(item) + " does not contain an icon reference");
				
				if (item.Model == null)
					throw new ArgumentException(nameof(item), nameof(item) + " does not contain an model reference");

				Item = item;

				Icon = item.Icon;

				Model = item.Model;

				InteractEvent = item.InteractEvent;
			}

			/**
				This function instantiates the Icon object of this Item in the
				scene, expanding it to fill the space defined by its parent
				
				It also ensures that the new RectTransform has an ItemReference
				component attached with a reference to this Item.Record, for
				future instantiation.
			*/
			public ItemReference InstantiateAsIcon(RectTransform parent = null)
			{
				RectTransform newIcon;

				if (parent == null)
					newIcon = (Instantiate(Icon.gameObject) as GameObject).transform as RectTransform;
				else
					newIcon = (Instantiate(Icon.gameObject, parent) as GameObject).transform as RectTransform;

				newIcon.anchorMin = new Vector2(0, 0);
				newIcon.anchorMax = new Vector2(1, 1);
				newIcon.pivot = new Vector2(0.5f, 0.5f);

				newIcon.offsetMin = new Vector2(0, 0);
				newIcon.offsetMax = new Vector2(0, 0);

				return enforceItemReference(newIcon.gameObject);
			}

			/**
				This function instantiates the Model object of this Item in the
				scene, zero-ing out its Transform in the process.
				
				It also ensures that the new Transform has an ItemReference
				component attached with a reference to this Item.Record, for
				future instantiation.
			*/
			public ItemReference InstantiateAsModel(Transform parent = null)
			{
				Transform newModel;

				if (parent == null)
					newModel = (Instantiate(Model.gameObject) as GameObject).transform;
				else
					newModel = (Instantiate(Model.gameObject, parent) as GameObject).transform;

				newModel.localPosition = Vector3.zero;
				newModel.localRotation = Quaternion.identity;
				newModel.localScale = Vector3.one;

				return enforceItemReference(newModel.gameObject);
			}

			ItemReference enforceItemReference(GameObject obj)
			{
				ItemReference item = obj.GetComponent<ItemReference>();

				if (item == null)
					item = obj.AddComponent<ItemReference>();

				item.SetItem(this);

				return item;
			}
		}

		/**
			Returns a new Item.Record object with the data contained in this
			Item.
		*/
		public Record CreateRecord() => new Record(this);
	}
}