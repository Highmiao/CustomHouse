using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 网格材质管理器 - 统一管理和复用材质以提高性能
/// </summary>
public static class GridMaterialManager
{
    private static Material cachedLineMaterial;
    private static Material cachedFillMaterial;
    
    public static Material GetLineMaterial()
    {
        if (cachedLineMaterial == null)
        {
            cachedLineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            cachedLineMaterial.hideFlags = HideFlags.HideAndDontSave;
            cachedLineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            cachedLineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            cachedLineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            cachedLineMaterial.SetInt("_ZWrite", 0);
        }
        return cachedLineMaterial;
    }
    
    public static Material GetFillMaterial()
    {
        if (cachedFillMaterial == null)
        {
            cachedFillMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            cachedFillMaterial.hideFlags = HideFlags.HideAndDontSave;
            cachedFillMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            cachedFillMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            cachedFillMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            cachedFillMaterial.SetInt("_ZWrite", 0);
        }
        return cachedFillMaterial;
    }
    
    public static void CleanupMaterials()
    {
        if (cachedLineMaterial != null)
        {
            Object.DestroyImmediate(cachedLineMaterial);
            cachedLineMaterial = null;
        }
        if (cachedFillMaterial != null)
        {
            Object.DestroyImmediate(cachedFillMaterial);
            cachedFillMaterial = null;
        }
    }
}

/// <summary>
/// 批处理绘制数据结构
/// </summary>
public struct BatchDrawData
{
    public List<Vector3> vertices;
    public List<int> indices;
    public Color materialColor;
    public bool isFill; // true=填充, false=线框
    
    public BatchDrawData(Color color, bool fill)
    {
        vertices = new List<Vector3>();
        indices = new List<int>();
        materialColor = color;
        isFill = fill;
    }
}

/// <summary>
/// 编辑器专用的网格预览系统
/// 只在编辑器模式下工作，提供实时的网格可视化
/// </summary>
public class GridPreviewSystem : EditorWindow
{
    private static GridPreviewSystem instance;
    private static GridVisualization currentGridSystem;
    
    // 静态属性，允许其他脚本访问
    public static GridVisualization CurrentGridSystem => currentGridSystem;
    
    // 表面高亮控制
    private static bool enableSurfaceHighlight = true;
    private static Color surfaceHighlightColor = Color.yellow;
    private static float surfaceHighlightAlpha = 0.3f;
    
    // 性能优化变量
    private static double lastUpdateTime = 0;
    private const double UPDATE_INTERVAL = 0.05; // 20fps限制，减少重绘频率
    private static bool needsRepaint = false;
    
    // 缓存组件引用以减少查找次数
    private static FurnitureItem[] cachedFurniture = null;
    private static MonoBehaviour[] cachedWalls = null;
    private static MonoBehaviour[] cachedFloors = null;
    private static double lastComponentCacheTime = 0;
    private const double CACHE_REFRESH_INTERVAL = 1.0; // 每秒刷新一次组件缓存
    
    // 公共属性访问器
    public static bool EnableSurfaceHighlight => enableSurfaceHighlight;
    public static Color SurfaceHighlightColor => surfaceHighlightColor;
    public static float SurfaceHighlightAlpha => surfaceHighlightAlpha;
    
    [MenuItem("Tools/Grid System/Grid Preview Window")]
    public static void ShowWindow()
    {
        instance = GetWindow<GridPreviewSystem>();
        instance.titleContent = new GUIContent("Grid Preview");
        instance.Show();
        
        // 确保当前网格系统是最新的
        if (currentGridSystem == null)
        {
            currentGridSystem = FindObjectOfType<GridVisualization>();
        }
        
        // 启用Scene视图的持续重绘
        SceneView.duringSceneGui -= OnSceneGUIStatic;
        SceneView.duringSceneGui += OnSceneGUIStatic;
    }
    
    void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUIStatic;
        SceneView.duringSceneGui += OnSceneGUIStatic;
    }
    
    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUIStatic;
    }
    
    // 添加菜单项来快速切换网格显示
    [MenuItem("Tools/Grid System/Toggle Grid Preview")]
    public static void ToggleGridPreview()
    {
        if (currentGridSystem == null)
        {
            currentGridSystem = FindObjectOfType<GridVisualization>();
        }
        
        if (currentGridSystem != null)
        {
            SerializedObject serializedGrid = new SerializedObject(currentGridSystem);
            SerializedProperty showGridSystemProp = serializedGrid.FindProperty("showGridSystem");
            showGridSystemProp.boolValue = !showGridSystemProp.boolValue;
            serializedGrid.ApplyModifiedProperties();
            
            SceneView.RepaintAll();
            
            Debug.Log($"Grid System {(showGridSystemProp.boolValue ? "Enabled" : "Disabled")}");
        }
        else
        {
            Debug.LogWarning("No GridVisualization found in scene. Please add one first.");
        }
        
        // 确保回调已注册
        SceneView.duringSceneGui -= OnSceneGUIStatic;
        SceneView.duringSceneGui += OnSceneGUIStatic;
    }
    
    // 验证菜单项状态
    [MenuItem("Tools/Grid System/Toggle Grid Preview", true)]
    public static bool ToggleGridPreviewValidate()
    {
        if (currentGridSystem == null)
        {
            currentGridSystem = FindObjectOfType<GridVisualization>();
        }
        
        bool isEnabled = currentGridSystem != null && currentGridSystem.ShowGridSystem;
        Menu.SetChecked("Tools/Grid System/Toggle Grid Preview", isEnabled);
        return currentGridSystem != null;
    }
    
    // 静态的Scene GUI回调，这样即使窗口关闭也能保持显示
    private static void OnSceneGUIStatic(SceneView sceneView)
    {
        if (currentGridSystem == null)
            return;
            
        // 检查GridVisualization的主开关
        if (!currentGridSystem.ShowGridSystem)
        {
            // 清除高亮
            ClearSurfaceHighlight();
            return;
        }
        
        // 使用批处理绘制
        DrawBatchedPreview(); // 恢复批处理绘制
        
        // 处理表面高亮
        HandleSurfaceHighlight(sceneView); // 恢复表面高亮
        
        // 强制Scene视图重绘以保持实时更新（限制频率）
        double currentTime = EditorApplication.timeSinceStartup;
        if (Event.current.type == EventType.MouseMove)
        {
            if (currentTime - lastUpdateTime > UPDATE_INTERVAL)
            {
                lastUpdateTime = currentTime;
                needsRepaint = true;
                sceneView.Repaint();
            }
        }
        else if (Event.current.type == EventType.Repaint && needsRepaint)
        {
        }
    }
    
    /// <summary>
    /// 新的批处理绘制方法
    /// </summary>
    private static void DrawBatchedPreview()
    {
        if (Event.current.type != EventType.Repaint) return;
        
        // 创建批处理容器
        var batches = new Dictionary<string, BatchDrawData>();
        
        // 收集所有需要绘制的几何体
        CollectGridGeometry(batches);
        CollectFurnitureGeometry(batches);
        CollectWallGeometry(batches);
        CollectFloorGeometry(batches);
        
        // 批量绘制
        foreach (var kvp in batches)
        {
            if (kvp.Value.vertices.Count > 0)
            {
                DrawBatch(kvp.Value);
            }
        }
    }
    
    /// <summary>
    /// 收集网格几何体
    /// </summary>
    private static void CollectGridGeometry(Dictionary<string, BatchDrawData> batches)
    {
        if (currentGridSystem == null) return;
        
        var settings = currentGridSystem.Settings;
        
        // 获取或创建网格线批次
        string gridKey = $"Grid_{ColorToString(settings.gridColor)}";
        if (!batches.ContainsKey(gridKey))
        {
            batches[gridKey] = new BatchDrawData(settings.gridColor, false);
        }
        var gridBatch = batches[gridKey];
        
        // 添加网格线几何体到批次
        if (settings.showAllLevels)
        {
            for (int level = 0; level <= settings.maxLevels; level++)
            {
                AddGridLevelToBatch(gridBatch, level);
            }
        }
        else
        {
            AddGridLevelToBatch(gridBatch, 0);
        }
        
        batches[gridKey] = gridBatch;
    }
    
    /// <summary>
    /// 添加网格层级到批次
    /// </summary>
    private static void AddGridLevelToBatch(BatchDrawData batch, int level)
    {
        if (currentGridSystem == null) return;
        
        var settings = currentGridSystem.Settings;
        float height = currentGridSystem.GetHeightForLevel(level);
        
        // 添加网格线顶点
        for (int x = 0; x <= settings.gridDimensions.x; x++)
        {
            for (int y = 0; y <= settings.gridDimensions.y; y++)
            {
                Vector3 pos = currentGridSystem.GridToWorld(new Vector2Int(x, y), height);
                
                // 水平线
                if (x < settings.gridDimensions.x)
                {
                    Vector3 endPos = currentGridSystem.GridToWorld(new Vector2Int(x + 1, y), height);
                    AddLineToBatch(batch, pos, endPos);
                }
                
                // 垂直线
                if (y < settings.gridDimensions.y)
                {
                    Vector3 endPos = currentGridSystem.GridToWorld(new Vector2Int(x, y + 1), height);
                    AddLineToBatch(batch, pos, endPos);
                }
            }
        }
    }
    
    /// <summary>
    /// 收集家具几何体
    /// </summary>
    private static void CollectFurnitureGeometry(Dictionary<string, BatchDrawData> batches)
    {
        RefreshComponentCache();
        
        if (cachedFurniture == null) return;
        
        foreach (var item in cachedFurniture)
        {
            if (!item.Occupancy.showInSceneView) continue;
            
            // 占用格子
            AddFurnitureOccupancyToBatch(batches, item);
            
            // 表面格子
            if (item.Occupancy.showSurfaceInSceneView && item.Occupancy.providesSurface)
            {
                AddFurnitureSurfaceToBatch(batches, item);
            }
        }
    }
    
    /// <summary>
    /// 添加家具占用到批次
    /// </summary>
    private static void AddFurnitureOccupancyToBatch(Dictionary<string, BatchDrawData> batches, FurnitureItem furniture)
    {
        bool isValid = furniture.IsValidPosition();
        Color color = isValid ? furniture.Occupancy.occupiedColor : Color.red;
        color.a = currentGridSystem.Settings.occupiedAlpha;
        
        // 填充批次
        string fillKey = $"FurnitureFill_{ColorToString(color)}";
        if (!batches.ContainsKey(fillKey))
        {
            batches[fillKey] = new BatchDrawData(color, true);
        }
        
        // 边框批次
        Color borderColor = new Color(color.r, color.g, color.b, 1f);
        string borderKey = $"FurnitureBorder_{ColorToString(borderColor)}";
        if (!batches.ContainsKey(borderKey))
        {
            batches[borderKey] = new BatchDrawData(borderColor, false);
        }
        
        var fillBatch = batches[fillKey];
        var borderBatch = batches[borderKey];
        
        var positions = furniture.GetOccupiedGridPositions();
        float height = currentGridSystem.GetHeightForLevel(0);
        
        foreach (var gridPos in positions)
        {
            Vector3[] corners = GetGridCellCornersAtHeight(gridPos, height);
            AddQuadToBatch(fillBatch, corners);
            AddQuadBorderToBatch(borderBatch, corners);
        }
        
        batches[fillKey] = fillBatch;
        batches[borderKey] = borderBatch;
    }
    
    /// <summary>
    /// 添加家具表面到批次
    /// </summary>
    private static void AddFurnitureSurfaceToBatch(Dictionary<string, BatchDrawData> batches, FurnitureItem furniture)
    {
        Color color = furniture.Occupancy.surfaceColor;
        color.a = currentGridSystem.Settings.surfaceAlpha;
        
        // 填充批次
        string fillKey = $"SurfaceFill_{ColorToString(color)}";
        if (!batches.ContainsKey(fillKey))
        {
            batches[fillKey] = new BatchDrawData(color, true);
        }
        
        // 边框批次
        Color borderColor = new Color(color.r, color.g, color.b, 1f);
        string borderKey = $"SurfaceBorder_{ColorToString(borderColor)}";
        if (!batches.ContainsKey(borderKey))
        {
            batches[borderKey] = new BatchDrawData(borderColor, false);
        }
        
        var fillBatch = batches[fillKey];
        var borderBatch = batches[borderKey];
        
        var positions = furniture.GetSurfaceGridPositions();
        float height = furniture.Occupancy.baseHeight + furniture.Occupancy.furnitureHeight;
        
        foreach (var gridPos in positions)
        {
            Vector3[] corners = GetGridCellCornersAtHeight(gridPos, height);
            AddQuadToBatch(fillBatch, corners);
            AddQuadBorderToBatch(borderBatch, corners);
        }
        
        batches[fillKey] = fillBatch;
        batches[borderKey] = borderBatch;
    }
    
    /// <summary>
    /// 收集墙体几何体
    /// </summary>
    private static void CollectWallGeometry(Dictionary<string, BatchDrawData> batches)
    {
        RefreshComponentCache();
        
        if (cachedWalls == null) return;
        
        foreach (var item in cachedWalls)
        {
            var wallItem = item as WallItem;
            if (wallItem == null || !wallItem.WallConfig.showInSceneView) continue;
            
            // 墙体格子
            AddWallOccupancyToBatch(batches, wallItem);
            
            // 墙面格子
            if (wallItem.WallConfig.showWallSurfaceInSceneView && wallItem.WallConfig.providesWallSurface)
            {
                AddWallSurfaceToBatch(batches, wallItem);
            }
        }
    }
    
    /// <summary>
    /// 添加墙体占用到批次
    /// </summary>
    private static void AddWallOccupancyToBatch(Dictionary<string, BatchDrawData> batches, WallItem wall)
    {
        bool isValid = wall.IsValidPosition();
        Color color = isValid ? wall.WallConfig.baseOccupiedColor : Color.red;
        color.a = currentGridSystem.Settings.occupiedAlpha;
        
        // 填充批次
        string fillKey = $"WallFill_{ColorToString(color)}";
        if (!batches.ContainsKey(fillKey))
        {
            batches[fillKey] = new BatchDrawData(color, true);
        }
        
        // 边框批次
        Color borderColor = new Color(color.r, color.g, color.b, 1f);
        string borderKey = $"WallBorder_{ColorToString(borderColor)}";
        if (!batches.ContainsKey(borderKey))
        {
            batches[borderKey] = new BatchDrawData(borderColor, false);
        }
        
        var fillBatch = batches[fillKey];
        var borderBatch = batches[borderKey];
        
        var positions = wall.GetBaseOccupiedGridPositions();
        float height = 0f; // 墙体基座在地面高度
        
        foreach (var gridPos in positions)
        {
            Vector3[] corners = GetGridCellCornersAtHeight(gridPos, height);
            AddQuadToBatch(fillBatch, corners);
            AddQuadBorderToBatch(borderBatch, corners);
        }
        
        batches[fillKey] = fillBatch;
        batches[borderKey] = borderBatch;
    }
    
    /// <summary>
    /// 添加墙面到批次
    /// </summary>
    private static void AddWallSurfaceToBatch(Dictionary<string, BatchDrawData> batches, WallItem wall)
    {
        Color color = wall.WallConfig.wallSurfaceColor;
        color.a = currentGridSystem.Settings.surfaceAlpha;
        
        // 填充批次
        string fillKey = $"SurfaceFill_{ColorToString(color)}";
        if (!batches.ContainsKey(fillKey))
        {
            batches[fillKey] = new BatchDrawData(color, true);
        }
        
        // 边框批次
        Color borderColor = new Color(color.r, color.g, color.b, 1f);
        string borderKey = $"SurfaceBorder_{ColorToString(borderColor)}";
        if (!batches.ContainsKey(borderKey))
        {
            batches[borderKey] = new BatchDrawData(borderColor, false);
        }
        
        var fillBatch = batches[fillKey];
        var borderBatch = batches[borderKey];
        
        // 暂时使用简化的墙面位置计算
        // 直接获取墙体的占用位置，然后使用法向量偏移
        var wallOccupiedPositions = wall.GetBaseOccupiedGridPositions();
        
        // 计算法向量（垂直于墙面方向）
        // 对于墙面，我们需要确定墙面朝向哪一侧
        Vector2Int normalVector;
        switch (wall.WallConfig.direction)
        {
            case WallDirection.North: normalVector = Vector2Int.right; break;  // North墙的墙面在西侧
            case WallDirection.East: normalVector = Vector2Int.up; break;   // East墙的墙面在南侧
            case WallDirection.South: normalVector = Vector2Int.left; break; // South墙的墙面在东侧
            case WallDirection.West: normalVector = Vector2Int.down; break;     // West墙的墙面在北侧
            default: normalVector = Vector2Int.right; break;
        }
        
        foreach (var baseGridPos in wallOccupiedPositions)
        {
            for (int h = 0; h < wall.WallConfig.wallHeight; h++)
            {
                // 墙面应该在墙体旁边，使用法向量偏移
                Vector2Int surfaceGridPos = baseGridPos + normalVector;
                
                // 计算墙面高度
                float surfaceHeight = h * currentGridSystem.Settings.heightPerLevel;
                
                // 墙面应该绘制为垂直的平行四边形，不是水平的菱形
                Vector3[] wallCorners = GetWallSurfaceCornersAtHeight(surfaceGridPos, surfaceHeight);
                AddQuadToBatch(fillBatch, wallCorners);
                AddQuadBorderToBatch(borderBatch, wallCorners);
            }
        }
        
        batches[fillKey] = fillBatch;
        batches[borderKey] = borderBatch;
    }
    
    /// <summary>
    /// 收集地面几何体
    /// </summary>
    private static void CollectFloorGeometry(Dictionary<string, BatchDrawData> batches)
    {
        RefreshComponentCache();
        
        if (cachedFloors == null) return;
        
        foreach (var item in cachedFloors)
        {
            var floorItem = item as FloorItem;
            if (floorItem == null || !floorItem.FloorConfig.showInSceneView) continue;
            
            // 地面格子
            AddFloorOccupancyToBatch(batches, floorItem);
            
            // 地面表面格子
            if (floorItem.FloorConfig.showSurfaceInSceneView && floorItem.FloorConfig.providesSurface)
            {
                AddFloorSurfaceToBatch(batches, floorItem);
            }
        }
    }
    
    /// <summary>
    /// 添加地面占用到批次
    /// </summary>
    private static void AddFloorOccupancyToBatch(Dictionary<string, BatchDrawData> batches, FloorItem floor)
    {
        bool isValid = floor.IsValidPosition();
        Color floorColor = floor.FloorConfig.floorColor;
        
        if (!isValid) floorColor = Color.red;
        floorColor.a = currentGridSystem.Settings.occupiedAlpha;
        
        // 填充批次
        string fillKey = $"FloorFill_{ColorToString(floorColor)}";
        if (!batches.ContainsKey(fillKey))
        {
            batches[fillKey] = new BatchDrawData(floorColor, true);
        }
        
        // 边框批次
        Color borderColor = new Color(floorColor.r, floorColor.g, floorColor.b, 1f);
        string borderKey = $"FloorBorder_{ColorToString(borderColor)}";
        if (!batches.ContainsKey(borderKey))
        {
            batches[borderKey] = new BatchDrawData(borderColor, false);
        }
        
        var fillBatch = batches[fillKey];
        var borderBatch = batches[borderKey];
        
        // 获取地面位置
        var floorPositions = floor.GetFloorGridPositions();
        float floorHeight = floor.FloorConfig.floorHeight;
        
        foreach (var gridPos in floorPositions)
        {
            Vector3[] corners = GetGridCellCornersAtHeight(gridPos, floorHeight);
            AddQuadToBatch(fillBatch, corners);
            AddQuadBorderToBatch(borderBatch, corners);
        }
        
        batches[fillKey] = fillBatch;
        batches[borderKey] = borderBatch;
    }
    
    /// <summary>
    /// 添加地面表面到批次
    /// </summary>
    private static void AddFloorSurfaceToBatch(Dictionary<string, BatchDrawData> batches, FloorItem floor)
    {
        Color surfaceColor = floor.FloorConfig.surfaceColor;
        surfaceColor.a = currentGridSystem.Settings.surfaceAlpha;
        
        // 填充批次
        string fillKey = $"SurfaceFill_{ColorToString(surfaceColor)}";
        if (!batches.ContainsKey(fillKey))
        {
            batches[fillKey] = new BatchDrawData(surfaceColor, true);
        }
        
        // 边框批次
        Color borderColor = new Color(surfaceColor.r, surfaceColor.g, surfaceColor.b, 1f);
        string borderKey = $"SurfaceBorder_{ColorToString(borderColor)}";
        if (!batches.ContainsKey(borderKey))
        {
            batches[borderKey] = new BatchDrawData(borderColor, false);
        }
        
        var fillBatch = batches[fillKey];
        var borderBatch = batches[borderKey];
        
        // 获取地面表面位置
        var surfacePositions = floor.GetSurfaceGridPositions();
        float surfaceHeight = floor.FloorConfig.floorHeight + floor.FloorConfig.thickness;
        
        foreach (var gridPos in surfacePositions)
        {
            Vector3[] corners = GetGridCellCornersAtHeight(gridPos, surfaceHeight);
            AddQuadToBatch(fillBatch, corners);
            AddQuadBorderToBatch(borderBatch, corners);
        }
        
        batches[fillKey] = fillBatch;
        batches[borderKey] = borderBatch;
    }
    
    // 静态版本的网格绘制方法
    private static void DrawGridPreviewStatic()
    {
        if (currentGridSystem == null) return;
        
        var settings = currentGridSystem.Settings;
        
        // 绘制不同高度层的网格
        if (settings.showAllLevels)
        {
            for (int level = 0; level <= settings.maxLevels; level++)
            {
                DrawGridAtLevel(level);
            }
        }
        else
        {
            DrawGridAtLevel(0); // 只绘制地面层
        }
    }
    
    // 在指定高度层绘制网格
    private static void DrawGridAtLevel(int level)
    {
        if (currentGridSystem == null) return;
        
        var settings = currentGridSystem.Settings;
        float height = currentGridSystem.GetHeightForLevel(level);
        
        // 高层网格使用较淡的颜色
        Color gridColor = settings.gridColor;
        if (level > 0)
        {
            gridColor.a = 0.3f - (level * 0.05f); // 每层递减透明度
            gridColor.a = Mathf.Max(0.1f, gridColor.a);
        }
        
        Handles.color = gridColor;
        
        // 绘制网格线
        for (int x = 0; x <= settings.gridDimensions.x; x++)
        {
            for (int y = 0; y <= settings.gridDimensions.y; y++)
            {
                Vector3 pos = currentGridSystem.GridToWorld(new Vector2Int(x, y), height);
                
                // 绘制水平线
                if (x < settings.gridDimensions.x)
                {
                    Vector3 endPos = currentGridSystem.GridToWorld(new Vector2Int(x + 1, y), height);
                    Handles.DrawLine(pos, endPos);
                }
                
                // 绘制垂直线
                if (y < settings.gridDimensions.y)
                {
                    Vector3 endPos = currentGridSystem.GridToWorld(new Vector2Int(x, y + 1), height);
                    Handles.DrawLine(pos, endPos);
                }
            }
        }
    }
    
    // 静态版本的家具绘制方法
    private static void DrawFurniturePreviewStatic()
    {
        var furniture = FindObjectsOfType<FurnitureItem>();
        
        foreach (var item in furniture)
        {
            if (item.Occupancy.showInSceneView)
            {
                DrawFurnitureOccupancyStatic(item);
            }
        }
    }
    
    // 静态版本的墙面绘制方法
    private static void DrawWallPreviewStatic()
    {
        // 使用反射查找WallItem组件
        var wallComponents = Object.FindObjectsOfType<MonoBehaviour>()
            .Where(mb => mb.GetType().Name == "WallItem").ToArray();
        
        foreach (var wall in wallComponents)
        {
            // 通过反射获取WallConfig
            var wallConfigField = wall.GetType().GetField("wallConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wallConfigField != null)
            {
                var wallConfig = wallConfigField.GetValue(wall);
                var showInSceneViewField = wallConfig.GetType().GetField("showInSceneView");
                if (showInSceneViewField != null && (bool)showInSceneViewField.GetValue(wallConfig))
                {
                    DrawWallOccupancyStatic(wall);
                }
            }
        }
    }
    
    // 静态版本的墙面占用绘制方法
    private static void DrawWallOccupancyStatic(MonoBehaviour wallComponent)
    {
        if (currentGridSystem == null) return;
        
        try
        {
            // 通过反射获取墙面信息
            var wallType = wallComponent.GetType();
            var wallConfigField = wallType.GetField("wallConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wallConfigField == null) return;
            
            var wallConfig = wallConfigField.GetValue(wallComponent);
            var wallConfigType = wallConfig.GetType();
            
            // 获取基础占用位置
            var getBasePositionsMethod = wallType.GetMethod("GetBaseOccupiedGridPositions");
            if (getBasePositionsMethod == null) return;
            
            var basePositions = (List<Vector2Int>)getBasePositionsMethod.Invoke(wallComponent, null);
            
            // 获取是否有效位置
            var isValidMethod = wallType.GetMethod("IsValidPosition");
            bool isValid = isValidMethod != null ? (bool)isValidMethod.Invoke(wallComponent, null) : true;
            
            // 获取颜色配置
            var baseColorField = wallConfigType.GetField("baseOccupiedColor");
            Color baseColor = baseColorField != null ? (Color)baseColorField.GetValue(wallConfig) : Color.red;
            
            var showWallSurfaceField = wallConfigType.GetField("showWallSurfaceInSceneView");
            bool showWallSurface = showWallSurfaceField != null ? (bool)showWallSurfaceField.GetValue(wallConfig) : false;
            
            var providesWallSurfaceField = wallConfigType.GetField("providesWallSurface");
            bool providesWallSurface = providesWallSurfaceField != null ? (bool)providesWallSurfaceField.GetValue(wallConfig) : false;
            
            var directionField = wallConfigType.GetField("direction");
            var direction = directionField != null ? directionField.GetValue(wallConfig).ToString() : "Unknown";
            
            // 绘制地面占用区域
            float height = 0f; // 地面高度
            
            Color cellColor = isValid ? baseColor : Color.red;
            cellColor.a = currentGridSystem.Settings.occupiedAlpha;
            
            Handles.color = cellColor;
            
            foreach (var gridPos in basePositions)
            {
                DrawGridCellAtHeight(gridPos, height);
            }
            
            // 绘制边框
            Handles.color = new Color(cellColor.r, cellColor.g, cellColor.b, 1f);
            foreach (var gridPos in basePositions)
            {
                DrawGridCellBorderAtHeight(gridPos, height);
            }
            
            // 显示标签
            if (basePositions.Count > 0)
            {
                Vector3 labelPos = currentGridSystem.GridToWorld(basePositions[0], height) + Vector3.up * 0.5f;
                string displayText = $"墙面基础\\n({basePositions[0].x}, {basePositions[0].y})\\n方向:{direction}";
                Handles.Label(labelPos, displayText);
            }
            
            // 绘制墙面格子
            if (showWallSurface && providesWallSurface)
            {
                // 尝试获取新的墙面格子信息方法
                var getWallGridCellInfosMethod = wallType.GetMethod("GetWallGridCellInfos");
                if (getWallGridCellInfosMethod != null)
                {
                    var wallGridInfos = getWallGridCellInfosMethod.Invoke(wallComponent, null);
                    var wallSurfaceColorField = wallConfigType.GetField("wallSurfaceColor");
                    Color wallColor = wallSurfaceColorField != null ? 
                        (Color)wallSurfaceColorField.GetValue(wallConfig) : Color.cyan;
                    wallColor.a = currentGridSystem.Settings.wallAlpha;
                    
                    Handles.color = wallColor;
                    
                    // 使用反射获取列表内容
                    if (wallGridInfos != null)
                    {
                        var listType = wallGridInfos.GetType();
                        var countProperty = listType.GetProperty("Count");
                        var itemProperty = listType.GetProperty("Item");
                        
                        if (countProperty != null && itemProperty != null)
                        {
                            int count = (int)countProperty.GetValue(wallGridInfos);
                            
                            // 先绘制填充
                            for (int i = 0; i < count; i++)
                            {
                                var item = itemProperty.GetValue(wallGridInfos, new object[] { i });
                                
                                // 解析元组 (Vector2Int basePos, int heightLevel, Vector2Int direction)
                                var itemType = item.GetType();
                                var basePosField = itemType.GetField("basePos");
                                var heightLevelField = itemType.GetField("heightLevel");
                                var wallDirectionField = itemType.GetField("direction");
                                
                                if (basePosField != null && heightLevelField != null && wallDirectionField != null)
                                {
                                    Vector2Int basePos = (Vector2Int)basePosField.GetValue(item);
                                    int heightLevel = (int)heightLevelField.GetValue(item);
                                    Vector2Int wallDirection = (Vector2Int)wallDirectionField.GetValue(item);
                                    
                                    DrawWallGridCellCorners(basePos, heightLevel, wallDirection);
                                }
                            }
                            
                            // 再绘制边框
                            Handles.color = new Color(wallColor.r, wallColor.g, wallColor.b, 1f);
                            for (int i = 0; i < count; i++)
                            {
                                var item = itemProperty.GetValue(wallGridInfos, new object[] { i });
                                
                                // 解析元组 (Vector2Int basePos, int heightLevel, Vector2Int direction)
                                var itemType = item.GetType();
                                var basePosField = itemType.GetField("basePos");
                                var heightLevelField = itemType.GetField("heightLevel");
                                var wallDirectionField = itemType.GetField("direction");
                                
                                if (basePosField != null && heightLevelField != null && wallDirectionField != null)
                                {
                                    Vector2Int basePos = (Vector2Int)basePosField.GetValue(item);
                                    int heightLevel = (int)heightLevelField.GetValue(item);
                                    Vector2Int wallDirection = (Vector2Int)wallDirectionField.GetValue(item);
                                    
                                    DrawWallGridCellBorder(basePos, heightLevel, wallDirection);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // 回退到旧方法
                    var getWallPositionsMethod = wallType.GetMethod("GetWallSurfacePositions");
                    if (getWallPositionsMethod != null)
                    {
                        var wallPositions = (List<Vector3>)getWallPositionsMethod.Invoke(wallComponent, null);
                        
                        var wallSurfaceColorField = wallConfigType.GetField("wallSurfaceColor");
                        Color wallColor = wallSurfaceColorField != null ? 
                            (Color)wallSurfaceColorField.GetValue(wallConfig) : Color.cyan;
                        wallColor.a = currentGridSystem.Settings.wallAlpha;
                        
                        Handles.color = wallColor;
                        
                        foreach (var wallPos in wallPositions)
                        {
                            DrawWallGridCell(wallPos);
                        }
                        
                        // 绘制墙面边框
                        Handles.color = new Color(wallColor.r, wallColor.g, wallColor.b, 1f);
                        foreach (var wallPos in wallPositions)
                        {
                            DrawWallGridCellBorder(wallPos);
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Error drawing wall occupancy: {ex.Message}");
        }
    }
    
    // 静态版本的地面预览绘制方法
    private static void DrawFloorPreviewStatic()
    {
        // 使用反射查找FloorItem组件
        var floorComponents = Object.FindObjectsOfType<MonoBehaviour>()
            .Where(mb => mb.GetType().Name == "FloorItem").ToArray();
        
        foreach (var floor in floorComponents)
        {
            // 通过反射获取FloorConfig
            var floorConfigField = floor.GetType().GetField("floorConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (floorConfigField != null)
            {
                var floorConfig = floorConfigField.GetValue(floor);
                var showInSceneViewField = floorConfig.GetType().GetField("showInSceneView");
                if (showInSceneViewField != null && (bool)showInSceneViewField.GetValue(floorConfig))
                {
                    DrawFloorOccupancyStatic(floor);
                }
            }
        }
    }
    
    // 静态版本的地面占用绘制方法
    private static void DrawFloorOccupancyStatic(MonoBehaviour floorComponent)
    {
        if (currentGridSystem == null) return;
        
        try
        {
            // 通过反射获取地面信息
            var floorType = floorComponent.GetType();
            var floorConfigField = floorType.GetField("floorConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (floorConfigField == null) return;
            
            var floorConfig = floorConfigField.GetValue(floorComponent);
            var floorConfigType = floorConfig.GetType();
            
            // 获取地面占用位置
            var getFloorPositionsMethod = floorType.GetMethod("GetFloorGridPositions");
            if (getFloorPositionsMethod == null) return;
            
            var floorPositions = (List<Vector2Int>)getFloorPositionsMethod.Invoke(floorComponent, null);
            
            // 获取是否有效位置
            var isValidMethod = floorType.GetMethod("IsValidPosition");
            bool isValid = isValidMethod != null ? (bool)isValidMethod.Invoke(floorComponent, null) : true;
            
            // 获取颜色配置
            var floorColorField = floorConfigType.GetField("floorColor");
            Color floorColor = floorColorField != null ? (Color)floorColorField.GetValue(floorConfig) : new Color(0.6f, 0.4f, 0.2f, 0.7f);
            
            // 获取高度
            var floorHeightField = floorConfigType.GetField("floorHeight");
            float floorHeight = floorHeightField != null ? (float)floorHeightField.GetValue(floorConfig) : 0f;
            
            // 绘制地面格子
            if (!isValid) floorColor = Color.red;
            floorColor.a = currentGridSystem.Settings.occupiedAlpha;
            Handles.color = floorColor;
            
            foreach (var gridPos in floorPositions)
            {
                Vector3[] corners = GetGridCellCornersAtHeight(gridPos, floorHeight);
                Handles.DrawAAConvexPolygon(corners);
            }
            
            // 绘制边框
            Handles.color = new Color(floorColor.r, floorColor.g, floorColor.b, 1f);
            foreach (var gridPos in floorPositions)
            {
                Vector3[] corners = GetGridCellCornersAtHeight(gridPos, floorHeight);
                for (int i = 0; i < corners.Length; i++)
                {
                    int next = (i + 1) % corners.Length;
                    Handles.DrawLine(corners[i], corners[next]);
                }
            }
            
            // 绘制表面格子
            var showSurfaceField = floorConfigType.GetField("showSurfaceInSceneView");
            bool showSurface = showSurfaceField != null ? (bool)showSurfaceField.GetValue(floorConfig) : false;
            
            var providesSurfaceField = floorConfigType.GetField("providesSurface");
            bool providesSurface = providesSurfaceField != null ? (bool)providesSurfaceField.GetValue(floorConfig) : false;
            
            if (showSurface && providesSurface)
            {
                var getSurfacePositionsMethod = floorType.GetMethod("GetSurfaceGridPositions");
                if (getSurfacePositionsMethod != null)
                {
                    var surfacePositions = (List<Vector2Int>)getSurfacePositionsMethod.Invoke(floorComponent, null);
                    
                    var surfaceColorField = floorConfigType.GetField("surfaceColor");
                    Color surfaceColor = surfaceColorField != null ? (Color)surfaceColorField.GetValue(floorConfig) : new Color(0.2f, 0.8f, 0.2f, 0.5f);
                    
                    var thicknessField = floorConfigType.GetField("thickness");
                    float thickness = thicknessField != null ? (float)thicknessField.GetValue(floorConfig) : 0.2f;
                    
                    float surfaceHeight = floorHeight + thickness;
                    
                    // 绘制表面格子
                    surfaceColor.a = currentGridSystem.Settings.occupiedAlpha;
                    Handles.color = surfaceColor;
                    
                    foreach (var gridPos in surfacePositions)
                    {
                        Vector3[] corners = GetGridCellCornersAtHeight(gridPos, surfaceHeight);
                        Handles.DrawAAConvexPolygon(corners);
                    }
                    
                    // 绘制表面边框
                    Handles.color = new Color(surfaceColor.r, surfaceColor.g, surfaceColor.b, 1f);
                    foreach (var gridPos in surfacePositions)
                    {
                        Vector3[] corners = GetGridCellCornersAtHeight(gridPos, surfaceHeight);
                        for (int i = 0; i < corners.Length; i++)
                        {
                            int next = (i + 1) % corners.Length;
                            Handles.DrawLine(corners[i], corners[next]);
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Error drawing floor occupancy: {ex.Message}");
        }
    }
    
    // 静态版本的家具占用绘制方法
    private static void DrawFurnitureOccupancyStatic(FurnitureItem furniture)
    {
        if (currentGridSystem == null) return;
        
        // 绘制地面占用区域
        if (furniture.Occupancy.showInSceneView)
        {
            DrawOccupancyAtLevel(furniture, 0, furniture.GetOccupiedGridPositions(), 
                furniture.Occupancy.occupiedColor, "底部占用");
        }
        
        // 绘制表面提供区域
        if (furniture.Occupancy.showSurfaceInSceneView && furniture.Occupancy.providesSurface)
        {
            int surfaceLevel = furniture.GetSurfaceHeightLevel();
            DrawOccupancyAtLevel(furniture, surfaceLevel, furniture.GetSurfaceGridPositions(), 
                furniture.Occupancy.surfaceColor, "表面提供");
        }
    }
    
    // 在指定高度层绘制占用区域
    private static void DrawOccupancyAtLevel(FurnitureItem furniture, int level, List<Vector2Int> positions, 
        Color baseColor, string label)
    {
        if (currentGridSystem == null || positions.Count == 0) return;
        
        bool isValid = furniture.IsValidPosition();
        // 使用固定的层级高度，而不是物体的实际Y坐标
        float height = currentGridSystem.GetHeightForLevel(level);
        
        Color cellColor = isValid ? baseColor : Color.red;
        cellColor.a = level == 0 ? currentGridSystem.Settings.occupiedAlpha : currentGridSystem.Settings.surfaceAlpha;
        
        Handles.color = cellColor;
        
        foreach (var gridPos in positions)
        {
            DrawGridCellAtHeight(gridPos, height);
        }
        
        // 绘制边框
        Handles.color = new Color(cellColor.r, cellColor.g, cellColor.b, 1f);
        foreach (var gridPos in positions)
        {
            DrawGridCellBorderAtHeight(gridPos, height);
        }
        
        // 显示标签
        if (positions.Count > 0)
        {
            Vector3 labelPos = currentGridSystem.GridToWorld(positions[0], height) + Vector3.up * 0.5f;
            string displayText = $"{label}\n({positions[0].x}, {positions[0].y}) L{level}\nH:{height:F1}";
            Handles.Label(labelPos, displayText);
        }
    }
    
    // 在指定高度绘制格子
    private static void DrawGridCellAtHeight(Vector2Int gridPos, float height)
    {
        Vector3[] corners = GetGridCellCornersAtHeight(gridPos, height);
        Handles.DrawAAConvexPolygon(corners);
    }
    
    // 在指定高度绘制格子边框
    private static void DrawGridCellBorderAtHeight(Vector2Int gridPos, float height)
    {
        Vector3[] corners = GetGridCellCornersAtHeight(gridPos, height);
        
        for (int i = 0; i < corners.Length; i++)
        {
            int next = (i + 1) % corners.Length;
            Handles.DrawLine(corners[i], corners[next]);
        }
    }
    
    // 获取指定高度的格子角点
    private static Vector3[] GetGridCellCornersAtHeight(Vector2Int gridPos, float height)
    {
        if (currentGridSystem == null) return new Vector3[0];
        
        Vector3 bottomLeft = currentGridSystem.GridToWorld(gridPos, height);
        Vector3 bottomRight = currentGridSystem.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = currentGridSystem.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = currentGridSystem.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    /// <summary>
    /// 获取墙面格子的角点 - 垂直的平行四边形
    /// </summary>
    private static Vector3[] GetWallSurfaceCornersAtHeight(Vector2Int gridPos, float height)
    {
        if (currentGridSystem == null) return new Vector3[0];
        
        float heightPerLevel = currentGridSystem.Settings.heightPerLevel;
        
        // 墙面格子应该是垂直的平行四边形，从当前高度到下一个高度
        Vector3 bottomLeft = currentGridSystem.GridToWorld(gridPos, height);
        Vector3 bottomRight = currentGridSystem.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = currentGridSystem.GridToWorld(gridPos + Vector2Int.right, height + heightPerLevel);
        Vector3 topLeft = currentGridSystem.GridToWorld(gridPos, height + heightPerLevel);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    // 绘制墙面格子
    private static void DrawWallGridCell(Vector3 wallPos)
    {
        if (currentGridSystem == null) return;
        
        var settings = currentGridSystem.Settings;
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
    
    // 绘制正确的墙面格子（平行四边形）
    private static void DrawWallGridCellCorners(Vector2Int baseGridPos, int wallHeightLevel, Vector2Int direction)
    {
        if (currentGridSystem == null) return;
        
        // 获取墙面格子的四个顶点
        Vector3[] corners = currentGridSystem.GetWallGridCellCorners(baseGridPos, wallHeightLevel, direction);
        
        // 使用和地面格子相同的填充样式
        Handles.DrawAAConvexPolygon(corners);
    }
    
    // 绘制墙面格子边框
    private static void DrawWallGridCellBorder(Vector2Int baseGridPos, int wallHeightLevel, Vector2Int direction)
    {
        if (currentGridSystem == null) return;
        
        // 获取墙面格子的四个顶点
        Vector3[] corners = currentGridSystem.GetWallGridCellCorners(baseGridPos, wallHeightLevel, direction);
        
        // 使用和地面格子相同的边框样式
        for (int i = 0; i < corners.Length; i++)
        {
            int next = (i + 1) % corners.Length;
            Handles.DrawLine(corners[i], corners[next]);
        }
    }
    
    // 绘制墙面格子边框
    private static void DrawWallGridCellBorder(Vector3 wallPos)
    {
        // 网格线模式下，边框和填充是一样的
        DrawWallGridCell(wallPos);
    }
    
    // 静态版本的格子角点获取方法（向后兼容）
    private static Vector3[] GetGridCellCornersStatic(Vector2Int gridPos)
    {
        return GetGridCellCornersAtHeight(gridPos, 0f);
    }
    
    void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUIStatic;
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Grid Preview Controls", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 网格系统选择
        EditorGUILayout.LabelField("Grid System", EditorStyles.boldLabel);
        currentGridSystem = EditorGUILayout.ObjectField("Current Grid", 
            currentGridSystem, typeof(GridVisualization), true) as GridVisualization;
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find in Scene"))
        {
            currentGridSystem = FindObjectOfType<GridVisualization>();
            if (currentGridSystem == null)
            {
                EditorUtility.DisplayDialog("Not Found", "No GridVisualization found in scene.", "OK");
            }
        }
        
        if (currentGridSystem != null && GUILayout.Button("Select in Scene"))
        {
            Selection.activeGameObject = currentGridSystem.gameObject;
            EditorGUIUtility.PingObject(currentGridSystem.gameObject);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 主开关控制（通过GridVisualization组件）
        if (currentGridSystem != null)
        {
            EditorGUILayout.LabelField("Main Control", EditorStyles.boldLabel);
            
            SerializedObject serializedGrid = new SerializedObject(currentGridSystem);
            SerializedProperty showGridSystemProp = serializedGrid.FindProperty("showGridSystem");
            
            EditorGUI.BeginChangeCheck();
            bool newShowGrid = EditorGUILayout.Toggle("Show Grid System", showGridSystemProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                showGridSystemProp.boolValue = newShowGrid;
                serializedGrid.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
            
            // 快速切换按钮
            if (GUILayout.Button(showGridSystemProp.boolValue ? "Hide All Grids" : "Show All Grids"))
            {
                showGridSystemProp.boolValue = !showGridSystemProp.boolValue;
                serializedGrid.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No GridVisualization selected. Please assign or find one in the scene.", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find in Scene"))
        {
            currentGridSystem = FindObjectOfType<GridVisualization>();
            if (currentGridSystem == null)
            {
                EditorUtility.DisplayDialog("Not Found", "No GridVisualization found in scene.", "OK");
            }
        }
        if (GUILayout.Button("Create New"))
        {
            CreateNewGridSystem();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 显示当前状态
        if (currentGridSystem != null)
        {
            bool isGridSystemEnabled = currentGridSystem.ShowGridSystem;
            string status = isGridSystemEnabled ? 
                "Grid System ON - All grids visible (Ground + Wall grids)" : 
                "Grid System OFF - All grids hidden (Ground + Wall grids)";
            EditorGUILayout.HelpBox(status, isGridSystemEnabled ? MessageType.Info : MessageType.Warning);
            
            // 显示控制层级
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Control Hierarchy:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. GridVisualization.showGridSystem (Master control)");
            EditorGUILayout.LabelField("2. Individual furniture/wall showInSceneView (Local control)");
            
            // 显示控制项目
            if (isGridSystemEnabled)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Controlled Items:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• Ground furniture grids (chairs, benches, etc.)");
                EditorGUILayout.LabelField("• Wall surface grids (wall panels)");
                EditorGUILayout.LabelField("• All grid overlays and highlights");
            }
            
            EditorGUILayout.Space();
            
            // 高度层级控制
            EditorGUILayout.LabelField("Height Levels", EditorStyles.boldLabel);
            var settings = currentGridSystem.Settings;
            
            bool newShowAllLevels = EditorGUILayout.Toggle("Show All Levels", settings.showAllLevels);
            if (newShowAllLevels != settings.showAllLevels)
            {
                settings.showAllLevels = newShowAllLevels;
                SceneView.RepaintAll();
            }
            
            int newMaxLevels = EditorGUILayout.IntSlider("Max Levels", settings.maxLevels, 1, 10);
            if (newMaxLevels != settings.maxLevels)
            {
                settings.maxLevels = newMaxLevels;
                SceneView.RepaintAll();
            }
            
            float newHeightPerLevel = EditorGUILayout.FloatField("Height Per Level", settings.heightPerLevel);
            if (newHeightPerLevel != settings.heightPerLevel)
            {
                settings.heightPerLevel = Mathf.Max(0.1f, newHeightPerLevel);
                SceneView.RepaintAll();
            }
            
            EditorGUILayout.Space();
            
            // 表面高亮控制
            EditorGUILayout.LabelField("Surface Highlight", EditorStyles.boldLabel);
            
            bool newEnableHighlight = EditorGUILayout.Toggle("Enable Surface Highlight", enableSurfaceHighlight);
            if (newEnableHighlight != enableSurfaceHighlight)
            {
                enableSurfaceHighlight = newEnableHighlight;
                if (!enableSurfaceHighlight)
                {
                    ClearSurfaceHighlight();
                }
                SceneView.RepaintAll();
            }
            
            if (enableSurfaceHighlight)
            {
                EditorGUI.indentLevel++;
                
                Color newHighlightColor = EditorGUILayout.ColorField("Highlight Color", surfaceHighlightColor);
                if (newHighlightColor != surfaceHighlightColor)
                {
                    surfaceHighlightColor = newHighlightColor;
                    SceneView.RepaintAll();
                }
                
                float newHighlightAlpha = EditorGUILayout.Slider("Highlight Alpha", surfaceHighlightAlpha, 0.1f, 1.0f);
                if (newHighlightAlpha != surfaceHighlightAlpha)
                {
                    surfaceHighlightAlpha = newHighlightAlpha;
                    SceneView.RepaintAll();
                }
                
                EditorGUI.indentLevel--;
                
                EditorGUILayout.HelpBox("Move mouse over scene to highlight surfaces (ground, floors, walls, furniture).", MessageType.Info);
            }

            if (GUILayout.Button("Refresh Preview"))
            {
                SceneView.RepaintAll();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No grid system found. Create one to see the preview.", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        // 家具和墙面信息
        var furniture = FindObjectsOfType<FurnitureItem>();
        var wallComponents = Object.FindObjectsOfType<MonoBehaviour>()
            .Where(mb => mb.GetType().Name == "WallItem").ToArray();
        
        EditorGUILayout.LabelField($"Furniture in scene: {furniture.Length}");
        EditorGUILayout.LabelField($"Walls in scene: {wallComponents.Length}");
        
        if (furniture.Length > 0 || wallComponents.Length > 0)
        {
            EditorGUILayout.BeginHorizontal();
            if (furniture.Length > 0 && GUILayout.Button("Select All Furniture"))
            {
                Selection.objects = furniture;
            }
            if (wallComponents.Length > 0 && GUILayout.Button("Select All Walls"))
            {
                Selection.objects = wallComponents.Cast<Object>().ToArray();
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Check Overlaps"))
            {
                CheckAndReportOverlaps();
            }
        }
    }
    
    private void CreateNewGridSystem()
    {
        GameObject go = new GameObject("Grid Visualization (Editor Only)");
        currentGridSystem = go.AddComponent<GridVisualization>();
        
        var settings = currentGridSystem.Settings;
        settings.isIsometric = true;
        settings.gridSize = new Vector2(1f, 0.5f);
        settings.gridDimensions = new Vector2Int(20, 20);
        
        Selection.activeGameObject = go;
        SceneView.RepaintAll();
        
        Debug.Log("Created new GridVisualization for editor preview!");
    }
    
    private void CheckAndReportOverlaps()
    {
        var furniture = FindObjectsOfType<FurnitureItem>();
        var overlapping = new List<FurnitureItem>();
        
        foreach (var item in furniture)
        {
            if (!item.IsValidPosition())
            {
                overlapping.Add(item);
            }
        }
        
        if (overlapping.Count > 0)
        {
            Debug.LogWarning($"Found {overlapping.Count} overlapping furniture items!");
            Selection.objects = System.Array.ConvertAll(overlapping.ToArray(), f => f.gameObject as Object);
        }
        else
        {
            Debug.Log("No overlapping furniture found. All positions are valid!");
        }
    }
    
    #region Surface Highlight System
    
    // 表面高亮系统变量（使用统一的控制变量）
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
    
    private static void HandleSurfaceHighlight(SceneView sceneView)
    {
        if (!enableSurfaceHighlight || currentGridSystem == null) return;
        
        // 只在鼠标移动和重绘事件时处理，减少处理频率
        if (Event.current.type != EventType.MouseMove && 
            Event.current.type != EventType.Repaint && 
            Event.current.type != EventType.MouseDrag)
            return;
        
        // 获取鼠标在Scene视图中的位置
        Vector2 mousePosition = Event.current.mousePosition;
        Vector3 worldPosition = GetWorldPositionFromMouse(sceneView, mousePosition);
        
        if (worldPosition == Vector3.zero) return;
        
        // 转换为网格坐标
        Vector2Int gridPos = currentGridSystem.WorldToGridIgnoreHeight(worldPosition);
        
        // 检查是否有效的网格位置
        if (!currentGridSystem.IsValidGridPosition(gridPos)) return;
        
        // 查找当前位置的所有可用表面
        var surfaces = GetAvailableSurfaces(gridPos);
        
        if (surfaces.Count > 0)
        {
            // 选择最接近鼠标世界位置高度的表面
            var selectedSurface = SelectBestSurface(surfaces, worldPosition.y);
            
            // 如果表面发生变化，更新高亮
            if (!IsSameSurface(selectedSurface, lastHighlightedSurface))
            {
                lastHighlightedGrid = gridPos;
                lastHighlightedSurface = selectedSurface;
                
                // 显示信息到控制台
                ShowSurfaceInfo(selectedSurface);
            }
            
            // 绘制高亮（传递sceneView参数以支持2D模式）
            DrawSurfaceHighlight(selectedSurface, sceneView);
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
    }
    
    private static void ClearSurfaceHighlight()
    {
        lastHighlightedGrid = null;
        lastHighlightedSurface = null;
    }
    
    /// <summary>
    /// 刷新组件缓存以提高性能
    /// </summary>
    private static void RefreshComponentCache()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastComponentCacheTime < CACHE_REFRESH_INTERVAL)
            return;
            
        lastComponentCacheTime = currentTime;
        
        // 缓存所有组件引用
        cachedFurniture = Object.FindObjectsOfType<FurnitureItem>();
        cachedWalls = Object.FindObjectsOfType<WallItem>().Cast<MonoBehaviour>().ToArray();
        cachedFloors = Object.FindObjectsOfType<FloorItem>().Cast<MonoBehaviour>().ToArray();
    }
    
    /// <summary>
    /// 清除所有缓存，强制重新查找组件
    /// </summary>
    public static void ClearCache()
    {
        cachedFurniture = null;
        cachedWalls = null;
        cachedFloors = null;
        lastComponentCacheTime = 0;
    }
    
    #region 批处理辅助方法
    
    /// <summary>
    /// 颜色转字符串（用于批次键）
    /// </summary>
    private static string ColorToString(Color color)
    {
        return $"{color.r:F2}_{color.g:F2}_{color.b:F2}_{color.a:F2}";
    }
    
    /// <summary>
    /// 添加线段到批次
    /// </summary>
    private static void AddLineToBatch(BatchDrawData batch, Vector3 start, Vector3 end)
    {
        // 检查顶点是否有效
        if (!IsValidVertex(start) || !IsValidVertex(end))
        {
            Debug.LogWarning($"Invalid vertex detected: start={start}, end={end}");
            return;
        }
        
        int startIndex = batch.vertices.Count;
        
        batch.vertices.Add(start);
        batch.vertices.Add(end);
        
        batch.indices.Add(startIndex);
        batch.indices.Add(startIndex + 1);
    }
    
    /// <summary>
    /// 检查顶点是否有效
    /// </summary>
    private static bool IsValidVertex(Vector3 vertex)
    {
        return !float.IsNaN(vertex.x) && !float.IsNaN(vertex.y) && !float.IsNaN(vertex.z) &&
               !float.IsInfinity(vertex.x) && !float.IsInfinity(vertex.y) && !float.IsInfinity(vertex.z);
    }
    
    /// <summary>
    /// 添加四边形到批次
    /// </summary>
    private static void AddQuadToBatch(BatchDrawData batch, Vector3[] corners)
    {
        int startIndex = batch.vertices.Count;
        
        // 添加顶点
        foreach (var corner in corners)
        {
            batch.vertices.Add(corner);
        }
        
        // 添加三角形索引
        batch.indices.Add(startIndex);
        batch.indices.Add(startIndex + 1);
        batch.indices.Add(startIndex + 2);
        
        batch.indices.Add(startIndex);
        batch.indices.Add(startIndex + 2);
        batch.indices.Add(startIndex + 3);
    }
    
    /// <summary>
    /// 添加四边形边框到批次
    /// </summary>
    private static void AddQuadBorderToBatch(BatchDrawData batch, Vector3[] corners)
    {
        for (int i = 0; i < corners.Length; i++)
        {
            int next = (i + 1) % corners.Length;
            AddLineToBatch(batch, corners[i], corners[next]);
        }
    }
    
    /// <summary>
    /// 绘制批次
    /// </summary>
    private static void DrawBatch(BatchDrawData batch)
    {
        if (batch.vertices.Count == 0) return;
        
        Material material = batch.isFill ? GridMaterialManager.GetFillMaterial() : GridMaterialManager.GetLineMaterial();
        material.color = batch.materialColor;
        
        if (material.SetPass(0))
        {
            GL.Begin(batch.isFill ? GL.TRIANGLES : GL.LINES);
            GL.Color(batch.materialColor);
            
            for (int i = 0; i < batch.indices.Count; i++)
            {
                int vertIndex = batch.indices[i];
                if (vertIndex >= 0 && vertIndex < batch.vertices.Count)
                {
                    GL.Vertex(batch.vertices[vertIndex]);
                }
            }
            
            GL.End();
        }
    }
    
    #endregion
    
    private static Vector3 GetWorldPositionFromMouse(SceneView sceneView, Vector2 mousePosition)
    {
        // 检查是否为2D视图
        if (sceneView.in2DMode)
        {
            // 2D模式：使用HandleUtility来获取更准确的世界坐标
            // 这个方法考虑了所有的坐标系转换
            Vector3 worldPos = HandleUtility.GUIPointToWorldRay(mousePosition).origin;
            
            // 在2D模式中，相机通常是正交的，所以射线的原点就是我们要的位置
            // 但我们需要将Z坐标设为0以匹配2D平面
            worldPos.z = 0f;
            
            return worldPos;
        }
        else
        {
            // 3D模式：使用射线投射
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            
            // 创建一个水平面进行射线投射（假设主要在XZ平面工作）
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            
            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            
            return Vector3.zero;
        }
    }
    
    private static List<SurfaceInfo> GetAvailableSurfaces(Vector2Int gridPos)
    {
        var surfaces = new List<SurfaceInfo>();
        
        // 刷新组件缓存（如果需要）
        RefreshComponentCache();
        
        // 1. 检查地面网格（基础层）
        if (currentGridSystem.IsValidGridPosition(gridPos))
        {
            surfaces.Add(new SurfaceInfo(
                SurfaceInfo.SurfaceType.Ground,
                gridPos,
                0f,
                "地面网格",
                null
            ));
        }
        
        // 2. 检查地板表面（使用缓存）
        if (cachedFloors != null)
        {
            foreach (var floor in cachedFloors)
            {
            try
            {
                var floorType = floor.GetType();
                var floorConfigField = floorType.GetField("floorConfig", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (floorConfigField == null) continue;
                
                var floorConfig = floorConfigField.GetValue(floor);
                var floorConfigType = floorConfig.GetType();
                
                var providesSurfaceField = floorConfigType.GetField("providesSurface");
                bool providesSurface = providesSurfaceField != null ? (bool)providesSurfaceField.GetValue(floorConfig) : false;
                
                var showSurfaceField = floorConfigType.GetField("showSurfaceInSceneView");
                bool showSurface = showSurfaceField != null ? (bool)showSurfaceField.GetValue(floorConfig) : false;
                
                if (providesSurface && showSurface)
                {
                    var getSurfacePositionsMethod = floorType.GetMethod("GetSurfaceGridPositions");
                    if (getSurfacePositionsMethod != null)
                    {
                        var surfacePositions = (List<Vector2Int>)getSurfacePositionsMethod.Invoke(floor, null);
                        if (surfacePositions.Contains(gridPos))
                        {
                            var floorHeightField = floorConfigType.GetField("floorHeight");
                            var thicknessField = floorConfigType.GetField("thickness");
                            float floorHeight = floorHeightField != null ? (float)floorHeightField.GetValue(floorConfig) : 0f;
                            float thickness = thicknessField != null ? (float)thicknessField.GetValue(floorConfig) : 0.2f;
                            
                            surfaces.Add(new SurfaceInfo(
                                SurfaceInfo.SurfaceType.Floor,
                                gridPos,
                                floorHeight + thickness,
                                floor.name,
                                floor
                            ));
                        }
                    }
                }
            }
            catch { /* 忽略反射错误 */ }
            }
        }
        
        // 3. 检查家具表面（使用缓存）
        if (cachedFurniture != null)
        {
            foreach (var item in cachedFurniture)
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
        }
        
        // 4. 检查墙面表面（使用缓存）
        if (cachedWalls != null)
        {
            foreach (var wall in cachedWalls)
        {
            try
            {
                var wallType = wall.GetType();
                var wallConfigField = wallType.GetField("wallConfig", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (wallConfigField == null) continue;
                
                var wallConfig = wallConfigField.GetValue(wall);
                var wallConfigType = wallConfig.GetType();
                
                var providesSurfaceField = wallConfigType.GetField("providesWallSurface");
                bool providesSurface = providesSurfaceField != null ? (bool)providesSurfaceField.GetValue(wallConfig) : false;
                
                var showSurfaceField = wallConfigType.GetField("showWallSurfaceInSceneView");
                bool showSurface = showSurfaceField != null ? (bool)showSurfaceField.GetValue(wallConfig) : false;
                
                if (providesSurface && showSurface)
                {
                    // 墙面使用GetWallSurfacePositions方法，返回Vector3列表
                    var getWallSurfacePositionsMethod = wallType.GetMethod("GetWallSurfacePositions");
                    if (getWallSurfacePositionsMethod != null)
                    {
                        var wallSurfacePositions = (List<Vector3>)getWallSurfacePositionsMethod.Invoke(wall, null);
                        
                        // 检查是否有墙面位置包含当前网格位置
                        foreach (var wallPos in wallSurfacePositions)
                        {
                            // 将墙面3D位置转换为2D网格坐标进行比较
                            Vector2Int wallGridPos = currentGridSystem.WorldToGridIgnoreHeight(wallPos);
                            if (wallGridPos == gridPos)
                            {
                                var wallHeightField = wallConfigType.GetField("wallHeight");
                                var baseHeightField = wallConfigType.GetField("baseHeight");
                                float wallHeight = wallHeightField != null ? (float)wallHeightField.GetValue(wallConfig) : 3f;
                                float baseHeight = baseHeightField != null ? (float)baseHeightField.GetValue(wallConfig) : 0f;
                                
                                surfaces.Add(new SurfaceInfo(
                                    SurfaceInfo.SurfaceType.WallSurface,
                                    gridPos,
                                    wallPos.y, // 使用实际的Y坐标作为高度
                                    wall.name,
                                    wall
                                ));
                                break; // 找到匹配的墙面位置后退出循环
                            }
                        }
                    }
                }
            }
            catch { /* 忽略反射错误 */ }
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
        
        // 减少控制台输出频率，避免刷屏
        if (s.sourceObject != null)
        {
            Debug.Log($"鼠标指向: {typeText} - {s.objectName} | 网格({s.gridPosition.x}, {s.gridPosition.y}) | 高度: {s.height:F2}");
        }
        else
        {
            Debug.Log($"鼠标指向: {typeText} | 网格({s.gridPosition.x}, {s.gridPosition.y}) | 高度: {s.height:F2}");
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
    
    private static void DrawSurfaceHighlight(SurfaceInfo? surface, SceneView sceneView)
    {
        if (!surface.HasValue || currentGridSystem == null) return;
        
        // 只在Repaint事件时绘制，避免不必要的绘制调用
        if (Event.current.type != EventType.Repaint) return;
        
        var s = surface.Value;
        
        // 设置高亮颜色
        Color color = surfaceHighlightColor;
        color.a = surfaceHighlightAlpha;
        Handles.color = color;
        
        // 获取格子角点
        Vector3[] corners = GetSurfaceCorners(s);
        
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
        
        // 使用简单的标签显示
        var defaultColor = GUI.color;
        GUI.color = Color.yellow;
        Handles.Label(center + Vector3.up * 0.5f, label);
        GUI.color = defaultColor;
    }
    
    private static Vector3[] GetSurfaceCorners(SurfaceInfo surface)
    {
        Vector2Int gridPos = surface.gridPosition;
        float height = surface.height;
        
        Vector3 bottomLeft = currentGridSystem.GridToWorld(gridPos, height);
        Vector3 bottomRight = currentGridSystem.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = currentGridSystem.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = currentGridSystem.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    #endregion
}
