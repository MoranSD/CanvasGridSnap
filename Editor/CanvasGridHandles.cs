using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class CanvasGridHandles
{
    private static RectTransform lastSelectedRect;
    private static CanvasGridData lastGridData;
    private static Canvas lastCanvas;
    private static bool toolsWereHidden = false;

    static CanvasGridHandles()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;
    }

    private static void OnSelectionChanged()
    {
        if (toolsWereHidden)
        {
            Tools.hidden = false;
            toolsWereHidden = false;
        }

        lastSelectedRect = null;
        lastGridData = null;
        lastCanvas = null;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        DrawAllGrids();

        if (Selection.activeGameObject == null) return;

        RectTransform rectTransform = Selection.activeGameObject.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        CanvasGridData gridData = canvas.GetComponent<CanvasGridData>();
        if (gridData == null || !gridData.snapEnabled) return;

        lastSelectedRect = rectTransform;
        lastGridData = gridData;
        lastCanvas = canvas;

        if (gridData.hideUnityTools && !Tools.hidden)
        {
            Tools.hidden = true;
            toolsWereHidden = true;
        }

        DrawCustomHandles(rectTransform, gridData);
    }

    private static void DrawAllGrids()
    {
        CanvasGridData[] allGrids = Resources.FindObjectsOfTypeAll<CanvasGridData>();

        foreach (CanvasGridData gridData in allGrids)
        {
            if (gridData == null || !gridData.showGrid) continue;

            Canvas canvas = gridData.GetComponent<Canvas>();
            RectTransform canvasRect = canvas?.GetComponent<RectTransform>();

            if (canvasRect == null) continue;

            DrawGridPoints(canvasRect, gridData);
        }
    }

    private static void DrawGridPoints(RectTransform canvasRect, CanvasGridData gridData)
    {
        Rect canvasRect_local = canvasRect.rect;

        float startX = -canvasRect_local.width / 2f;
        float endX = canvasRect_local.width / 2f;
        float startY = -canvasRect_local.height / 2f;
        float endY = canvasRect_local.height / 2f;

        int gridX = 0;
        int gridY = 0;

        for (float x = startX; x <= endX; x += gridData.gridSize.x)
        {
            gridY = 0;
            for (float y = startY; y <= endY; y += gridData.gridSize.y)
            {
                Vector3 pointWorld = canvasRect.TransformPoint(new Vector3(x, y, 0));

                bool isMajor = (gridX % gridData.majorGridMultiplier == 0) && (gridY % gridData.majorGridMultiplier == 0);
                float pointSize = isMajor ? gridData.gridMajorPointSize : gridData.gridPointSize;
                Color pointColor = isMajor ? gridData.gridMajorColor : gridData.gridColor;

                Handles.color = pointColor;

                float handleSize = HandleUtility.GetHandleSize(pointWorld) * pointSize;
                Handles.DrawSolidDisc(pointWorld, Vector3.forward, handleSize);

                gridY++;
            }
            gridX++;
        }
    }

    private static void DrawCustomHandles(RectTransform rectTransform, CanvasGridData gridData)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Handles.color = Color.yellow;

        Handles.DrawLine(corners[0], corners[1]);
        Handles.DrawLine(corners[1], corners[2]);
        Handles.DrawLine(corners[2], corners[3]);
        Handles.DrawLine(corners[3], corners[0]);

        Vector3 center = (corners[0] + corners[2]) / 2f;
        DrawMoveHandle(center, rectTransform, gridData);

        // Углы
        DrawCornerHandle(corners[0], rectTransform, gridData, 0); // Bottom-Left
        DrawCornerHandle(corners[3], rectTransform, gridData, 1); // Bottom-Right  
        DrawCornerHandle(corners[1], rectTransform, gridData, 2); // Top-Left
        DrawCornerHandle(corners[2], rectTransform, gridData, 3); // Top-Right

        // Стороны
        Vector3 midBottom = (corners[0] + corners[3]) / 2f;
        Vector3 midTop = (corners[1] + corners[2]) / 2f;
        Vector3 midLeft = (corners[0] + corners[1]) / 2f;
        Vector3 midRight = (corners[2] + corners[3]) / 2f;

        DrawBottomEdge(midBottom, rectTransform, gridData);
        DrawTopEdge(midTop, rectTransform, gridData);
        DrawLeftEdge(midLeft, rectTransform, gridData);
        DrawRightEdge(midRight, rectTransform, gridData);
    }

    private static void DrawMoveHandle(Vector3 position, RectTransform rectTransform, CanvasGridData gridData)
    {
        float handleSize = HandleUtility.GetHandleSize(position) * 0.06f;

        EditorGUI.BeginChangeCheck();
        Vector3 newPos = Handles.FreeMoveHandle(position, handleSize, Vector3.zero, Handles.CircleHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rectTransform, "Move UI Element");
            MoveRectTransform(rectTransform, position, newPos, gridData);
            EditorUtility.SetDirty(rectTransform);
        }
    }

    private static void MoveRectTransform(RectTransform rectTransform, Vector3 oldPos, Vector3 newPos, CanvasGridData gridData)
    {
        Vector3 delta = newPos - oldPos;

        Vector3 newWorldPos = rectTransform.position + delta;

        if (gridData.snapOnDrag)
        {
            Vector3 localPos = rectTransform.parent.GetComponent<RectTransform>().InverseTransformPoint(newWorldPos);

            localPos.x = Mathf.Round(localPos.x / gridData.gridSize.x) * gridData.gridSize.x;
            localPos.y = Mathf.Round(localPos.y / gridData.gridSize.y) * gridData.gridSize.y;

            newWorldPos = rectTransform.parent.GetComponent<RectTransform>().TransformPoint(localPos);
        }

        rectTransform.position = newWorldPos;
    }

    private static void DrawCornerHandle(Vector3 position, RectTransform rectTransform, CanvasGridData gridData, int cornerIndex)
    {
        float handleSize = HandleUtility.GetHandleSize(position) * 0.1f;

        EditorGUI.BeginChangeCheck();
        Vector3 newPos = Handles.FreeMoveHandle(position, handleSize, Vector3.zero, Handles.RectangleHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rectTransform, "Resize RectTransform");
            ResizeRectTransformFromCorner(rectTransform, position, newPos, cornerIndex, gridData);
            EditorUtility.SetDirty(rectTransform);
        }
    }

    private static void ResizeRectTransformFromCorner(RectTransform rectTransform, Vector3 oldPos, Vector3 newPos, int cornerIndex, CanvasGridData gridData)
    {
        Vector3 delta = newPos - oldPos;

        Vector2 sizeDelta = rectTransform.sizeDelta;
        Vector2 anchoredPos = rectTransform.anchoredPosition;

        // Применяем сетку к изменению
        if (gridData.snapOnResize)
        {
            delta.x = Mathf.Round(delta.x / gridData.gridSize.x) * gridData.gridSize.x;
            delta.y = Mathf.Round(delta.y / gridData.gridSize.y) * gridData.gridSize.y;
        }

        switch (cornerIndex)
        {
            case 0: // Bottom-Left
                sizeDelta.x -= delta.x;
                sizeDelta.y -= delta.y;
                anchoredPos.x += delta.x / 2;
                anchoredPos.y += delta.y / 2;
                break;
            case 1: // Bottom-Right
                sizeDelta.x += delta.x;
                sizeDelta.y -= delta.y;
                anchoredPos.x += delta.x / 2;
                anchoredPos.y += delta.y / 2;
                break;
            case 2: // Top-Left
                sizeDelta.x -= delta.x;
                sizeDelta.y += delta.y;
                anchoredPos.x += delta.x / 2;
                anchoredPos.y += delta.y / 2;
                break;
            case 3: // Top-Right
                sizeDelta.x += delta.x;
                sizeDelta.y += delta.y;
                anchoredPos.x += delta.x / 2;
                anchoredPos.y += delta.y / 2;
                break;
        }

        // Минимальный размер
        sizeDelta.x = Mathf.Max(gridData.gridSize.x, sizeDelta.x);
        sizeDelta.y = Mathf.Max(gridData.gridSize.y, sizeDelta.y);

        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPos;
    }

    // TOP EDGE - тянем вверх/вниз (высота и позиция)
    private static void DrawTopEdge(Vector3 position, RectTransform rectTransform, CanvasGridData gridData)
    {
        float handleSize = HandleUtility.GetHandleSize(position) * 0.08f;

        EditorGUI.BeginChangeCheck();
        var fmh_248_59_638976202078640617 = Quaternion.identity; Vector3 newPos = Handles.FreeMoveHandle(position, handleSize, Vector3.zero, Handles.DotHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rectTransform, "Resize Top Edge");

            float deltaY = newPos.y - position.y;

            if (gridData.snapOnResize)
            {
                deltaY = Mathf.Round(deltaY / gridData.gridSize.y) * gridData.gridSize.y;
            }

            Vector2 sizeDelta = rectTransform.sizeDelta;
            Vector2 anchoredPos = rectTransform.anchoredPosition;

            sizeDelta.y += deltaY;
            sizeDelta.y = Mathf.Max(gridData.gridSize.y, sizeDelta.y);
            anchoredPos.y += deltaY / 2;

            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPos;

            EditorUtility.SetDirty(rectTransform);
        }
    }

    // BOTTOM EDGE - тянем вверх/вниз (высота и позиция)
    private static void DrawBottomEdge(Vector3 position, RectTransform rectTransform, CanvasGridData gridData)
    {
        float handleSize = HandleUtility.GetHandleSize(position) * 0.08f;

        EditorGUI.BeginChangeCheck();
        var fmh_281_59_638976202078649399 = Quaternion.identity; Vector3 newPos = Handles.FreeMoveHandle(position, handleSize, Vector3.zero, Handles.DotHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rectTransform, "Resize Bottom Edge");

            float deltaY = newPos.y - position.y;

            if (gridData.snapOnResize)
            {
                deltaY = Mathf.Round(deltaY / gridData.gridSize.y) * gridData.gridSize.y;
            }

            Vector2 sizeDelta = rectTransform.sizeDelta;
            Vector2 anchoredPos = rectTransform.anchoredPosition;

            sizeDelta.y -= deltaY;
            sizeDelta.y = Mathf.Max(gridData.gridSize.y, sizeDelta.y);
            anchoredPos.y += deltaY / 2;

            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPos;

            EditorUtility.SetDirty(rectTransform);
        }
    }

    // RIGHT EDGE - тянем влево/вправо (ширина и позиция)
    private static void DrawRightEdge(Vector3 position, RectTransform rectTransform, CanvasGridData gridData)
    {
        float handleSize = HandleUtility.GetHandleSize(position) * 0.08f;

        EditorGUI.BeginChangeCheck();
        var fmh_314_59_638976202078652845 = Quaternion.identity; Vector3 newPos = Handles.FreeMoveHandle(position, handleSize, Vector3.zero, Handles.DotHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rectTransform, "Resize Right Edge");

            float deltaX = newPos.x - position.x;

            if (gridData.snapOnResize)
            {
                deltaX = Mathf.Round(deltaX / gridData.gridSize.x) * gridData.gridSize.x;
            }

            Vector2 sizeDelta = rectTransform.sizeDelta;
            Vector2 anchoredPos = rectTransform.anchoredPosition;

            sizeDelta.x += deltaX;
            sizeDelta.x = Mathf.Max(gridData.gridSize.x, sizeDelta.x);
            anchoredPos.x += deltaX / 2;

            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPos;

            EditorUtility.SetDirty(rectTransform);
        }
    }

    // LEFT EDGE - тянем влево/вправо (ширина и позиция)
    private static void DrawLeftEdge(Vector3 position, RectTransform rectTransform, CanvasGridData gridData)
    {
        float handleSize = HandleUtility.GetHandleSize(position) * 0.08f;

        EditorGUI.BeginChangeCheck();
        var fmh_347_59_638976202078655681 = Quaternion.identity; Vector3 newPos = Handles.FreeMoveHandle(position, handleSize, Vector3.zero, Handles.DotHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rectTransform, "Resize Left Edge");

            float deltaX = newPos.x - position.x;

            if (gridData.snapOnResize)
            {
                deltaX = Mathf.Round(deltaX / gridData.gridSize.x) * gridData.gridSize.x;
            }

            Vector2 sizeDelta = rectTransform.sizeDelta;
            Vector2 anchoredPos = rectTransform.anchoredPosition;

            sizeDelta.x -= deltaX;
            sizeDelta.x = Mathf.Max(gridData.gridSize.x, sizeDelta.x);
            anchoredPos.x += deltaX / 2;

            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPos;

            EditorUtility.SetDirty(rectTransform);
        }
    }
}