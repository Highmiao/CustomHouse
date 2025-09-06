using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 地面系统示例脚本
/// 演示如何在编辑器中创建和使用地面
/// </summary>
public class FloorSystemExample : MonoBehaviour
{
    [Header("Floor Creation Settings")]
    public GridVisualization gridSystem;
    
    [Header("Example Settings")]
    public int numberOfGroundFloors = 3;
    public int numberOfSecondFloors = 2;
    public float secondFloorHeight = 3f;
    
#if UNITY_EDITOR
    [ContextMenu("Generate Example Floors")]
    public void GenerateExampleFloors()
    {
        if (gridSystem == null)
        {
            gridSystem = FindObjectOfType<GridVisualization>();
            if (gridSystem == null)
            {
                Debug.LogError("No GridVisualization found in scene!");
                return;
            }
        }
        
        // 清除现有地面
        ClearExistingFloors();
        
        // 生成地面层地面
        for (int i = 0; i < numberOfGroundFloors; i++)
        {
            CreateGroundFloor(new Vector2Int(i * 4, 0), new Vector2Int(3, 3));
        }
        
        // 生成二楼地面
        for (int i = 0; i < numberOfSecondFloors; i++)
        {
            CreateSecondFloor(new Vector2Int(i * 3, 5), new Vector2Int(2, 2), secondFloorHeight);
        }
        
        Debug.Log($"Generated {numberOfGroundFloors} ground floors and {numberOfSecondFloors} second floors!");
        
        // 刷新Scene视图
        SceneView.RepaintAll();
    }
    
    [ContextMenu("Clear All Floors")]
    public void ClearExistingFloors()
    {
        var floors = FindObjectsOfType<FloorItem>();
        foreach (var item in floors)
        {
            if (Application.isPlaying)
                Destroy(item.gameObject);
            else
                DestroyImmediate(item.gameObject);
        }
        
        // 刷新Scene视图
        SceneView.RepaintAll();
    }
    
    private void CreateGroundFloor(Vector2Int gridPosition, Vector2Int size)
    {
        GameObject floorGO = CreateFloorItem("Ground Floor", gridPosition, 0f);
        
        // 设置地面属性
        var floor = floorGO.GetComponent<FloorItem>();
        var config = floor.FloorConfig;
        config.size = size;
        config.floorHeight = 0f; // 地面层
        config.thickness = 0.1f;
        config.floorColor = new Color(0.6f, 0.4f, 0.2f, 0.7f); // 棕色
        config.surfaceColor = new Color(0.2f, 0.8f, 0.2f, 0.5f); // 绿色表面
        config.providesSurface = true;
        config.surfaceSize = size; // 表面与地面同大小
        
        // 设置外观
        SetFloorAppearance(floorGO, config.floorColor, size, config.thickness);
    }
    
    private void CreateSecondFloor(Vector2Int gridPosition, Vector2Int size, float height)
    {
        GameObject floorGO = CreateFloorItem("Second Floor", gridPosition, height);
        
        // 设置二楼地面属性
        var floor = floorGO.GetComponent<FloorItem>();
        var config = floor.FloorConfig;
        config.size = size;
        config.floorHeight = height;
        config.thickness = 0.2f;
        config.floorColor = new Color(0.5f, 0.3f, 0.1f, 0.8f); // 深棕色
        config.surfaceColor = new Color(0.3f, 0.7f, 0.3f, 0.6f); // 深绿色表面
        config.providesSurface = true;
        config.surfaceSize = size; // 表面与地面同大小
        
        // 设置外观
        SetFloorAppearance(floorGO, config.floorColor, size, config.thickness);
    }
    
    private GameObject CreateFloorItem(string name, Vector2Int gridPosition, float height)
    {
        GameObject floorGO = new GameObject(name);
        FloorItem floor = floorGO.AddComponent<FloorItem>();
        floor.GridSystem = gridSystem;
        
        // 设置位置
        floorGO.transform.position = gridSystem.GridToWorld(gridPosition, height);
        
        return floorGO;
    }
    
    private void SetFloorAppearance(GameObject floorGO, Color color, Vector2Int size, float thickness)
    {
        // 添加可视化对象
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(floorGO.transform, false);
        visual.name = "Visual";
        
        // 设置材质
        var renderer = visual.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        renderer.material = material;
        
        // 调整尺寸 (适合XY平面的2D游戏)
        if (gridSystem != null)
        {
            var settings = gridSystem.Settings;
            visual.transform.localScale = new Vector3(
                size.x * settings.gridSize.x * 0.9f,
                thickness,
                size.y * settings.gridSize.y * 0.9f
            );
            
            // 调整位置让地面在正确高度
            visual.transform.localPosition = new Vector3(
                (size.x - 1) * settings.gridSize.x * 0.5f,
                thickness * 0.5f,
                (size.y - 1) * settings.gridSize.y * 0.5f
            );
        }
    }
    
    /// <summary>
    /// 检查指定网格位置是否可以放置地面
    /// </summary>
    public bool CanPlaceFloorAt(Vector2Int gridPosition, Vector2Int size, float height)
    {
        if (gridSystem == null) return false;
        
        // 检查是否在网格范围内
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int checkPos = gridPosition + new Vector2Int(x, y);
                if (!gridSystem.IsValidGridPosition(checkPos))
                    return false;
            }
        }
        
        // 检查是否与现有地面重叠（相同高度）
        var allFloors = FindObjectsOfType<FloorItem>();
        foreach (var floor in allFloors)
        {
            if (Mathf.Approximately(floor.FloorConfig.floorHeight, height))
            {
                var occupiedPositions = floor.GetFloorGridPositions();
                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector2Int checkPos = gridPosition + new Vector2Int(x, y);
                        if (occupiedPositions.Contains(checkPos))
                            return false;
                    }
                }
            }
        }
        
        return true;
    }
#else
    void Start()
    {
        Debug.LogWarning("FloorSystemExample is editor-only and will be disabled at runtime.");
        this.enabled = false;
    }
#endif
}
