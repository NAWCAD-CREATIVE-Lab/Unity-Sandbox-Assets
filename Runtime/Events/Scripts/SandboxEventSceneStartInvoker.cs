using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets
{
	/**
		This class invoked an event on scene start
	*/
	public class SandboxEventSceneStartInvoker : MonoBehaviour
	{
		/**
			The event to invoke on scene start
		*/
		[field: SerializeField]
		private SandboxEvent SceneStarted;

		void Start()
		{
			if (Application.isPlaying && isActiveAndEnabled)
			{
				if (FindObjectsByType<SandboxEventSceneStartInvoker>(FindObjectsSortMode.None).Length > 1)
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