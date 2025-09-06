using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(WallItem))]
public class WallItemEditor : Editor
{
    private SerializedProperty wallConfigProp;
    private SerializedProperty gridSystemProp;
    
    void OnEnable()
    {
        wallConfigProp = serializedObject.FindProperty("wallConfig");
        gridSystemProp = serializedObject.FindProperty("gridSystem");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        WallItem wallItem = (WallItem)target;
        
        EditorGUILayout.LabelField("Wall Item Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Grid System reference
        EditorGUILayout.PropertyField(gridSystemProp);
        
        EditorGUILayout.Space();
        
        // Wall Configuration
        EditorGUILayout.LabelField("Wall Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        var directionProp = wallConfigProp.FindPropertyRelative("direction");
        var lengthProp = wallConfigProp.FindPropertyRelative("wallLength");
        var heightProp = wallConfigProp.FindPropertyRelative("wallHeight");
        var offsetProp = wallConfigProp.FindPropertyRelative("baseOffset");
        
        EditorGUILayout.PropertyField(directionProp);
        EditorGUILayout.PropertyField(lengthProp);
        EditorGUILayout.PropertyField(heightProp);
        EditorGUILayout.PropertyField(offsetProp);
        
        EditorGUILayout.Space();
        
        // Wall Surface
        EditorGUILayout.LabelField("Wall Surface", EditorStyles.boldLabel);
        var providesSurfaceProp = wallConfigProp.FindPropertyRelative("providesWallSurface");
        var useCustomShapeProp = wallConfigProp.FindPropertyRelative("useCustomWallShape");
        var customShapeProp = wallConfigProp.FindPropertyRelative("customWallShape");
        
        EditorGUILayout.PropertyField(providesSurfaceProp);
        EditorGUILayout.PropertyField(useCustomShapeProp);
        
        if (useCustomShapeProp.boolValue)
        {
            EditorGUILayout.PropertyField(customShapeProp, true);
        }
        
        EditorGUILayout.Space();
        
        // Visualization
        EditorGUILayout.LabelField("Visualization", EditorStyles.boldLabel);
        var baseOccupiedColorProp = wallConfigProp.FindPropertyRelative("baseOccupiedColor");
        var wallSurfaceColorProp = wallConfigProp.FindPropertyRelative("wallSurfaceColor");
        var showInSceneProp = wallConfigProp.FindPropertyRelative("showInSceneView");
        var showWallSurfaceProp = wallConfigProp.FindPropertyRelative("showWallSurfaceInSceneView");
        
        EditorGUILayout.PropertyField(baseOccupiedColorProp);
        EditorGUILayout.PropertyField(wallSurfaceColorProp);
        EditorGUILayout.PropertyField(showInSceneProp);
        EditorGUILayout.PropertyField(showWallSurfaceProp);
        
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space();
        
        // Status information
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        
        if (wallItem.GridSystem != null)
        {
            var basePositions = wallItem.GetBaseOccupiedGridPositions();
            var wallPositions = wallItem.GetWallSurfacePositions();
            bool isValid = wallItem.IsValidPosition();
            
            EditorGUILayout.LabelField($"Base Grid Positions: {basePositions.Count}");
            EditorGUILayout.LabelField($"Wall Surface Positions: {wallPositions.Count}");
            
            if (basePositions.Count > 0)
            {
                EditorGUILayout.LabelField($"First Position: ({basePositions[0].x}, {basePositions[0].y})");
            }
            
            // Validation status
            if (isValid)
            {
                EditorGUILayout.HelpBox("Wall position is valid", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Wall position overlaps with other objects!", MessageType.Error);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No Grid System assigned. Assign one to see wall grid information.", MessageType.Warning);
            
            if (GUILayout.Button("Find Grid System in Scene"))
            {
                var gridSystem = FindObjectOfType<GridVisualization>();
                if (gridSystem != null)
                {
                    wallItem.GridSystem = gridSystem;
                    EditorUtility.SetDirty(wallItem);
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found", "No GridVisualization found in scene. Please add one first.", "OK");
                }
            }
        }
        
        EditorGUILayout.Space();
        
        // Utility buttons
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Snap to Grid"))
        {
            SnapToGrid(wallItem);
        }
        if (GUILayout.Button("Validate Position"))
        {
            ValidatePosition(wallItem);
        }
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
        
        // Force scene repaint if properties changed
        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }
    
    void OnSceneGUI()
    {
        WallItem wallItem = (WallItem)target;
        
        // 检查GridVisualization的主开关和本地设置
        if (wallItem.GridSystem == null || 
            !wallItem.GridSystem.ShowGridSystem ||  // 主开关
            !wallItem.WallConfig.showInSceneView)   // 本地开关
            return;
        
        DrawWallVisualization(wallItem);
        
        // 显示墙面信息标签
        if (wallItem.WallConfig.showInSceneView)
        {
            var basePositions = wallItem.GetBaseOccupiedGridPositions();
            if (basePositions.Count > 0)
            {
                Vector3 labelPos = wallItem.GridSystem.GridToWorld(basePositions[0], 0f) + Vector3.up * 0.5f;
                string info = $"Wall {wallItem.WallConfig.direction}\nCells: {wallItem.GetWallGridCellInfos().Count}";
                Handles.Label(labelPos, info);
            }
        }
    }
    
    private void DrawWallVisualization(WallItem wallItem)
    {
        var basePositions = wallItem.GetBaseOccupiedGridPositions();
        var wallPositions = wallItem.GetWallSurfacePositions();
        bool isValid = wallItem.IsValidPosition();
        
        // 绘制地面占用
        Color baseColor = isValid ? wallItem.WallConfig.baseOccupiedColor : Color.red;
        baseColor.a = 0.3f;
        Handles.color = baseColor;
        
        foreach (var gridPos in basePositions)
        {
            Vector3[] corners = GetGridCellCorners(wallItem.GridSystem, gridPos, 0f);
            Handles.DrawAAConvexPolygon(corners);
        }
        
        // 绘制地面边框
        Handles.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        foreach (var gridPos in basePositions)
        {
            Vector3[] corners = GetGridCellCorners(wallItem.GridSystem, gridPos, 0f);
            for (int i = 0; i < corners.Length; i++)
            {
                int next = (i + 1) % corners.Length;
                Handles.DrawLine(corners[i], corners[next]);
            }
        }
        
        // 绘制墙面格子
        if (wallItem.WallConfig.showWallSurfaceInSceneView && wallItem.WallConfig.providesWallSurface)
        {
            Color wallColor = wallItem.WallConfig.wallSurfaceColor;
            wallColor.a = 0.3f;
            Handles.color = wallColor;
            
            // 使用新的墙面格子信息方法，先绘制填充
            var wallGridInfos = wallItem.GetWallGridCellInfos();
            foreach (var info in wallGridInfos)
            {
                DrawWallGridCellCorners(wallItem.GridSystem, info.basePos, info.heightLevel, info.direction);
            }
            
            // 再绘制边框
            Handles.color = new Color(wallColor.r, wallColor.g, wallColor.b, 1f);
            foreach (var info in wallGridInfos)
            {
                DrawWallGridCellBorder(wallItem.GridSystem, info.basePos, info.heightLevel, info.direction);
            }
        }
        
        // 显示信息标签
        if (basePositions.Count > 0)
        {
            Vector3 labelPos = wallItem.GridSystem.GridToWorld(basePositions[0], 0f) + Vector3.up * 0.5f;
            string info = $"Wall {wallItem.WallConfig.direction}\\nLength: {wallItem.WallConfig.wallLength}\\nHeight: {wallItem.WallConfig.wallHeight}";
            Handles.Label(labelPos, info);
        }
    }
    
    private Vector3[] GetGridCellCorners(GridVisualization gridSystem, Vector2Int gridPos, float height)
    {
        Vector3 bottomLeft = gridSystem.GridToWorld(gridPos, height);
        Vector3 bottomRight = gridSystem.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = gridSystem.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = gridSystem.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    private void DrawWallGridCell(GridVisualization gridSystem, Vector3 wallPos)
    {
        var settings = gridSystem.Settings;
        float cellSize = settings.wallGridSize;
        float halfSize = cellSize * 0.5f;
        
        // 绘制墙面网格线 - 就像第一张图中的红色线框那样
        // 垂直线（左边界）
        Vector3 leftBottom = wallPos + new Vector3(-halfSize, -halfSize, 0);
        Vector3 leftTop = wallPos + new Vector3(-halfSize, halfSize, 0);
        Handles.DrawLine(leftBottom, leftTop);
        
        // 垂直线（右边界）
        Vector3 rightBottom = wallPos + new Vector3(halfSize, -halfSize, 0);
        Vector3 rightTop = wallPos + new Vector3(halfSize, halfSize, 0);
        Handles.DrawLine(rightBottom, rightTop);
        
        // 水平线（下边界）
        Handles.DrawLine(leftBottom, rightBottom);
        
        // 水平线（上边界）
        Handles.DrawLine(leftTop, rightTop);
    }
    
    private void DrawWallGridCellCorners(GridVisualization gridSystem, Vector2Int baseGridPos, int wallHeightLevel, Vector2Int direction)
    {
        if (gridSystem == null) return;
        
        // 获取墙面格子的四个顶点
        Vector3[] corners = gridSystem.GetWallGridCellCorners(baseGridPos, wallHeightLevel, direction);
        
        // 使用和地面格子相同的填充样式
        Handles.DrawAAConvexPolygon(corners);
    }
    
    private void DrawWallGridCellBorder(GridVisualization gridSystem, Vector2Int baseGridPos, int wallHeightLevel, Vector2Int direction)
    {
        if (gridSystem == null) return;
        
        // 获取墙面格子的四个顶点
        Vector3[] corners = gridSystem.GetWallGridCellCorners(baseGridPos, wallHeightLevel, direction);
        
        // 使用和地面格子相同的边框样式
        for (int i = 0; i < corners.Length; i++)
        {
            int next = (i + 1) % corners.Length;
            Handles.DrawLine(corners[i], corners[next]);
        }
    }
    
    private void DrawWallGridCellBorder(GridVisualization gridSystem, Vector3 wallPos)
    {
        // 网格线模式下，边框和填充是一样的
        DrawWallGridCell(gridSystem, wallPos);
    }
    
    private void SnapToGrid(WallItem wallItem)
    {
        if (wallItem.GridSystem == null) return;
        
        Vector2Int gridPos = wallItem.GridSystem.WorldToGridIgnoreHeight(wallItem.transform.position);
        Vector3 worldPos = wallItem.GridSystem.GridToWorld(gridPos, wallItem.transform.position.z);
        
        Undo.RecordObject(wallItem.transform, "Snap Wall to Grid");
        wallItem.transform.position = worldPos;
        
        EditorUtility.SetDirty(wallItem);
        SceneView.RepaintAll();
    }
    
    private void ValidatePosition(WallItem wallItem)
    {
        if (wallItem.GridSystem == null)
        {
            EditorUtility.DisplayDialog("Validation", "No Grid System assigned!", "OK");
            return;
        }
        
        bool isValid = wallItem.IsValidPosition();
        string message = isValid ? "Wall position is valid." : "Wall position overlaps with other objects!";
        EditorUtility.DisplayDialog("Position Validation", message, "OK");
    }
}
