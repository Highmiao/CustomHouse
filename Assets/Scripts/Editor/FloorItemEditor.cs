using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FloorItem))]
public class FloorItemEditor : Editor
{
    private FloorItem floor;
    
    void OnEnable()
    {
        floor = target as FloorItem;
    }
    
    void OnSceneGUI()
    {
        // 绘制已由GridPreviewSystem统一处理，只保留标签显示
        if (floor.GridSystem != null && 
            floor.GridSystem.ShowGridSystem &&  // 检查主开关
            floor.FloorConfig.showInSceneView)    // 检查本地开关
        {
            Vector2Int gridPos = floor.GridSystem.WorldToGrid(floor.transform.position);
            // 在地面上方显示标签
            Handles.Label(floor.transform.position + Vector3.up * 1f, 
                $"Floor: ({gridPos.x}, {gridPos.y}) H:{floor.GetFloorHeightLevel()}");
                
            // 绘制已由GridPreviewSystem统一处理，不再在这里重复绘制
            // DrawFloorVisualization();
        }
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // 显示当前地面占据的网格位置
        var floorPositions = floor.GetFloorGridPositions();
        EditorGUILayout.LabelField("Floor Grid Positions:", EditorStyles.boldLabel);
        
        if (floorPositions.Count > 0)
        {
            string positionText = "";
            foreach (var pos in floorPositions)
            {
                positionText += $"({pos.x}, {pos.y}) ";
            }
            EditorGUILayout.LabelField($"Level {floor.GetFloorHeightLevel()}: {positionText}", EditorStyles.wordWrappedLabel);
        }
        else
        {
            EditorGUILayout.LabelField("None", EditorStyles.centeredGreyMiniLabel);
        }
        
        // 显示表面提供的网格位置
        if (floor.FloorConfig.providesSurface)
        {
            var surfacePositions = floor.GetSurfaceGridPositions();
            EditorGUILayout.LabelField("Surface Grid Positions:", EditorStyles.boldLabel);
            
            if (surfacePositions.Count > 0)
            {
                string surfaceText = "";
                foreach (var pos in surfacePositions)
                {
                    surfaceText += $"({pos.x}, {pos.y}) ";
                }
                EditorGUILayout.LabelField($"Level {floor.GetSurfaceHeightLevel()}: {surfaceText}", EditorStyles.wordWrappedLabel);
            }
            else
            {
                EditorGUILayout.LabelField("None", EditorStyles.centeredGreyMiniLabel);
            }
        }
        
        EditorGUILayout.Space();
        
        // 显示位置有效性
        bool isValid = floor.IsValidPosition();
        EditorGUILayout.LabelField("Position Valid:", isValid ? "Yes" : "No", 
            isValid ? EditorStyles.label : EditorStyles.boldLabel);
        
        if (!isValid)
        {
            EditorGUILayout.HelpBox("当前位置与其他地面重叠！", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Snap to Grid"))
        {
            SnapToGrid();
        }
        
        EditorGUILayout.HelpBox(
            "这个组件表示一个地面区域。可以设置它在网格中占据的大小和形状，以及提供的表面区域。" +
            "地面可以有不同的高度，用于创建多层结构。" +
            "仅在编辑器模式下工作，运行时会自动禁用。", 
            MessageType.Info);
            
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Open Grid Preview Window"))
        {
            EditorApplication.ExecuteMenuItem("Tools/Grid System/Grid Preview Window");
        }
    }
    
    private void SnapToGrid()
    {
        if (floor.GridSystem == null)
        {
            Debug.LogWarning("No GridVisualization found in scene!");
            return;
        }
        
        Vector2Int gridPos = floor.GridSystem.WorldToGrid(floor.transform.position);
        Vector3 snappedPos = floor.GridSystem.GridToWorld(gridPos, floor.FloorConfig.floorHeight);
        
        Undo.RecordObject(floor.transform, "Snap to Grid");
        floor.transform.position = snappedPos;
    }
    
    private void DrawFloorGrids()
    {
        var floorPositions = floor.GetFloorGridPositions();
        bool isValid = floor.IsValidPosition();
        
        // 绘制地面格子
        Color floorColor = isValid ? floor.FloorConfig.floorColor : Color.red;
        floorColor.a = floor.GridSystem.Settings.occupiedAlpha;
        
        Handles.color = floorColor;
        
        float floorHeight = floor.FloorConfig.floorHeight;
        
        foreach (var gridPos in floorPositions)
        {
            Vector3[] corners = GetFloorGridCellCorners(gridPos, floorHeight);
            Handles.DrawAAConvexPolygon(corners);
        }
        
        // 绘制地面格子边框
        Handles.color = new Color(floorColor.r, floorColor.g, floorColor.b, 1f);
        foreach (var gridPos in floorPositions)
        {
            Vector3[] corners = GetFloorGridCellCorners(gridPos, floorHeight);
            for (int i = 0; i < corners.Length; i++)
            {
                int next = (i + 1) % corners.Length;
                Handles.DrawLine(corners[i], corners[next]);
            }
        }
        
        // 绘制表面格子
        if (floor.FloorConfig.showSurfaceInSceneView && floor.FloorConfig.providesSurface)
        {
            var surfacePositions = floor.GetSurfaceGridPositions();
            
            Color surfaceColor = floor.FloorConfig.surfaceColor;
            surfaceColor.a = floor.GridSystem.Settings.occupiedAlpha;
            
            Handles.color = surfaceColor;
            
            // 计算表面高度
            float surfaceHeight = floor.FloorConfig.floorHeight + floor.FloorConfig.thickness;
            
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
    
    private Vector3[] GetFloorGridCellCorners(Vector2Int gridPos, float height)
    {
        Vector3 bottomLeft = floor.GridSystem.GridToWorld(gridPos, height);
        Vector3 bottomRight = floor.GridSystem.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = floor.GridSystem.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = floor.GridSystem.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    private Vector3[] GetSurfaceGridCellCorners(Vector2Int gridPos, float height)
    {
        Vector3 bottomLeft = floor.GridSystem.GridToWorld(gridPos, height);
        Vector3 bottomRight = floor.GridSystem.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = floor.GridSystem.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = floor.GridSystem.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
}
