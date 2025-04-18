using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace CREATIVE.SandboxAssets.BehaviorTrees
{
	public class TreeEditor : EditorWindow
	{
		TreeGraphView treeView = null;
		
		[MenuItem("Window/CREATIVE Lab/Sandbox Assets/Behavior Tree Editor")]
		public static void OpenWindow()
		{
			TreeEditor window = GetWindow<TreeEditor>();
			window.titleContent = new GUIContent("Behavior Tree");
		}

		void CreateGUI()
		{
			treeView = new TreeGraphView();

			treeView.style.flexGrow = new StyleFloat(1f);

			rootVisualElement.Add(treeView);

			OnSelectionChange();
		}

		void OnSelectionChange()
		{
			UnityEngine.Object selectedObject = Selection.activeObject;

			if (selectedObject!=null && selectedObject is BehaviorTree)
				treeView.PopulateView(new SerializedObject(selectedObject));
		}
	}
}