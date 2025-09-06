using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FurnitureItem))]
public class FurnitureItemEditor : Editor
{
    private FurnitureItem furniture;
    
    void OnEnable()
    {
        furniture = target as FurnitureItem;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // 显示当前占据的网格位置
        var positions = furniture.GetOccupiedGridPositions();
        EditorGUILayout.LabelField("Occupied Grid Positions (Ground):", EditorStyles.boldLabel);
        
        if (positions.Count > 0)
        {
            string positionText = "";
            foreach (var pos in positions)
            {
                positionText += $"({pos.x}, {pos.y}) ";
            }
            EditorGUILayout.LabelField(positionText, EditorStyles.wordWrappedLabel);
        }
        else
        {
            EditorGUILayout.LabelField("None", EditorStyles.centeredGreyMiniLabel);
        }
        
        // 显示表面提供的网格位置
        if (furniture.Occupancy.providesSurface)
        {
            var surfacePositions = furniture.GetSurfaceGridPositions();
            EditorGUILayout.LabelField("Surface Grid Positions:", EditorStyles.boldLabel);
            
            if (surfacePositions.Count > 0)
            {
                string surfaceText = "";
                foreach (var pos in surfacePositions)
                {
                    surfaceText += $"({pos.x}, {pos.y}) ";
                }
                EditorGUILayout.LabelField($"Level {furniture.GetSurfaceHeightLevel()}: {surfaceText}", EditorStyles.wordWrappedLabel);
            }
            else
            {
                EditorGUILayout.LabelField("None", EditorStyles.centeredGreyMiniLabel);
            }
        }
        
        EditorGUILayout.Space();
        
        // 显示位置有效性
        bool isValid = furniture.IsValidPosition();
        EditorGUILayout.LabelField("Position Valid:", isValid ? "Yes" : "No", 
            isValid ? EditorStyles.label : EditorStyles.boldLabel);
        
        if (!isValid)
        {
            EditorGUILayout.HelpBox("当前位置与其他家具重叠！", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Snap to Grid"))
        {
            SnapToGrid();
        }
        
        EditorGUILayout.HelpBox(
            "这个组件表示一个家具物品。可以设置它在网格中占据的大小和形状。" +
            "仅在编辑器模式下工作，运行时会自动禁用。" +
            "在Scene视图中会显示占据的格子。建议打开Grid Preview Window获得更好的预览体验。", 
            MessageType.Info);
            
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Open Grid Preview Window"))
        {
            EditorApplication.ExecuteMenuItem("Tools/Grid System/Grid Preview Window");
        }
    }
    
    private void SnapToGrid()
    {
        if (furniture.GridSystem == null)
        {
            Debug.LogWarning("No GridVisualization found in scene!");
            return;
        }
        
        Vector2Int gridPos = furniture.GridSystem.WorldToGrid(furniture.transform.position);
        Vector3 snappedPos = furniture.GridSystem.GridToWorld(gridPos);
        
        Undo.RecordObject(furniture.transform, "Snap to Grid");
        furniture.transform.position = snappedPos;
    }
    
    void OnSceneGUI()
    {
        // 现在统一由GridPreviewSystem处理所有绘制，避免重复绘制
        // 只保留标签显示
        if (furniture.GridSystem != null && 
            furniture.GridSystem.ShowGridSystem &&  // 检查主开关
            furniture.Occupancy.showInSceneView)    // 检查本地开关
        {
            Vector2Int gridPos = furniture.GridSystem.WorldToGrid(furniture.transform.position);
            // 在XY平面上，标签显示在物体上方
            Handles.Label(furniture.transform.position + Vector3.up * 1f, 
                $"Grid: ({gridPos.x}, {gridPos.y})");
                
            // 绘制已由GridPreviewSystem统一处理，不再在这里重复绘制
            // DrawFurnitureGrids();
        }
    }
    
    private void DrawFurnitureGrids()
    {
        var occupiedPositions = furniture.GetOccupiedGridPositions();
        bool isValid = furniture.IsValidPosition();
        
        // 绘制占用格子
        Color cellColor = isValid ? furniture.Occupancy.occupiedColor : Color.red;
        cellColor.a = furniture.GridSystem.Settings.occupiedAlpha;
        
        Handles.color = cellColor;
        
        foreach (var gridPos in occupiedPositions)
        {
            Vector3[] corners = GetGridCellCorners(gridPos);
            Handles.DrawAAConvexPolygon(corners);
        }
        
        // 绘制占用格子边框
        Handles.color = new Color(cellColor.r, cellColor.g, cellColor.b, 1f);
        foreach (var gridPos in occupiedPositions)
        {
            Vector3[] corners = GetGridCellCorners(gridPos);
            for (int i = 0; i < corners.Length; i++)
            {
                int next = (i + 1) % corners.Length;
                Handles.DrawLine(corners[i], corners[next]);
            }
        }
        
        // 绘制表面格子
        if (furniture.Occupancy.showSurfaceInSceneView && furniture.Occupancy.providesSurface)
        {
            var surfacePositions = furniture.GetSurfaceGridPositions();
            
            Color surfaceColor = furniture.Occupancy.surfaceColor;
            surfaceColor.a = furniture.GridSystem.Settings.occupiedAlpha;
            
            Handles.color = surfaceColor;
            
            // 计算表面高度
            float surfaceHeight = furniture.Occupancy.baseHeight + furniture.Occupancy.furnitureHeight;
            
            foreach (var gridPos in surfacePositions)
            {
                Vector3[] corners = GetSurfaceGridCellCorners(gridPos, surfaceHeight);
                Handles.DrawAAConvexPolygon(corners);
            }
            
            // 绘制表面格子边框
            Handles.color = new Color(surfaceColor.r, surfaceColor.g, surfaceColor.b, 1f);
            foreach (var gridPos in surfacePositions)
            {
                Vector3[] corners = GetSurfaceGridCellCorners(gridPos, surfaceHeight);
                for (int i = 0; i < corners.Length; i++)
                {
                    int next = (i + 1) % corners.Length;
                    Handles.DrawLine(corners[i], corners[next]);
                }
            }
        }
    }
    
    private Vector3[] GetGridCellCorners(Vector2Int gridPos)
    {
        Vector3 bottomLeft = furniture.GridSystem.GridToWorld(gridPos);
        Vector3 bottomRight = furniture.GridSystem.GridToWorld(gridPos + Vector2Int.right);
        Vector3 topRight = furniture.GridSystem.GridToWorld(gridPos + Vector2Int.one);
        Vector3 topLeft = furniture.GridSystem.GridToWorld(gridPos + Vector2Int.up);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    private Vector3[] GetSurfaceGridCellCorners(Vector2Int gridPos, float height)
    {
        Vector3 bottomLeft = furniture.GridSystem.GridToWorld(gridPos, height);
        Vector3 bottomRight = furniture.GridSystem.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = furniture.GridSystem.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = furniture.GridSystem.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
}
