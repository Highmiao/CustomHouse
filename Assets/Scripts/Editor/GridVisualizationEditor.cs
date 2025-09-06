using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(GridVisualization))]
public class GridVisualizationEditor : Editor
{
    private GridVisualization gridViz;
    
    void OnEnable()
    {
        gridViz = target as GridVisualization;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Refresh Grid Visualization"))
        {
            SceneView.RepaintAll();
        }
        
        EditorGUILayout.HelpBox(
            "这个组件用于可视化等距视角场景中的网格。" +
            "仅在编辑器模式下工作，运行时会自动禁用。" +
            "建议使用 Tools > Grid System > Grid Preview Window 获得更好的预览体验。", 
            MessageType.Info);
            
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Open Grid Preview Window"))
        {
            EditorApplication.ExecuteMenuItem("Tools/Grid System/Grid Preview Window");
        }
    }
    
    void OnSceneGUI()
    {
        if (gridViz == null) return;
        
        // 检查主开关
        if (!gridViz.ShowGridSystem)
            return;
        
        // 不在这里绘制，统一由GridPreviewSystem处理
        // DrawGrid();
        // DrawOccupiedCells();
        // DrawAllFurnitureGrids();
        // DrawAllWallGrids();
        // DrawAllFloorGrids();
        
        // 让Scene视图持续更新
        if (Event.current.type == EventType.Repaint)
        {
            SceneView.RepaintAll();
        }
    }
    
    private void DrawGrid()
    {
        var settings = gridViz.Settings;
        
        Handles.color = settings.gridColor;
        
        // 绘制网格线
        for (int x = 0; x <= settings.gridDimensions.x; x++)
        {
            for (int y = 0; y <= settings.gridDimensions.y; y++)
            {
                Vector3 pos = gridViz.GridToWorld(new Vector2Int(x, y));
                
                // 绘制水平线
                if (x < settings.gridDimensions.x)
                {
                    Vector3 endPos = gridViz.GridToWorld(new Vector2Int(x + 1, y));
                    Handles.DrawLine(pos, endPos);
                }
                
                // 绘制垂直线
                if (y < settings.gridDimensions.y)
                {
                    Vector3 endPos = gridViz.GridToWorld(new Vector2Int(x, y + 1));
                    Handles.DrawLine(pos, endPos);
                }
            }
        }
    }
    
    private void DrawOccupiedCells()
    {
        var furniture = FindObjectsOfType<FurnitureItem>();
        
        foreach (var item in furniture)
        {
            if (item.Occupancy.showInSceneView)
            {
                DrawFurnitureOccupancy(item);
            }
        }
    }
    
    private void DrawFurnitureOccupancy(FurnitureItem furniture)
    {
        var occupiedPositions = furniture.GetOccupiedGridPositions();
        bool isValid = furniture.IsValidPosition();
        
        // 绘制占用格子
        Color cellColor = isValid ? furniture.Occupancy.occupiedColor : Color.red;
        cellColor.a = gridViz.Settings.occupiedAlpha;
        
        Handles.color = cellColor;
        
        foreach (var gridPos in occupiedPositions)
        {
            DrawGridCell(gridPos);
        }
        
        // 绘制占用格子边框
        Handles.color = new Color(cellColor.r, cellColor.g, cellColor.b, 1f);
        foreach (var gridPos in occupiedPositions)
        {
            DrawGridCellBorder(gridPos);
        }
        
        // 绘制表面格子
        if (furniture.Occupancy.showSurfaceInSceneView && furniture.Occupancy.providesSurface)
        {
            var surfacePositions = furniture.GetSurfaceGridPositions();
            
            Color surfaceColor = furniture.Occupancy.surfaceColor;
            surfaceColor.a = gridViz.Settings.occupiedAlpha;
            
            Handles.color = surfaceColor;
            
            // 计算表面高度
            float surfaceHeight = furniture.Occupancy.baseHeight + furniture.Occupancy.furnitureHeight;
            
            foreach (var gridPos in surfacePositions)
            {
                DrawSurfaceGridCell(gridPos, surfaceHeight);
            }
            
            // 绘制表面格子边框
            Handles.color = new Color(surfaceColor.r, surfaceColor.g, surfaceColor.b, 1f);
            foreach (var gridPos in surfacePositions)
            {
                DrawSurfaceGridCellBorder(gridPos, surfaceHeight);
            }
        }
    }
    
    private void DrawGridCell(Vector2Int gridPos)
    {
        Vector3[] corners = GetGridCellCorners(gridPos);
        
        // 填充格子
        Handles.DrawAAConvexPolygon(corners);
    }
    
    private void DrawGridCellBorder(Vector2Int gridPos)
    {
        Vector3[] corners = GetGridCellCorners(gridPos);
        
        // 绘制边框
        for (int i = 0; i < corners.Length; i++)
        {
            int next = (i + 1) % corners.Length;
            Handles.DrawLine(corners[i], corners[next]);
        }
    }
    
    private void DrawSurfaceGridCell(Vector2Int gridPos, float height)
    {
        Vector3[] corners = GetSurfaceGridCellCorners(gridPos, height);
        
        // 填充格子
        Handles.DrawAAConvexPolygon(corners);
    }
    
    private void DrawSurfaceGridCellBorder(Vector2Int gridPos, float height)
    {
        Vector3[] corners = GetSurfaceGridCellCorners(gridPos, height);
        
        // 绘制边框
        for (int i = 0; i < corners.Length; i++)
        {
            int next = (i + 1) % corners.Length;
            Handles.DrawLine(corners[i], corners[next]);
        }
    }
    
    private Vector3[] GetGridCellCorners(Vector2Int gridPos)
    {
        Vector3 bottomLeft = gridViz.GridToWorld(gridPos);
        Vector3 bottomRight = gridViz.GridToWorld(gridPos + Vector2Int.right);
        Vector3 topRight = gridViz.GridToWorld(gridPos + Vector2Int.one);
        Vector3 topLeft = gridViz.GridToWorld(gridPos + Vector2Int.up);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    private Vector3[] GetSurfaceGridCellCorners(Vector2Int gridPos, float height)
    {
        Vector3 bottomLeft = gridViz.GridToWorld(gridPos, height);
        Vector3 bottomRight = gridViz.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = gridViz.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = gridViz.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    private void DrawAllFurnitureGrids()
    {
        var allFurniture = FindObjectsOfType<FurnitureItem>();
        
        foreach (var furniture in allFurniture)
        {
            if (furniture.GridSystem == gridViz && furniture.Occupancy.showInSceneView)
            {
                DrawFurnitureOccupancy(furniture);
            }
        }
    }
    
    private void DrawAllWallGrids()
    {
        var allWalls = FindObjectsOfType<WallItem>();
        
        foreach (var wall in allWalls)
        {
            if (wall.GridSystem == gridViz && wall.WallConfig.showInSceneView)
            {
                DrawWallOccupancy(wall);
            }
        }
    }
    
    private void DrawAllFloorGrids()
    {
        var allFloors = FindObjectsOfType<FloorItem>();
        
        foreach (var floor in allFloors)
        {
            if (floor.GridSystem == gridViz && floor.FloorConfig.showInSceneView)
            {
                DrawFloorOccupancy(floor);
            }
        }
    }
    
    private void DrawWallOccupancy(WallItem wall)
    {
        var basePositions = wall.GetBaseOccupiedGridPositions();
        bool isValid = wall.IsValidPosition();
        
        // 绘制地面占用
        Color baseColor = isValid ? wall.WallConfig.baseOccupiedColor : Color.red;
        baseColor.a = 0.3f;
        Handles.color = baseColor;
        
        foreach (var gridPos in basePositions)
        {
            Vector3[] corners = GetGridCellCorners(gridPos);
            Handles.DrawAAConvexPolygon(corners);
        }
        
        // 绘制地面边框
        Handles.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        foreach (var gridPos in basePositions)
        {
            Vector3[] corners = GetGridCellCorners(gridPos);
            for (int i = 0; i < corners.Length; i++)
            {
                int next = (i + 1) % corners.Length;
                Handles.DrawLine(corners[i], corners[next]);
            }
        }
        
        // 绘制墙面格子
        if (wall.WallConfig.showWallSurfaceInSceneView && wall.WallConfig.providesWallSurface)
        {
            Color wallColor = wall.WallConfig.wallSurfaceColor;
            wallColor.a = 0.3f;
            Handles.color = wallColor;
            
            var wallGridInfos = wall.GetWallGridCellInfos();
            foreach (var info in wallGridInfos)
            {
                Vector3[] corners = gridViz.GetWallGridCellCorners(info.basePos, info.heightLevel, info.direction);
                Handles.DrawAAConvexPolygon(corners);
            }
            
            // 绘制墙面边框
            Handles.color = new Color(wallColor.r, wallColor.g, wallColor.b, 1f);
            foreach (var info in wallGridInfos)
            {
                Vector3[] corners = gridViz.GetWallGridCellCorners(info.basePos, info.heightLevel, info.direction);
                for (int i = 0; i < corners.Length; i++)
                {
                    int next = (i + 1) % corners.Length;
                    Handles.DrawLine(corners[i], corners[next]);
                }
            }
        }
    }
    
    private void DrawFloorOccupancy(FloorItem floor)
    {
        var floorPositions = floor.GetFloorGridPositions();
        bool isValid = floor.IsValidPosition();
        
        // 绘制地面格子
        Color floorColor = isValid ? floor.FloorConfig.floorColor : Color.red;
        floorColor.a = gridViz.Settings.occupiedAlpha;
        
        Handles.color = floorColor;
        
        float floorHeight = floor.FloorConfig.floorHeight;
        
        foreach (var gridPos in floorPositions)
        {
            DrawFloorGridCell(gridPos, floorHeight);
        }
        
        // 绘制地面格子边框
        Handles.color = new Color(floorColor.r, floorColor.g, floorColor.b, 1f);
        foreach (var gridPos in floorPositions)
        {
            DrawFloorGridCellBorder(gridPos, floorHeight);
        }
        
        // 绘制表面格子
        if (floor.FloorConfig.showSurfaceInSceneView && floor.FloorConfig.providesSurface)
        {
            var surfacePositions = floor.GetSurfaceGridPositions();
            
            Color surfaceColor = floor.FloorConfig.surfaceColor;
            surfaceColor.a = gridViz.Settings.occupiedAlpha;
            
            Handles.color = surfaceColor;
            
            // 计算表面高度
            float surfaceHeight = floor.FloorConfig.floorHeight + floor.FloorConfig.thickness;
            
            foreach (var gridPos in surfacePositions)
            {
                DrawFloorSurfaceGridCell(gridPos, surfaceHeight);
            }
            
            // 绘制表面格子边框
            Handles.color = new Color(surfaceColor.r, surfaceColor.g, surfaceColor.b, 1f);
            foreach (var gridPos in surfacePositions)
            {
                DrawFloorSurfaceGridCellBorder(gridPos, surfaceHeight);
            }
        }
    }
    
    private void DrawFloorGridCell(Vector2Int gridPos, float height)
    {
        Vector3[] corners = GetFloorGridCellCorners(gridPos, height);
        Handles.DrawAAConvexPolygon(corners);
    }
    
    private void DrawFloorGridCellBorder(Vector2Int gridPos, float height)
    {
        Vector3[] corners = GetFloorGridCellCorners(gridPos, height);
        for (int i = 0; i < corners.Length; i++)
        {
            int next = (i + 1) % corners.Length;
            Handles.DrawLine(corners[i], corners[next]);
        }
    }
    
    private void DrawFloorSurfaceGridCell(Vector2Int gridPos, float height)
    {
        Vector3[] corners = GetFloorSurfaceGridCellCorners(gridPos, height);
        Handles.DrawAAConvexPolygon(corners);
    }
    
    private void DrawFloorSurfaceGridCellBorder(Vector2Int gridPos, float height)
    {
        Vector3[] corners = GetFloorSurfaceGridCellCorners(gridPos, height);
        for (int i = 0; i < corners.Length; i++)
        {
            int next = (i + 1) % corners.Length;
            Handles.DrawLine(corners[i], corners[next]);
        }
    }
    
    private Vector3[] GetFloorGridCellCorners(Vector2Int gridPos, float height)
    {
        Vector3 bottomLeft = gridViz.GridToWorld(gridPos, height);
        Vector3 bottomRight = gridViz.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = gridViz.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = gridViz.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    private Vector3[] GetFloorSurfaceGridCellCorners(Vector2Int gridPos, float height)
    {
        Vector3 bottomLeft = gridViz.GridToWorld(gridPos, height);
        Vector3 bottomRight = gridViz.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = gridViz.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = gridViz.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
}
