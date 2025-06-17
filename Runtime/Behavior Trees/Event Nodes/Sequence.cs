using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CREATIVE.SandboxAssets.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	/**
		A serialized list of Step objects that form a sequence of behavior
		automatically initiated at the start of a scene, and performed
		back-and-forth by the user and the application until the sequence is
		complete.
	*/
	public class Sequence : MonoBehaviour
	{
		[field: SerializeField] List<Step> Steps = new List<Step>();

		Graph registeredGraph;

		Action registeredGraphTeardown;

		int currentNode;

		int currentStep { get => (currentNode/2) + 1; }

		bool registered = false;

		bool completed = false;

#if UNITY_EDITOR
		private event Action repaintEditor;
		
		[CustomEditor(typeof(Sequence))]
		public class Editor : UnityEditor.Editor
		{
			void OnEnable()		=> (target as Sequence).repaintEditor += Repaint;
			void OnDisable()	=> (target as Sequence).repaintEditor -= Repaint;
			
			public override void OnInspectorGUI()
			{
				serializedObject.Update();

				Sequence sequence = target as Sequence;

				if (Application.isPlaying && sequence.registered)
				{
					if (sequence.completed)
						EditorGUILayout.LabelField("Sequence Completed");
					else
						EditorGUILayout.LabelField
							("Current Step: " + sequence.currentStep);
					
					EditorGUILayout.Space(20);
				}

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Steps)));

				serializedObject.ApplyModifiedProperties();
			}
		}
#endif
		
		void OnEnable() => ReRegister();
		void Start()	=> ReRegister();

#if UNITY_EDITOR
		void OnValidate() => ReRegister();
#endif
		
		void OnDisable()	=> UnRegister();
		void OnDestroy()	=> UnRegister();

		void ReRegister()
		{
			UnRegister();

			if (Application.isPlaying && isActiveAndEnabled && Steps!=null)
			{
				registeredGraph = new Graph
				(
					Steps,
					gameObject,
					(currentNode) => IncrementNode(),
					CompleteSequence,
					out registeredGraphTeardown
				);

				currentNode = 0;
					
				registered = true;

				registeredGraph.Evaluate();
			}
		}

		void UnRegister()
		{
			if (registered)
			{
				registeredGraphTeardown();

				registered = false;
			}

			completed = false;
		}

		void IncrementNode()
		{
			currentNode++;

#if UNITY_EDITOR
			if (currentNode%2==0 && repaintEditor!=null)
				repaintEditor();
#endif
		}

		void CompleteSequence()
		{
			completed = true;

#if UNITY_EDITOR
			if (repaintEditor != null)
				repaintEditor();
#endif
		}
	}
}