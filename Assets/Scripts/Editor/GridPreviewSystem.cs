using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

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
            return;
            
        DrawGridPreviewStatic();
        DrawFurniturePreviewStatic();
        DrawWallPreviewStatic();
        
        // 强制Scene视图重绘以保持实时更新
        if (Event.current.type == EventType.Repaint)
        {
            sceneView.Repaint();
        }
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
        {
            currentGridSystem = FindObjectOfType<GridVisualization>();
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
}
