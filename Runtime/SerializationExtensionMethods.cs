using System;
using System.Collections;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets
{
	public static class SerializationExtensionMethods
	{
#if UNITY_EDITOR
		static readonly InvalidOperationException isNotManagedReferenceException =
			new InvalidOperationException("This Serialized Property is not a Managed Reference.");
		
		public static bool IsManagedReference(this SerializedProperty serializedProperty)
		{
			return serializedProperty.propertyType == SerializedPropertyType.ManagedReference;
		}
		
		public static bool ManagedReferenceIsNull(this SerializedProperty serializedProperty)
		{
			if (!serializedProperty.IsManagedReference())
				throw isNotManagedReferenceException;
			
			return serializedProperty.managedReferenceId == ManagedReferenceUtility.RefIdNull;
		}

		public static void SetManagedReferenceNull(this SerializedProperty serializedProperty)
		{
			if (!serializedProperty.IsManagedReference())
				throw isNotManagedReferenceException;
			
			serializedProperty.managedReferenceId = ManagedReferenceUtility.RefIdNull;
		}
		
		public static bool ManagedReferenceIsOfType(this SerializedProperty serializedProperty, Type managedReferenceType)
		{
			if (!serializedProperty.IsManagedReference())
				throw isNotManagedReferenceException;
			
			if (serializedProperty.ManagedReferenceIsNull())
				throw new InvalidOperationException
					("This Serialized Property is null, and the Managed Reference type cannot be determined.");
			
			if (managedReferenceType == null)
				throw new ArgumentNullException(nameof(managedReferenceType));
			
			String[] serializedPropertyTypeInfo = serializedProperty.managedReferenceFullTypename.Split();

			return managedReferenceType.IsAssignableFrom
			(
				Type.GetType
				(
					serializedPropertyTypeInfo[1] + ", " +
					serializedPropertyTypeInfo[0]
				)
			);
		}

		public static bool ManagedReferenceEquals(this SerializedProperty serializedProperty, SerializedProperty otherSerializedProperty)
		{
			if (otherSerializedProperty == null)
				throw new ArgumentNullException(nameof(otherSerializedProperty));
			
			if (!serializedProperty.IsManagedReference())
				throw isNotManagedReferenceException;
			
			if (otherSerializedProperty.propertyType != SerializedPropertyType.ManagedReference)
				throw new ArgumentException
					(nameof(otherSerializedProperty), nameof(otherSerializedProperty) + " is not a Managed Reference.");
			
			return serializedProperty.managedReferenceId == otherSerializedProperty.managedReferenceId;
		}

		public static void SetManagedReference(this SerializedProperty serializedProperty, SerializedProperty otherSerializedProperty)
		{
			if (otherSerializedProperty == null)
				throw new ArgumentNullException(nameof(otherSerializedProperty));
			
			if (!serializedProperty.IsManagedReference())
				throw isNotManagedReferenceException;
			
			if (otherSerializedProperty.propertyType != SerializedPropertyType.ManagedReference)
				throw new ArgumentException
					(nameof(otherSerializedProperty), nameof(otherSerializedProperty) + " is not a Managed Reference.");
			
			serializedProperty.managedReferenceId = otherSerializedProperty.managedReferenceId;
		}
#endif
	}
}