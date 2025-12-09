using UnityEngine;
using UnityEditor;

//Simply re-styles a gameObject name in the Hiearchy window to be black and all caps.

namespace GabrielBissonnette.Primo
{
    [InitializeOnLoad]
    public static class HierarchySectionHeader
    {
        static HierarchySectionHeader()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
        }

        static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject != null && gameObject.name.StartsWith("//", System.StringComparison.Ordinal))
            {
                EditorGUI.DrawRect(selectionRect, Color.black);
                EditorGUI.DropShadowLabel(selectionRect, gameObject.name.Replace("/", "").ToUpperInvariant());
            }
        }
    }
}