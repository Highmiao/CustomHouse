using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 墙面系统示例脚本
/// 演示如何在编辑器中创建和使用墙面
/// </summary>
public class WallSystemExample : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("GameObject/Grid System/Create Wall")]
    public static void CreateWall()
    {
        // 查找网格系统
        GridVisualization gridSystem = FindObjectOfType<GridVisualization>();
        if (gridSystem == null)
        {
            Debug.LogError("No GridVisualization found in scene! Please add one first.");
            return;
        }
        
        // 创建墙面对象
        GameObject wallGO = new GameObject("Wall");
        WallItem wall = wallGO.AddComponent<WallItem>();
        wall.GridSystem = gridSystem;
        
        // 设置默认配置
        var config = wall.WallConfig;
        config.direction = WallDirection.North;
        config.wallLength = 3;
        config.wallHeight = 3;
        config.providesWallSurface = true;
        config.showInSceneView = true;
        config.showWallSurfaceInSceneView = true;
        config.baseOccupiedColor = Color.red;
        config.wallSurfaceColor = Color.cyan;
        
        // 定位到场景中心
        Vector3 centerPos = Vector3.zero;
        if (SceneView.lastActiveSceneView != null)
        {
            centerPos = SceneView.lastActiveSceneView.pivot;
        }
        
        // 对齐到网格
        Vector2Int gridPos = gridSystem.WorldToGridIgnoreHeight(centerPos);
        wallGO.transform.position = gridSystem.GridToWorld(gridPos, 0f);
        
        // 选中新创建的墙面
        Selection.activeGameObject = wallGO;
        
        // 确保场景视图刷新
        SceneView.RepaintAll();
        
        Debug.Log("Created new wall at grid position: " + gridPos);
    }
    
    [MenuItem("GameObject/Grid System/Create Wall Example Scene")]
    public static void CreateWallExampleScene()
    {
        // 确保有网格系统
        GridVisualization gridSystem = FindObjectOfType<GridVisualization>();
        if (gridSystem == null)
        {
            CreateBasicGridSystem();
            gridSystem = FindObjectOfType<GridVisualization>();
        }
        
        // 创建一个房间布局示例
        CreateRoomLayout(gridSystem);
        
        Debug.Log("Created wall example scene with room layout!");
    }
    
    private static void CreateBasicGridSystem()
    {
        GameObject go = new GameObject("Grid Visualization (Editor Only)");
        GridVisualization grid = go.AddComponent<GridVisualization>();
        
        var settings = grid.Settings;
        settings.isIsometric = true;
        settings.gridSize = new Vector2(1f, 0.5f);
        settings.gridDimensions = new Vector2Int(20, 20);
        
        Debug.Log("Created new GridVisualization for wall system!");
    }
    
    private static void CreateRoomLayout(GridVisualization gridSystem)
    {
        // 创建房间的四面墙
        CreateWallAtPosition(gridSystem, new Vector2Int(0, 0), WallDirection.North, 5, "North Wall");
        CreateWallAtPosition(gridSystem, new Vector2Int(0, 5), WallDirection.East, 4, "East Wall");
        CreateWallAtPosition(gridSystem, new Vector2Int(4, 1), WallDirection.South, 5, "South Wall");
        CreateWallAtPosition(gridSystem, new Vector2Int(0, 0), WallDirection.West, 5, "West Wall");
        
        // 添加一些家具
        CreateFurnitureAtPosition(gridSystem, new Vector2Int(1, 1), "Chair");
        CreateFurnitureAtPosition(gridSystem, new Vector2Int(3, 3), "Table");
    }
    
    private static void CreateWallAtPosition(GridVisualization gridSystem, Vector2Int gridPos, 
        WallDirection direction, int length, string name)
    {
        GameObject wallGO = new GameObject(name);
        WallItem wall = wallGO.AddComponent<WallItem>();
        wall.GridSystem = gridSystem;
        
        var config = wall.WallConfig;
        config.direction = direction;
        config.wallLength = length;
        config.wallHeight = 3;
        config.providesWallSurface = true;
        config.showInSceneView = true;
        config.showWallSurfaceInSceneView = true;
        config.baseOccupiedColor = GetColorForDirection(direction);
        config.wallSurfaceColor = Color.cyan;
        
        wallGO.transform.position = gridSystem.GridToWorld(gridPos, 0f);
    }
    
    private static void CreateFurnitureAtPosition(GridVisualization gridSystem, Vector2Int gridPos, string name)
    {
        GameObject furnitureGO = new GameObject(name);
        FurnitureItem furniture = furnitureGO.AddComponent<FurnitureItem>();
        furniture.GridSystem = gridSystem;
        
        var occupancy = furniture.Occupancy;
        occupancy.size = Vector2Int.one;
        occupancy.baseHeight = 0f;
        occupancy.furnitureHeight = 1f;
        occupancy.providesSurface = true;
        occupancy.showInSceneView = true;
        occupancy.showSurfaceInSceneView = true;
        occupancy.occupiedColor = Color.blue;
        occupancy.surfaceColor = Color.green;
        
        furnitureGO.transform.position = gridSystem.GridToWorld(gridPos, 0f);
    }
    
    private static Color GetColorForDirection(WallDirection direction)
    {
        switch (direction)
        {
            case WallDirection.North: return Color.red;
            case WallDirection.East: return Color.green;
            case WallDirection.South: return Color.blue;
            case WallDirection.West: return Color.yellow;
            default: return Color.gray;
        }
    }
    
    [MenuItem("Tools/Grid System/Wall Tools")]
    public static void ShowWallTools()
    {
        EditorUtility.DisplayDialog("Wall Tools", 
            "Wall system tools:\\n" +
            "• GameObject → Grid System → Create Wall: Creates a single wall\\n" +
            "• GameObject → Grid System → Create Wall Example Scene: Creates a room with walls and furniture\\n" +
            "• Tools → Grid System → Grid Preview Window: Shows the grid preview window\\n" +
            "• Tools → Grid System → Toggle Grid Preview: Toggles grid visualization", 
            "OK");
    }
#endif
}
