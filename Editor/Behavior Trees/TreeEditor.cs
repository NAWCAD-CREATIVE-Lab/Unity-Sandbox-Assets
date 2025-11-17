// Copyright 2025 U.S. Federal Government (in countries where recognized)
// Copyright 2025 Dakota Crouchelli dakota.h.crouchelli.civ@us.navy.mil

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace CREATIVE.SandboxAssets.Editor.BehaviorTrees
{
	/**
		An editor window for BehaviorTree objects.

		Essentially just contains a TreeView panel.
	*/
	public class TreeEditor : EditorWindow
	{
		[MenuItem("Window/CREATIVE Lab/Sandbox Assets/Behavior Tree Editor")]
		public static void OpenWindow() => GetWindow<TreeEditor>().titleContent = new GUIContent("Behavior Tree");

		void CreateGUI()
		{
			TreeView treeView = new TreeView();

			treeView.style.flexGrow = new StyleFloat(1f);

			rootVisualElement.Add(treeView);

			treeView.PopulateSelection();
		}
	}
}