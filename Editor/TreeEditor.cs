using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	public class TreeEditor : EditorWindow
	{
		[MenuItem("Window/CREATIVE Lab/Sandbox Assets/Behavior Tree Editor")]
		public static void OpenWindow()
		{
			GetWindow<TreeEditor>().titleContent = new GUIContent("Behavior Tree");
		}

		void CreateGUI()
		{
			TreeGraphView treeView = new TreeGraphView();

			treeView.style.flexGrow = new StyleFloat(1f);

			rootVisualElement.Add(treeView);

			treeView.PopulateSelection();
		}
	}
}