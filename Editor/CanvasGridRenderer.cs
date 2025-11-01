using UnityEngine;
using UnityEditor;
//using UnityEditor.SceneHierarchyHooks;

[InitializeOnLoad]
public static class CanvasGridRenderer
{
    static CanvasGridRenderer()
    {
        EditorApplication.update += UpdateGridDisplay;
    }

    private static void UpdateGridDisplay()
    {
        // Ищем все Canvas с CanvasGridData компонентом
        CanvasGridData[] allGrids = Resources.FindObjectsOfTypeAll<CanvasGridData>();

        foreach (CanvasGridData gridData in allGrids)
        {
            if (gridData == null || !gridData.showGrid) continue;

            Canvas canvas = gridData.GetComponent<Canvas>();
            RectTransform canvasRect = canvas?.GetComponent<RectTransform>();

            if (canvasRect == null) continue;

            // Запускаем отрисовку через SceneView
            SceneView.RepaintAll();
            break; // Достаточно одного вызова для перерисовки всех SceneView
        }
    }
}
