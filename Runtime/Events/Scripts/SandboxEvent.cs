using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets
{
	/**
		An subscribable event-like interface for Unity Objects.

		Objects that invoke the event or listen to it must both pre-register
		themselves, so connections within the project can be centrally tracked.

		Invokers and listeners that are not GameObjects in a scene will have to
		be manually registered in the SandboxEvent inspector window. GameObjects
		can register themselves via script.

		An Invoker outside of the scene can also be set as an "Exclusive" Invoker
		which prevents any other object from invoking the event.

		Each event can be invoked optionally with another object as an argument.
	*/
	[CreateAssetMenu(fileName = "Event", menuName = "NAWCAD CREATIVE Lab/Sandbox Assets/Event")]
	public class SandboxEvent : ScriptableObject
	{
		[field: SerializeField]
		private UnityEngine.Object ExclusiveInvokerFromAssetFolder;

		[field: SerializeField]
		private List<UnityEngine.Object> InvokersFromAssetFolder = new List<UnityEngine.Object>();

		[field: SerializeField]
		private List<SandboxEventAssetListener> ListenersFromAssetFolder = new List<SandboxEventAssetListener>();

		public  IEnumerable<GameObject>	InvokersFromScene { get { return new List<GameObject>(invokersFromScene); } }
		private List<GameObject>		invokersFromScene = new List<GameObject>();

		private List<SandboxEventListener>			listenersFromScene = new List<SandboxEventListener>();
		public  IEnumerable<SandboxEventListener>	ListenersFromScene 
			{ get { return new List<SandboxEventListener>(listenersFromScene); } }
		
#if UNITY_EDITOR
		/**
			Custom editor for the SandboxEvent.
		*/
		[CustomEditor(typeof(SandboxEvent))]
		public class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				SandboxEvent sandboxEvent = target as SandboxEvent;

				GUIStyle headerStyle = new GUIStyle()
				{
					fontStyle = FontStyle.Bold,
					normal = new GUIStyleState{ textColor = Color.white }
				};
				
				serializedObject.Update();
				
				EditorGUILayout.LabelField("Invokers", headerStyle);

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Exclusive Invoker");
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SandboxEvent.ExclusiveInvokerFromAssetFolder)), GUIContent.none);
					EditorGUILayout.EndHorizontal();
					
					if (sandboxEvent.ExclusiveInvokerFromAssetFolder==null)
					{
						EditorGUILayout.PropertyField
						(
							serializedObject.FindProperty(nameof(SandboxEvent.InvokersFromAssetFolder)),
							new GUIContent("Invokers from Assets Folder")
						);
						
						EditorGUILayout.LabelField("Invokers from Scene:");

							EditorGUI.BeginDisabledGroup(true);

								if (sandboxEvent.InvokersFromScene == null || sandboxEvent.InvokersFromScene.Count()==0)
									EditorGUILayout.LabelField("\tEmpty");
								
								else foreach (GameObject invoker in sandboxEvent.InvokersFromScene)
									EditorGUILayout.ObjectField(invoker, typeof(GameObject), false);
							
							EditorGUI.EndDisabledGroup();
					}
				
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Listeners", headerStyle);
					
					EditorGUILayout.PropertyField
					(
						serializedObject.FindProperty(nameof(SandboxEvent.ListenersFromAssetFolder)),
						new GUIContent("Listeners from Assets Folder")
					);
					
					EditorGUILayout.LabelField("Listeners from Scene:");

						EditorGUI.BeginDisabledGroup(true);

						if (sandboxEvent.ListenersFromScene == null || sandboxEvent.ListenersFromScene.Count()==0)
							EditorGUILayout.LabelField("\tEmpty");
						
						else foreach (SandboxEventListener listener in sandboxEvent.ListenersFromScene)
							EditorGUILayout.ObjectField(listener.ListeningObject, typeof(UnityEngine.Object), false);

						EditorGUI.EndDisabledGroup();
					
				serializedObject.ApplyModifiedProperties();
			}

			public override bool RequiresConstantRepaint()
			{
				return true;
			}
		}
#endif
		
		/**
			Checks to make sure that the invoking object is authorized to invoke
			this event, and calls the Invoke method of all SandboxEventListener
			objects with the supplied target object.

			Only works when the application is playing.
		*/
		public void Invoke(UnityEngine.Object invoker, UnityEngine.Object target = null)
		{
			/*
				Restricts event invoking to when the scene is active.

				Not strictly necessary, but helps avoid unintended side effects
			*/
			if (Application.isPlaying)
			{
				if (invoker == null)
					throw new ArgumentNullException("invoker");
				
				if
				(
					invoker!=ExclusiveInvokerFromAssetFolder											&&
					(!(invoker is GameObject) || !invokersFromScene.Contains(invoker as GameObject))	&&
					!InvokersFromAssetFolder.Contains(invoker)
				)
					throw new InvalidOperationException
					(
						"Object \"" + invoker.name + "\" " +
						"attempted to invoke the \"" + name + "\" Sandbox Event " +
						"but has not previously registered as an invoker"
					);
				
				String debugMessage =
					"\tSandbox Event:\t<b>"	+ name			+ "</b>\n" +
					"\t\tInvoker:\t\t"		+ invoker.name;
				
				if (target!=null)
					debugMessage += "\n\t\tTarget:\t\t" + target.name;
				
				Debug.Log(debugMessage);

				for (int i=(listenersFromScene.Count-1); i>=0; i--)
				{
					if (target==null)
					{
						if (listenersFromScene[i].Invoke())
							listenersFromScene.RemoveAt(i);
					}
					
					else if (listenersFromScene[i].Invoke(target))
						listenersFromScene.RemoveAt(i);
				}
				
				for (int i=(ListenersFromAssetFolder.Count-1); i>=0; i--)
				{
					if (ListenersFromAssetFolder[i].Event != this)
						Debug.LogError
						(
							"Cannot invoke listener \"" + ListenersFromAssetFolder[i].name + "\"" +
							" as it is not a listener for this event."
						);
					
					else if (target == null)
					{
						if (ListenersFromAssetFolder[i].Invoke(target))
							ListenersFromAssetFolder.RemoveAt(i);
					}

					else if (ListenersFromAssetFolder[i].Invoke(target))
						ListenersFromAssetFolder.RemoveAt(i);
				}
			}
		}

		/**
			Calls the main Invoke method with target set to null.
		*/
		public void Invoke(UnityEngine.Object invoker)
		{
			Invoke(invoker, null);
		}

		/**
			Registers the supplied GameObject as an authorized invoker of this
			event.

			Will not function with prefabs.

			Will not function if the Application is not playing.
		*/
		public void AddInvoker(GameObject invoker)
		{
			if (Application.isPlaying)
			{
				if (invoker == null)
					throw new ArgumentNullException("invoker");
				
				/*
					Skip adding the invoker if it's a prefab

					Unity treats them as GameObjects and they cause the event
					to be invoked twice
				*/
				if (invoker.scene.name!=null && invoker.scene.rootCount!=0)
				{
					if (ExclusiveInvokerFromAssetFolder!=null)
						throw new InvalidOperationException
						(
							"Object \"" + invoker.name + "\" " +
							"attempted to add itself as an invoker to the \"" + name + "\" Sandbox Event " +
							"but object \"" + ExclusiveInvokerFromAssetFolder.name + "\" already has exclusive rights."
						);
					
					invokersFromScene.Add(invoker as GameObject);
				}
			}
		}

		/**
			Removes the supplied GameObject as a authorized invoker of this
			event.
		*/
		public void DropInvoker(GameObject invoker)
		{
			if (invoker == null)
				throw new ArgumentNullException("invoker");
			
			if(!invokersFromScene.Contains(invoker))
				throw new InvalidOperationException
				(
					"Object \"" + invoker.name + "\" " +
					"attempted to drop itself as an invoker to the \"" + name + "\" Sandbox Event " +
					"but has not previously registered as an invoker"
				);
			
			invokersFromScene.Remove(invoker as GameObject);
		}

		/**
			Registers the supplied object as a listener of this event.

			Will not function if the ListeningObject is a prefab.

			Will not function if the Application is not playing.
		*/
		public void AddListener(SandboxEventListener listener)
		{
			if (Application.isPlaying)
			{
				if (listener == null)
					throw new ArgumentNullException("listener");
				
				if (listener.Event == null)
					throw new ArgumentException
					(
						"Object \"" + listener.ListeningObject.name + "\" " +
						"attempted to add a listener to the \"" + name + "\" Sandbox Event " +
						"but the Event field of the Listener is null.",
						"listener"
					);
				
				else if (listener.Event != this)
					throw new ArgumentException
					(
						"Object \"" + listener.ListeningObject.name + "\" " +
						"attempted to add a listener to the \"" + name + "\" Sandbox Event " +
						"but the Event field of the Listener cooresponds to the \"" + listener.Event.name + "\" " +
						"instead of this one.",
						"listener"
					);
				
				if (listener is SandboxEventAssetListener)
					throw new ArgumentException
					(
						"Object \"" + listener.ListeningObject.name + "\" " +
						"attempted to add a listener to the \"" + name + "\" Sandbox Event through AddListener " +
						"but the listener is a SandboxEventAssetListener. " +
						"This listener must be added through ListenersFromAssetFolder.",
						"listener"
					);
				
				else if (listener.ListeningObject == null)
					throw new ArgumentException
					(
						"An object attempted to add a listener to the \"" + name + "\" Sandbox Event " +
						"but the ListeningObject field of the Listener is null.",
						"listener"
					);
				
				else if (!(listener.ListeningObject is GameObject))
					throw new ArgumentException
					(
						"Object \"" + listener.ListeningObject.name + "\" " +
						"attempted to add a listener to the \"" + name + "\" Sandbox Event " +
						"but the listener does not appear to be in the Scene or the Asset Folder.",
						"listener"
					);
				
				else
				{
					GameObject listeningObject = listener.ListeningObject as GameObject;

					if (listeningObject.scene.name!=null && listeningObject.scene.rootCount!=0)
					listenersFromScene.Add(listener);
				}
			}
		}

		/**
			Removes the supplied object as a listener of this event.
		*/
		public void DropListener(SandboxEventListener listener)
		{
			if (listener == null)
				throw new ArgumentNullException("listener");
			
			if (listener.Event == null)
				throw new ArgumentException
				(
					"Object \"" + listener.ListeningObject.name + "\" " +
					"attempted to drop a listener from the \"" + name + "\" Sandbox Event " +
					"but the Event field of the Listener is null.",
					"listener"
				);
			
			if (listener.Event != this)
				throw new ArgumentException
				(
					"Object \"" + listener.ListeningObject.name + "\" " +
					"attempted to drop a listener from the \"" + name + "\" Sandbox Event " +
					"but the Event field of the Listener cooresponds to the \"" + listener.Event.name + "\" " +
					"instead of this one.",
					"listener"
				);
			
			if (!listenersFromScene.Contains(listener))
				throw new ArgumentException
				(
					listener.ListeningObject==null?
					"An object " :
					"Object \"" + listener.ListeningObject.name + "\" " +
					"attempted to drop a listener from the \"" + name + "\" Sandbox Event " +
					"but has not previously added the listener provided.",
					"listener"
				);
			
			listenersFromScene.Remove(listener);
		}
	}
}
