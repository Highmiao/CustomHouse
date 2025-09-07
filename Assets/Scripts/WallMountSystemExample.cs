using UnityEngine;

/// <summary>
/// 墙面挂载物体示例脚本
/// 展示如何创建和配置可以挂载到墙面上的物体
/// </summary>
public class WallMountSystemExample : MonoBehaviour
{
    [Header("Example Settings")]
    public GameObject wallMountablePrefab; // 预制体模板
    public Transform parentTransform; // 生成的物体的父对象
    
    [Header("Test Positions")]
    public Vector2Int[] testPositions = {
        new Vector2Int(2, 0),
        new Vector2Int(5, 0),
        new Vector2Int(8, 0)
    };
    
    void Start()
    {
        CreateExampleWallMountables();
    }
    
    /// <summary>
    /// 创建示例墙面挂载物体
    /// </summary>
    void CreateExampleWallMountables()
    {
        var gridSystem = FindObjectOfType<GridVisualization>();
        if (gridSystem == null)
        {
            Debug.LogWarning("No GridVisualization found in scene!");
            return;
        }
        
        // 创建不同类型的墙面挂载物体
        CreatePicture();
        CreateWallShelf();
        CreateWallClock();
        CreateWallLamp();
    }
    
    /// <summary>
    /// 创建墙面画框
    /// </summary>
    void CreatePicture()
    {
        GameObject picture = CreateWallMountable("Wall Picture", new Vector2Int(2, 2));
        var mountConfig = picture.GetComponent<WallMountableItem>().MountConfig;
        
        // 配置画框
        mountConfig.gridSize = new Vector2Int(2, 1); // 2x1 的画框
        mountConfig.mountHeightLevel = 2; // 挂在第2层高度
        mountConfig.heightOffset = 0f;
        
        mountConfig.canMountOnNorth = true;
        mountConfig.canMountOnEast = true;
        mountConfig.canMountOnSouth = true;
        mountConfig.canMountOnWest = true;
        
        mountConfig.providesSurface = false; // 画框不提供表面
        mountConfig.mountColor = new Color(0.8f, 0.6f, 0.4f, 0.7f); // 棕色
        
        mountConfig.showInSceneView = true;
        
        Debug.Log("Created Wall Picture at position (2, 2)");
    }
    
    /// <summary>
    /// 创建墙面置物架
    /// </summary>
    void CreateWallShelf()
    {
        GameObject shelf = CreateWallMountable("Wall Shelf", new Vector2Int(5, 2));
        var mountConfig = shelf.GetComponent<WallMountableItem>().MountConfig;
        
        // 配置置物架
        mountConfig.gridSize = new Vector2Int(3, 1); // 3x1 的置物架
        mountConfig.mountHeightLevel = 1; // 挂在第1层高度
        mountConfig.heightOffset = 0.5f; // 在层级中间位置
        
        mountConfig.canMountOnNorth = true;
        mountConfig.canMountOnEast = true;
        mountConfig.canMountOnSouth = true;
        mountConfig.canMountOnWest = true;
        
        // 置物架提供表面
        mountConfig.providesSurface = true;
        mountConfig.surfaceSize = new Vector2Int(3, 1); // 3x1 的表面
        mountConfig.surfaceOffset = Vector2Int.zero;
        mountConfig.surfaceProtrusion = 0.3f; // 向外突出0.3单位
        
        mountConfig.mountColor = new Color(0.6f, 0.4f, 0.2f, 0.7f); // 深棕色
        mountConfig.surfaceColor = new Color(0.2f, 0.8f, 0.6f, 0.5f); // 青绿色表面
        
        mountConfig.showInSceneView = true;
        mountConfig.showSurfaceInSceneView = true;
        
        Debug.Log("Created Wall Shelf at position (5, 2) with surface");
    }
    
    /// <summary>
    /// 创建墙面时钟
    /// </summary>
    void CreateWallClock()
    {
        GameObject clock = CreateWallMountable("Wall Clock", new Vector2Int(8, 2));
        var mountConfig = clock.GetComponent<WallMountableItem>().MountConfig;
        
        // 配置时钟
        mountConfig.gridSize = new Vector2Int(1, 1); // 1x1 的时钟
        mountConfig.mountHeightLevel = 3; // 挂在第3层高度
        mountConfig.heightOffset = 0f;
        
        mountConfig.canMountOnNorth = true;
        mountConfig.canMountOnEast = true;
        mountConfig.canMountOnSouth = true;
        mountConfig.canMountOnWest = true;
        
        mountConfig.providesSurface = false; // 时钟不提供表面
        mountConfig.mountColor = new Color(0.2f, 0.2f, 0.2f, 0.7f); // 深灰色
        
        mountConfig.showInSceneView = true;
        
        Debug.Log("Created Wall Clock at position (8, 2)");
    }
    
    /// <summary>
    /// 创建墙面灯具
    /// </summary>
    void CreateWallLamp()
    {
        GameObject lamp = CreateWallMountable("Wall Lamp", new Vector2Int(10, 2));
        var mountConfig = lamp.GetComponent<WallMountableItem>().MountConfig;
        
        // 配置灯具
        mountConfig.gridSize = new Vector2Int(1, 1); // 1x1 的灯具
        mountConfig.mountHeightLevel = 2; // 挂在第2层高度
        mountConfig.heightOffset = 0.8f; // 接近该层级顶部
        
        // 限制只能挂在特定墙面上
        mountConfig.canMountOnNorth = true;
        mountConfig.canMountOnEast = false;
        mountConfig.canMountOnSouth = true;
        mountConfig.canMountOnWest = false;
        
        mountConfig.providesSurface = false; // 灯具不提供表面
        mountConfig.mountColor = new Color(1f, 1f, 0.2f, 0.7f); // 黄色
        
        mountConfig.showInSceneView = true;
        
        Debug.Log("Created Wall Lamp at position (10, 2) - limited to North/South walls");
    }
    
    /// <summary>
    /// 创建基础的墙面挂载物体
    /// </summary>
    GameObject CreateWallMountable(string itemName, Vector2Int position)
    {
        GameObject obj;
        
        if (wallMountablePrefab != null)
        {
            obj = Instantiate(wallMountablePrefab, parentTransform);
        }
        else
        {
            obj = new GameObject();
            if (parentTransform != null)
                obj.transform.SetParent(parentTransform);
        }
        
        obj.name = itemName;
        
        // 添加 WallMountableItem 组件
        var mountable = obj.GetComponent<WallMountableItem>();
        if (mountable == null)
        {
            mountable = obj.AddComponent<WallMountableItem>();
        }
        
        // 设置位置和方向
        mountable.SetMountPosition(position, WallDirection.North);
        
        return obj;
    }
    
    /// <summary>
    /// 清除所有示例物体
    /// </summary>
    [ContextMenu("Clear All Wall Mountables")]
    public void ClearAllWallMountables()
    {
        var wallMountables = FindObjectsOfType<WallMountableItem>();
        foreach (var item in wallMountables)
        {
            if (Application.isPlaying)
                Destroy(item.gameObject);
            else
                DestroyImmediate(item.gameObject);
        }
        
        Debug.Log($"Cleared {wallMountables.Length} wall mountable items");
    }
    
    /// <summary>
    /// 检查所有墙面挂载物体的有效性
    /// </summary>
    [ContextMenu("Validate All Wall Mountables")]
    public void ValidateAllWallMountables()
    {
        var wallMountables = FindObjectsOfType<WallMountableItem>();
        int validCount = 0;
        int invalidCount = 0;
        
        foreach (var item in wallMountables)
        {
            if (item.IsValidPosition())
            {
                validCount++;
                Debug.Log($"✓ {item.name} is in a valid position");
            }
            else
            {
                invalidCount++;
                Debug.LogWarning($"✗ {item.name} is in an invalid position!");
            }
        }
        
        Debug.Log($"Validation complete: {validCount} valid, {invalidCount} invalid wall mountables");
    }
    
    /// <summary>
    /// 演示如何手动移动墙面挂载物体
    /// </summary>
    [ContextMenu("Demo: Move Wall Mountables")]
    public void DemoMoveWallMountables()
    {
        var wallMountables = FindObjectsOfType<WallMountableItem>();
        
        foreach (var item in wallMountables)
        {
            // 随机移动到新位置
            Vector2Int randomPos = new Vector2Int(
                Random.Range(0, 10),
                Random.Range(0, 10)
            );
            
            // 随机选择墙面方向
            WallDirection[] directions = { WallDirection.North, WallDirection.East, WallDirection.South, WallDirection.West };
            WallDirection randomDirection = directions[Random.Range(0, directions.Length)];
            
            item.SetMountPosition(randomPos, randomDirection);
            
            Debug.Log($"Moved {item.name} to ({randomPos.x}, {randomPos.y}) facing {randomDirection}");
        }
    }
    
    /// <summary>
    /// 同步所有墙面挂载物体的位置
    /// </summary>
    [ContextMenu("Sync Wall Mountable Positions")]
    public void SyncWallMountablePositions()
    {
        var wallMountables = FindObjectsOfType<WallMountableItem>();
        
        foreach (var item in wallMountables)
        {
            // 强制从 Transform 位置更新挂载位置
            var gridSystem = item.GridSystem;
            if (gridSystem != null)
            {
                Vector2Int gridPos = gridSystem.WorldToGridIgnoreHeight(item.transform.position);
                var nearestWall = item.GetType().GetMethod("FindNearestWall", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (nearestWall != null)
                {
                    var wall = nearestWall.Invoke(item, new object[] { gridPos });
                    if (wall != null)
                    {
                        var wallConfigField = wall.GetType().GetField("WallConfig");
                        if (wallConfigField != null)
                        {
                            var wallConfig = wallConfigField.GetValue(wall);
                            var directionField = wallConfig.GetType().GetField("direction");
                            if (directionField != null)
                            {
                                var direction = (WallDirection)directionField.GetValue(wallConfig);
                                item.SetMountPosition(gridPos, direction);
                            }
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Synchronized {wallMountables.Length} wall mountable positions");
    }
}
