// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;

namespace CREATIVE.SandboxAssets.Editor.BehaviorTrees
{
	/**
		A collection of extension methods that make it easier and cleaner to
		read, write, and error-check SerializedProperty objects.
	*/
	public static class SerializedPropertyExtensions
	{
		static readonly InvalidOperationException isNotManagedReferenceException =
			new InvalidOperationException("This Serialized Property is not a Managed Reference.");

		/**
			Whether or not this SerializedProperty is a managed reference,
			which means it references an object that does not derive from
			UnityEngine.Object.
		*/
		public static bool IsManagedReference(this SerializedProperty serializedProperty)
		{
			if (serializedProperty == null)
				throw new ArgumentNullException(nameof(serializedProperty));

			return serializedProperty.propertyType == SerializedPropertyType.ManagedReference;
		}

		/**
			Whether or not this SerializedProperty is null.

			Throws an exception if this SerializedProperty is not a managed
			reference.
		*/
		public static bool ManagedReferenceIsNull(this SerializedProperty serializedProperty)
		{
			if (serializedProperty == null)
				throw new ArgumentNullException(nameof(serializedProperty));

			if (!serializedProperty.IsManagedReference())
				throw isNotManagedReferenceException;

			return serializedProperty.managedReferenceId == ManagedReferenceUtility.RefIdNull;
		}

		/**
			Sets this SerializedProperty to null.

			Throws an exception if this SerializedProperty is not a managed
			reference.
		*/
		public static void SetManagedReferenceNull(this SerializedProperty serializedProperty)
		{
			if (serializedProperty == null)
				throw new ArgumentNullException(nameof(serializedProperty));

			if (!serializedProperty.IsManagedReference())
				throw isNotManagedReferenceException;

			serializedProperty.managedReferenceId = ManagedReferenceUtility.RefIdNull;
		}

		/**
			Returns a type object representing the type that this
			SerializedProperty will be de-serialized into.

			Throws an exception if this SerializedProperty is not a managed
			reference, or if it is null, as type information is not
			serialized for null references.
		*/
		public static Type GetManagedReferenceType(this SerializedProperty serializedProperty)
		{
			if (serializedProperty == null)
				throw new ArgumentNullException(nameof(serializedProperty));

			if (!serializedProperty.IsManagedReference())
				throw isNotManagedReferenceException;

			if (serializedProperty.ManagedReferenceIsNull())
				throw new InvalidOperationException
					("This Serialized Property is null, and the Managed Reference type cannot be determined.");

			String[] serializedPropertyTypeInfo = serializedProperty.managedReferenceFullTypename.Split();

			return Type.GetType(serializedPropertyTypeInfo[1] + ", " + serializedPropertyTypeInfo[0]);
		}

		/**
			Whether or not this SerializedProperty will be de-serialized into
			an object assignable to the given type.
			
			Throws an exception if this SerializedProperty is not a managed
			reference, or if it is null, as type information is not
			serialized for null references.
		*/
		public static bool ManagedReferenceIsOfType
			(this SerializedProperty serializedProperty, Type managedReferenceType)
		{
			if (serializedProperty == null)
				throw new ArgumentNullException(nameof(serializedProperty));

			if (managedReferenceType == null)
				throw new ArgumentNullException(nameof(managedReferenceType));

			return managedReferenceType.IsAssignableFrom(serializedProperty.GetManagedReferenceType());
		}

		/**
			Whether or not this SerializedProperty is an equavalent managed
			reference to the given SerializedProperty.

			Throws an exception if this SerializedProperty or the given
			SerializedProperty are not managed references.
		*/
		public static bool ManagedReferenceEquals
			(this SerializedProperty serializedProperty, SerializedProperty otherSerializedProperty)
		{
			if (serializedProperty == null)
				throw new ArgumentNullException(nameof(serializedProperty));

			if (otherSerializedProperty == null)
				throw new ArgumentNullException(nameof(otherSerializedProperty));

			if (!serializedProperty.IsManagedReference())
				throw isNotManagedReferenceException;

			if (otherSerializedProperty.propertyType != SerializedPropertyType.ManagedReference)
				throw new ArgumentException
					(nameof(otherSerializedProperty), nameof(otherSerializedProperty) + " is not a Managed Reference.");

			return serializedProperty.managedReferenceId == otherSerializedProperty.managedReferenceId;
		}

		/**
			Sets the managed reference for this SerializedProperty to the
			managed reference of the given SerializedProperty.

			Throws an exception if this SerializedProperty or the given
			SerializedProperty are not managed references.
		*/
		public static void SetManagedReference
			(this SerializedProperty serializedProperty, SerializedProperty otherSerializedProperty)
		{
			if (serializedProperty == null)
				throw new ArgumentNullException(nameof(serializedProperty));

			if (otherSerializedProperty == null)
				throw new ArgumentNullException(nameof(otherSerializedProperty));

			if (!serializedProperty.IsManagedReference())
				throw isNotManagedReferenceException;

			if (otherSerializedProperty.propertyType != SerializedPropertyType.ManagedReference)
				throw new ArgumentException
					(nameof(otherSerializedProperty), nameof(otherSerializedProperty) + " is not a Managed Reference.");

			serializedProperty.managedReferenceId = otherSerializedProperty.managedReferenceId;
		}
	}
}