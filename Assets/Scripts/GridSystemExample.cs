using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 示例脚本：展示如何在编辑器中使用网格系统进行预览
/// 这个脚本仅在编辑器模式下工作
/// </summary>
public class GridSystemExample : MonoBehaviour
{
    [Header("示例设置")]
    public GridVisualization gridSystem;
    public GameObject benchPrefab;
    public GameObject chairPrefab;
    
    [Header("编辑器预览设置")]
    [SerializeField] private bool autoFindGridSystem = true;
    public int numberOfBenches = 3;
    public int numberOfChairs = 5;
    
#if UNITY_EDITOR
    void OnValidate()
    {
        if (autoFindGridSystem && gridSystem == null)
        {
            gridSystem = FindObjectOfType<GridVisualization>();
        }
    }
#endif
    
    [ContextMenu("Generate Example Furniture")]
    public void GenerateExampleFurniture()
    {
#if UNITY_EDITOR
        if (gridSystem == null)
        {
            gridSystem = FindObjectOfType<GridVisualization>();
            if (gridSystem == null)
            {
                Debug.LogError("No GridVisualization found in scene!");
                return;
            }
        }
        
        // 清除现有家具
        ClearExistingFurniture();
        
        // 生成凳子
        for (int i = 0; i < numberOfBenches; i++)
        {
            CreateBench(new Vector2Int(i * 3, 2));
        }
        
        // 生成椅子
        for (int i = 0; i < numberOfChairs; i++)
        {
            CreateChair(new Vector2Int(i * 2, 5));
        }
        
        // 刷新Scene视图以显示更新
        SceneView.RepaintAll();
#else
        Debug.LogWarning("GenerateExampleFurniture is only available in editor mode!");
#endif
    }
    
    [ContextMenu("Clear All Furniture")]
    public void ClearExistingFurniture()
    {
#if UNITY_EDITOR
        var furniture = FindObjectsOfType<FurnitureItem>();
        foreach (var item in furniture)
        {
            if (Application.isPlaying)
                Destroy(item.gameObject);
            else
                DestroyImmediate(item.gameObject);
        }
        
        // 刷新Scene视图
        SceneView.RepaintAll();
#else
        Debug.LogWarning("ClearExistingFurniture is only available in editor mode!");
#endif
    }
    
    private void CreateBench(Vector2Int gridPosition)
    {
        GameObject bench = CreateFurnitureItem("Bench", gridPosition);
        
        // 设置凳子的属性
        var furniture = bench.GetComponent<FurnitureItem>();
        furniture.Occupancy.size = new Vector2Int(2, 1); // 凳子占据2x1格子
        furniture.Occupancy.occupiedColor = Color.yellow;
        
        // 设置高度和表面
        furniture.Occupancy.baseHeight = 0f; // 凳子放在地面上
        furniture.Occupancy.furnitureHeight = 0.8f; // 凳子高度
        furniture.Occupancy.providesSurface = true; // 提供表面
        furniture.Occupancy.surfaceSize = new Vector2Int(2, 1); // 表面大小与底部相同
        furniture.Occupancy.surfaceColor = new Color(1f, 1f, 0f, 0.5f); // 淡黄色表面
        
        // 设置外观
        var renderer = bench.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.yellow;
            renderer.material = material;
        }
        
        // 调整尺寸 (适合XY平面的2D游戏)
        bench.transform.localScale = new Vector3(
            furniture.Occupancy.size.x * gridSystem.Settings.gridSize.x * 0.8f,
            furniture.Occupancy.furnitureHeight,
            0.5f  // Z轴深度保持较小
        );
    }
    
    private void CreateChair(Vector2Int gridPosition)
    {
        GameObject chair = CreateFurnitureItem("Chair", gridPosition);
        
        // 设置椅子的属性
        var furniture = chair.GetComponent<FurnitureItem>();
        furniture.Occupancy.size = new Vector2Int(1, 1); // 椅子占据1x1格子
        furniture.Occupancy.occupiedColor = Color.blue;
        
        // 设置高度和表面
        furniture.Occupancy.baseHeight = 0f; // 椅子放在地面上
        furniture.Occupancy.furnitureHeight = 1.0f; // 椅子稍高一些
        furniture.Occupancy.providesSurface = false; // 椅子不提供可用表面（有人坐着）
        
        // 设置外观
        var renderer = chair.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.blue;
            renderer.material = material;
        }
        
        // 调整尺寸 (适合XY平面的2D游戏)
        chair.transform.localScale = new Vector3(
            furniture.Occupancy.size.x * gridSystem.Settings.gridSize.x * 0.8f,
            furniture.Occupancy.furnitureHeight,
            0.8f  // Z轴深度保持较小
        );
    }
    
    private GameObject CreateFurnitureItem(string name, Vector2Int gridPosition)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        
        // 添加家具组件
        var furniture = go.AddComponent<FurnitureItem>();
        furniture.GridSystem = gridSystem;
        
        // 设置位置
        Vector3 worldPosition = gridSystem.GridToWorld(gridPosition);
        go.transform.position = worldPosition;
        
        return go;
    }
    
    /// <summary>
    /// 检查指定网格位置是否可以放置家具
    /// </summary>
    public bool CanPlaceFurnitureAt(Vector2Int gridPosition, Vector2Int size)
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
        
        // 检查是否与现有家具重叠
        var allFurniture = FindObjectsOfType<FurnitureItem>();
        foreach (var furniture in allFurniture)
        {
            var occupiedPositions = furniture.GetOccupiedGridPositions();
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
        
        return true;
    }
}
