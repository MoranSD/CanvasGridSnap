using UnityEngine;

[ExecuteInEditMode]
public class CanvasGridData : MonoBehaviour
{
    [Header("Grid Settings")]
    public bool showGrid = true;
    public Vector2 gridSize = new Vector2(50, 50);

    [Header("Grid Visual Settings")]
    public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    public Color gridMajorColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public float gridPointSize = 0.04f;
    public float gridMajorPointSize = 0.08f;
    public int majorGridMultiplier = 5;

    [Header("Snap Settings")]
    public bool snapEnabled = true;
    public bool snapOnDrag = true;
    public bool snapOnResize = true;

    [Header("Editor Tools")]
    public bool hideUnityTools = true;
}
