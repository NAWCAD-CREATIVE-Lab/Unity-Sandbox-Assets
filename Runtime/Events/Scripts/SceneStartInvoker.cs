// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets.Events
{
	/**
		This class invoked an event on scene start
	*/
	public class SceneStartInvoker : MonoBehaviour
	{
		/**
			The event to invoke on scene start
		*/
		[field: SerializeField] SandboxEvent SceneStarted = null;

		void Start()
		{
			if (Application.isPlaying && isActiveAndEnabled && SceneStarted!=null)
			{
				if (FindObjectsByType<SceneStartInvoker>(FindObjectsSortMode.None).Length > 1)
					Debug.LogWarning
					(
						"There are multiple active Sandbox Event Scene Start Invoker scripts in this scene." + "\n" +
						"The Scene Started event may be invoked multiple times."
					);
				
				SceneStarted.AddInvoker(gameObject);

				SceneStarted.Invoke(gameObject);

				SceneStarted.DropInvoker(gameObject);
			}
		}
	}
}