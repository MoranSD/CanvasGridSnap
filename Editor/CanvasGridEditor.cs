using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CanvasGridData))]
public class CanvasGridEditor : Editor
{
    private CanvasGridData gridData;
    private Canvas canvas;
    private RectTransform canvasRect;

    private void OnEnable()
    {
        gridData = (CanvasGridData)target;
        canvas = gridData.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Snap All UI Elements to Grid", GUILayout.Height(30)))
        {
            SnapAllUIElements();
        }

        if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
        {
            gridData.showGrid = false;
            SceneView.RepaintAll();
        }
    }

    private void SnapRectTransformToGrid(RectTransform rectTransform)
    {
        Undo.RecordObject(rectTransform, "Snap to Grid");

        // Получаем текущие мировые координаты углов
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        // Преобразуем углы в локальные координаты родителя
        RectTransform parentRect = rectTransform.parent.GetComponent<RectTransform>();
        Vector3[] localCorners = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            localCorners[i] = parentRect.InverseTransformPoint(corners[i]);
        }

        // Вычисляем границы элемента в локальных координатах
        float minX = Mathf.Min(localCorners[0].x, localCorners[1].x, localCorners[2].x, localCorners[3].x);
        float maxX = Mathf.Max(localCorners[0].x, localCorners[1].x, localCorners[2].x, localCorners[3].x);
        float minY = Mathf.Min(localCorners[0].y, localCorners[1].y, localCorners[2].y, localCorners[3].y);
        float maxY = Mathf.Max(localCorners[0].y, localCorners[1].y, localCorners[2].y, localCorners[3].y);

        // Привязываем края к сетке
        float snappedMinX = Mathf.Round(minX / gridData.gridSize.x) * gridData.gridSize.x;
        float snappedMaxX = Mathf.Round(maxX / gridData.gridSize.x) * gridData.gridSize.x;
        float snappedMinY = Mathf.Round(minY / gridData.gridSize.y) * gridData.gridSize.y;
        float snappedMaxY = Mathf.Round(maxY / gridData.gridSize.y) * gridData.gridSize.y;

        // Вычисляем новый размер и позицию
        float newWidth = snappedMaxX - snappedMinX;
        float newHeight = snappedMaxY - snappedMinY;

        // Убеждаемся, что размер не меньше минимального
        newWidth = Mathf.Max(gridData.gridSize.x, newWidth);
        newHeight = Mathf.Max(gridData.gridSize.y, newHeight);

        // Вычисляем новый центр
        float newCenterX = (snappedMinX + snappedMaxX) / 2f;
        float newCenterY = (snappedMinY + snappedMaxY) / 2f;

        // Применяем изменения
        rectTransform.sizeDelta = new Vector2(newWidth, newHeight);

        // Преобразуем новый центр обратно в мировые координаты и устанавливаем позицию
        Vector3 newWorldCenter = parentRect.TransformPoint(new Vector3(newCenterX, newCenterY, 0));
        rectTransform.position = newWorldCenter;

        EditorUtility.SetDirty(rectTransform);
    }

    private void SnapAllUIElements()
    {
        if (canvas == null) return;

        RectTransform[] allRects = canvas.GetComponentsInChildren<RectTransform>();

        // Создаем группу отмены для всех изменений
        Undo.SetCurrentGroupName("Snap All UI Elements to Grid");
        int group = Undo.GetCurrentGroup();

        int count = 0;
        foreach (RectTransform rect in allRects)
        {
            if (rect != canvasRect && rect.GetComponent<CanvasGridData>() == null)
            {
                Undo.RecordObject(rect, "Snap UI Element");
                SnapRectTransformToGrid(rect);
                count++;
            }
        }

        // Завершаем группу отмены
        Undo.CollapseUndoOperations(group);

        Debug.Log($"Snap applied to {count} UI elements");
        SceneView.RepaintAll();
    }
}