using UnityEngine;
using UnityEditor;

public class ReplaceWithPrefab : EditorWindow
{
    private GameObject prefab;

    [MenuItem("Tools/Replace Window")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceWithPrefab>("Replace Objects");
    }

    private void OnGUI()
    {
        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab to use:", prefab, typeof(GameObject), false);

        if (GUILayout.Button("Replace Selected Objects"))
        {
            if (prefab == null)
            {
                Debug.LogError("Assign a prefab first!");
                return;
            }

            GameObject[] selection = Selection.gameObjects;

            for (int i = 0; i < selection.Length; i++)
            {
                GameObject oldObj = selection[i];

                // Ensure we aren't trying to replace the prefab itself if it's in the scene
                if (PrefabUtility.GetCorrespondingObjectFromSource(oldObj) == prefab && PrefabUtility.IsPartOfPrefabAsset(oldObj)) continue;

                GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(newObj, "Replace With Prefab");

                newObj.transform.SetParent(oldObj.transform.parent);
                newObj.transform.localPosition = oldObj.transform.localPosition;
                newObj.transform.localRotation = oldObj.transform.localRotation;
                newObj.transform.localScale = oldObj.transform.localScale;

                Undo.DestroyObjectImmediate(oldObj);
            }
        }
    }
}