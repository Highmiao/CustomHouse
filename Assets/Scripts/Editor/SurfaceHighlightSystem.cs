using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 表面高亮系统 - 鼠标悬停时高亮显示可放置的表面
/// </summary>
public static class SurfaceHighlightSystem
{
    private static Vector2Int? lastHighlightedGrid = null;
    private static SurfaceInfo? lastHighlightedSurface = null;
    
    public struct SurfaceInfo
    {
        public enum SurfaceType
        {
            Ground,        // 地面网格
            Floor,         // 地板表面
            FurnitureSurface, // 家具表面
            WallSurface    // 墙面表面
        }
        
        public SurfaceType type;
        public Vector2Int gridPosition;
        public float height;
        public string objectName;
        public MonoBehaviour sourceObject;
        
        public SurfaceInfo(SurfaceType type, Vector2Int gridPos, float height, string name, MonoBehaviour source)
        {
            this.type = type;
            this.gridPosition = gridPos;
            this.height = height;
            this.objectName = name;
            this.sourceObject = source;
        }
    }
    
    /// <summary>
    /// 在Scene视图中处理鼠标悬停高亮
    /// </summary>
    public static void HandleSceneViewHighlight(SceneView sceneView)
    {
        if (!GridPreviewSystem.EnableSurfaceHighlight) return;
        
        var gridSystem = Object.FindObjectOfType<GridVisualization>();
        if (gridSystem == null || !gridSystem.ShowGridSystem) return;
        
        // 获取鼠标在Scene视图中的位置
        Vector2 mousePosition = Event.current.mousePosition;
        Vector3 worldPosition = GetWorldPositionFromMouse(sceneView, mousePosition);
        
        if (worldPosition == Vector3.zero) return;
        
        // 转换为网格坐标
        Vector2Int gridPos = gridSystem.WorldToGridIgnoreHeight(worldPosition);
        
        // 检查是否有效的网格位置
        if (!gridSystem.IsValidGridPosition(gridPos)) return;
        
        // 查找当前位置的所有可用表面
        var surfaces = GetAvailableSurfaces(gridPos, gridSystem);
        
        if (surfaces.Count > 0)
        {
            // 选择最接近鼠标世界位置高度的表面
            var selectedSurface = SelectBestSurface(surfaces, worldPosition.y);
            
            // 如果表面发生变化，更新高亮
            if (!IsSameSurface(selectedSurface, lastHighlightedSurface))
            {
                lastHighlightedGrid = gridPos;
                lastHighlightedSurface = selectedSurface;
                
                // 显示信息
                ShowSurfaceInfo(selectedSurface);
            }
            
            // 绘制高亮
            DrawSurfaceHighlight(selectedSurface, gridSystem);
        }
        else
        {
            // 清除高亮
            if (lastHighlightedGrid.HasValue)
            {
                lastHighlightedGrid = null;
                lastHighlightedSurface = null;
            }
        }
        
        // 强制重绘Scene视图
        sceneView.Repaint();
    }
    
    private static Vector3 GetWorldPositionFromMouse(SceneView sceneView, Vector2 mousePosition)
    {
        // 翻转Y坐标（Screen坐标系统差异）
        mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y;
        
        // 从屏幕坐标创建射线
        Ray ray = sceneView.camera.ScreenPointToRay(mousePosition);
        
        // 创建一个水平面进行射线投射（假设主要在XZ平面工作）
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        return Vector3.zero;
    }
    
    private static List<SurfaceInfo> GetAvailableSurfaces(Vector2Int gridPos, GridVisualization gridSystem)
    {
        var surfaces = new List<SurfaceInfo>();
        
        // 1. 检查地面网格（基础层）
        if (gridSystem.IsValidGridPosition(gridPos))
        {
            surfaces.Add(new SurfaceInfo(
                SurfaceInfo.SurfaceType.Ground,
                gridPos,
                0f,
                "Ground Grid",
                null
            ));
        }
        
        // 2. 检查地板表面
        var floors = Object.FindObjectsOfType<FloorItem>();
        foreach (var floor in floors)
        {
            if (floor.FloorConfig.providesSurface && floor.FloorConfig.showSurfaceInSceneView)
            {
                var surfacePositions = floor.GetSurfaceGridPositions();
                if (surfacePositions.Contains(gridPos))
                {
                    float surfaceHeight = floor.FloorConfig.floorHeight + floor.FloorConfig.thickness;
                    surfaces.Add(new SurfaceInfo(
                        SurfaceInfo.SurfaceType.Floor,
                        gridPos,
                        surfaceHeight,
                        floor.name,
                        floor
                    ));
                }
            }
        }
        
        // 3. 检查家具表面
        var furniture = Object.FindObjectsOfType<FurnitureItem>();
        foreach (var item in furniture)
        {
            if (item.Occupancy.providesSurface && item.Occupancy.showSurfaceInSceneView)
            {
                var surfacePositions = item.GetSurfaceGridPositions();
                if (surfacePositions.Contains(gridPos))
                {
                    float surfaceHeight = item.Occupancy.baseHeight + item.Occupancy.furnitureHeight;
                    surfaces.Add(new SurfaceInfo(
                        SurfaceInfo.SurfaceType.FurnitureSurface,
                        gridPos,
                        surfaceHeight,
                        item.name,
                        item
                    ));
                }
            }
        }
        
        // 4. 检查墙面表面
        var walls = Object.FindObjectsOfType<WallItem>();
        foreach (var wall in walls)
        {
            if (wall.WallConfig.providesWallSurface && wall.WallConfig.showWallSurfaceInSceneView)
            {
                // 这里需要检查墙面网格，暂时简化处理
                // 可以根据需要扩展墙面表面的检测逻辑
            }
        }
        
        return surfaces;
    }
    
    private static SurfaceInfo SelectBestSurface(List<SurfaceInfo> surfaces, float mouseWorldY)
    {
        // 按高度排序，选择最接近鼠标位置的表面
        return surfaces.OrderBy(s => Mathf.Abs(s.height - mouseWorldY)).First();
    }
    
    private static bool IsSameSurface(SurfaceInfo? surface1, SurfaceInfo? surface2)
    {
        if (!surface1.HasValue && !surface2.HasValue) return true;
        if (!surface1.HasValue || !surface2.HasValue) return false;
        
        var s1 = surface1.Value;
        var s2 = surface2.Value;
        
        return s1.type == s2.type &&
               s1.gridPosition == s2.gridPosition &&
               Mathf.Approximately(s1.height, s2.height) &&
               s1.sourceObject == s2.sourceObject;
    }
    
    private static void ShowSurfaceInfo(SurfaceInfo? surface)
    {
        if (!surface.HasValue) return;
        
        var s = surface.Value;
        string typeText = GetSurfaceTypeText(s.type);
        
        if (s.sourceObject != null)
        {
            Debug.Log($"Surface: {typeText} - {s.objectName} at Grid({s.gridPosition.x}, {s.gridPosition.y}) Height: {s.height:F2}");
        }
        else
        {
            Debug.Log($"Surface: {typeText} at Grid({s.gridPosition.x}, {s.gridPosition.y}) Height: {s.height:F2}");
        }
    }
    
    private static string GetSurfaceTypeText(SurfaceInfo.SurfaceType type)
    {
        switch (type)
        {
            case SurfaceInfo.SurfaceType.Ground: return "地面网格";
            case SurfaceInfo.SurfaceType.Floor: return "地板表面";
            case SurfaceInfo.SurfaceType.FurnitureSurface: return "家具表面";
            case SurfaceInfo.SurfaceType.WallSurface: return "墙面表面";
            default: return "未知表面";
        }
    }
    
    private static void DrawSurfaceHighlight(SurfaceInfo? surface, GridVisualization gridSystem)
    {
        if (!surface.HasValue) return;
        
        var s = surface.Value;
        
        // 设置高亮颜色
        Color color = GridPreviewSystem.SurfaceHighlightColor;
        color.a = GridPreviewSystem.SurfaceHighlightAlpha;
        Handles.color = color;
        
        // 获取格子角点
        Vector3[] corners = GetSurfaceCorners(s, gridSystem);
        
        // 绘制高亮填充
        Handles.DrawAAConvexPolygon(corners);
        
        // 绘制高亮边框
        Handles.color = new Color(color.r, color.g, color.b, 1f);
        for (int i = 0; i < corners.Length; i++)
        {
            int next = (i + 1) % corners.Length;
            Handles.DrawLine(corners[i], corners[next]);
        }
        
        // 绘制表面信息标签
        Vector3 center = corners.Aggregate(Vector3.zero, (sum, corner) => sum + corner) / corners.Length;
        string label = $"{GetSurfaceTypeText(s.type)}\n{s.objectName}\nH: {s.height:F1}";
        
        // 设置标签样式
        var labelStyle = new GUIStyle(GUI.skin.box);
        labelStyle.normal.textColor = Color.white;
        labelStyle.normal.background = MakeTexture(1, 1, new Color(0, 0, 0, 0.7f));
        
        Handles.Label(center + Vector3.up * 0.5f, label, labelStyle);
    }
    
    private static Vector3[] GetSurfaceCorners(SurfaceInfo surface, GridVisualization gridSystem)
    {
        Vector2Int gridPos = surface.gridPosition;
        float height = surface.height;
        
        Vector3 bottomLeft = gridSystem.GridToWorld(gridPos, height);
        Vector3 bottomRight = gridSystem.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = gridSystem.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = gridSystem.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    private static Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 清除当前高亮
    /// </summary>
    public static void ClearHighlight()
    {
        lastHighlightedGrid = null;
        lastHighlightedSurface = null;
    }
}
