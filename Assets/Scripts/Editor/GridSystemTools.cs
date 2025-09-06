using UnityEngine;
using UnityEditor;

public class GridSystemTools : EditorWindow
{
    private GridVisualization gridVisualization;
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Grid System/Grid Visualization Tools")]
    public static void ShowWindow()
    {
        GridSystemTools window = GetWindow<GridSystemTools>();
        window.titleContent = new GUIContent("Grid System Tools");
        window.Show();
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("Grid System Tools", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 查找或创建GridVisualization
        DrawGridVisualizationSection();
        
        EditorGUILayout.Space();
        
        // 家具管理
        DrawFurnitureManagementSection();
        
        EditorGUILayout.Space();
        
        // 快捷操作
        DrawQuickActionsSection();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawGridVisualizationSection()
    {
        EditorGUILayout.LabelField("Grid Visualization", EditorStyles.boldLabel);
        
        gridVisualization = EditorGUILayout.ObjectField("Grid System", 
            gridVisualization, typeof(GridVisualization), true) as GridVisualization;
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Find in Scene"))
        {
            gridVisualization = FindObjectOfType<GridVisualization>();
            if (gridVisualization == null)
            {
                Debug.LogWarning("No GridVisualization found in scene!");
            }
        }
        
        if (GUILayout.Button("Create New"))
        {
            CreateGridVisualization();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (gridVisualization != null)
        {
            EditorGUILayout.HelpBox("Grid system found and ready to use!", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("No grid system selected. Please find or create one.", MessageType.Warning);
        }
    }
    
    private void DrawFurnitureManagementSection()
    {
        EditorGUILayout.LabelField("Furniture Management", EditorStyles.boldLabel);
        
        var furniture = FindObjectsOfType<FurnitureItem>();
        EditorGUILayout.LabelField($"Furniture in scene: {furniture.Length}");
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Select All Furniture"))
        {
            Selection.objects = furniture;
        }
        
        if (GUILayout.Button("Snap All to Grid"))
        {
            SnapAllFurnitureToGrid();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Add Furniture Component to Selected"))
        {
            AddFurnitureComponentToSelected();
        }
        
        // 显示重叠检查
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Overlap Check", EditorStyles.boldLabel);
        
        var overlapping = GetOverlappingFurniture();
        if (overlapping.Count > 0)
        {
            EditorGUILayout.HelpBox($"Found {overlapping.Count} overlapping furniture items!", MessageType.Warning);
            if (GUILayout.Button("Select Overlapping Items"))
            {
                Selection.objects = overlapping.ToArray();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No overlapping furniture found.", MessageType.Info);
        }
    }
    
    private void DrawQuickActionsSection()
    {
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refresh Scene View"))
        {
            SceneView.RepaintAll();
        }
        
        if (GUILayout.Button("Create Bench Prefab"))
        {
            CreateBenchPrefab();
        }
        
        if (GUILayout.Button("Create Chair Prefab"))
        {
            CreateChairPrefab();
        }
    }
    
    private void CreateGridVisualization()
    {
        GameObject go = new GameObject("Grid Visualization");
        gridVisualization = go.AddComponent<GridVisualization>();
        
        // 设置默认的等距视角设置 (XY平面)
        var settings = gridVisualization.Settings;
        settings.isIsometric = true;
        settings.gridSize = new Vector2(1f, 0.5f);  // XY平面等距视角的典型比例
        settings.gridDimensions = new Vector2Int(20, 20);
        
        Selection.activeGameObject = go;
        
        Debug.Log("Created new GridVisualization component!");
    }
    
    private void SnapAllFurnitureToGrid()
    {
        if (gridVisualization == null)
        {
            Debug.LogWarning("No grid system selected!");
            return;
        }
        
        var furniture = FindObjectsOfType<FurnitureItem>();
        
        Undo.RecordObjects(System.Array.ConvertAll(furniture, f => f.transform as Object), "Snap All to Grid");
        
        foreach (var item in furniture)
        {
            Vector2Int gridPos = gridVisualization.WorldToGrid(item.transform.position);
            Vector3 snappedPos = gridVisualization.GridToWorld(gridPos);
            item.transform.position = snappedPos;
        }
        
        Debug.Log($"Snapped {furniture.Length} furniture items to grid.");
    }
    
    private void AddFurnitureComponentToSelected()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No object selected!");
            return;
        }
        
        foreach (var obj in Selection.gameObjects)
        {
            if (obj.GetComponent<FurnitureItem>() == null)
            {
                Undo.AddComponent<FurnitureItem>(obj);
            }
        }
        
        Debug.Log($"Added FurnitureItem component to {Selection.gameObjects.Length} objects.");
    }
    
    private System.Collections.Generic.List<GameObject> GetOverlappingFurniture()
    {
        var overlapping = new System.Collections.Generic.List<GameObject>();
        var furniture = FindObjectsOfType<FurnitureItem>();
        
        foreach (var item in furniture)
        {
            if (!item.IsValidPosition())
            {
                overlapping.Add(item.gameObject);
            }
        }
        
        return overlapping;
    }
    
    private void CreateBenchPrefab()
    {
        CreateFurniturePrefab("Bench", new Vector2Int(2, 1), Color.yellow);
    }
    
    private void CreateChairPrefab()
    {
        CreateFurniturePrefab("Chair", new Vector2Int(1, 1), Color.blue);
    }
    
    private void CreateFurniturePrefab(string name, Vector2Int size, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        
        // 设置可视化
        var renderer = go.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        renderer.material = material;
        
        // 添加家具组件
        var furniture = go.AddComponent<FurnitureItem>();
        furniture.Occupancy.size = size;
        furniture.Occupancy.occupiedColor = color;
        furniture.GridSystem = gridVisualization;
        
        // 调整大小以适应网格 (XY平面2D游戏)
        if (gridVisualization != null)
        {
            var settings = gridVisualization.Settings;
            go.transform.localScale = new Vector3(
                size.x * settings.gridSize.x * 0.8f,
                size.y * settings.gridSize.y * 0.8f,
                0.5f  // Z轴深度保持较小
            );
        }
        
        Selection.activeGameObject = go;
        
        Debug.Log($"Created {name} prefab!");
    }
}
