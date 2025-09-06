using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class GridOccupancy
{
    [Header("Occupancy Settings")]
    public Vector2Int size = Vector2Int.one; // 物体占据的网格大小
    public Vector2Int offset = Vector2Int.zero; // 相对于物体位置的偏移
    public List<Vector2Int> customShape = new List<Vector2Int>(); // 自定义形状（相对坐标）
    public bool useCustomShape = false;
    
    [Header("Height Settings")]
    public float baseHeight = 0f; // 家具的基础高度（相对于地面）
    public float furnitureHeight = 1f; // 家具的物理高度（世界单位）
    public bool providesSurface = true; // 是否在顶部提供可用表面
    public Vector2Int surfaceSize = Vector2Int.one; // 顶部表面提供的网格大小
    public Vector2Int surfaceOffset = Vector2Int.zero; // 表面相对于物体位置的偏移
    public List<Vector2Int> customSurfaceShape = new List<Vector2Int>(); // 自定义表面形状
    public bool useCustomSurfaceShape = false;
    
    [Header("Visualization")]
    public Color occupiedColor = Color.red; // 占据区域颜色
    public Color surfaceColor = Color.green; // 表面区域颜色
    public bool showInSceneView = true;
    public bool showSurfaceInSceneView = true; // 是否显示表面
}

public class FurnitureItem : MonoBehaviour
{
    [SerializeField] private GridOccupancy occupancy = new GridOccupancy();
    [SerializeField] private GridVisualization gridSystem;
    
    public GridOccupancy Occupancy => occupancy;
    public GridVisualization GridSystem 
    { 
        get
        {
#if UNITY_EDITOR
            if (gridSystem == null)
                gridSystem = FindObjectOfType<GridVisualization>();
#endif
            return gridSystem;
        }
        set => gridSystem = value;
    }
    
#if UNITY_EDITOR
    // 这个组件仅在编辑器模式下使用
    void Awake()
    {
        if (Application.isPlaying)
        {
            // 在运行时禁用此组件
            this.enabled = false;
        }
    }
#endif
    
    /// <summary>
    /// 获取当前物体占据的所有网格坐标
    /// </summary>
    public List<Vector2Int> GetOccupiedGridPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
#if UNITY_EDITOR
        if (GridSystem == null)
            return positions;
            
        // 使用忽略高度的方法来获取正确的网格位置
        Vector2Int baseGridPos = GridSystem.WorldToGridIgnoreHeight(transform.position);
        baseGridPos += occupancy.offset;
        
        if (occupancy.useCustomShape && occupancy.customShape.Count > 0)
        {
            // 使用自定义形状
            foreach (var shapeOffset in occupancy.customShape)
            {
                Vector2Int gridPos = baseGridPos + shapeOffset;
                if (GridSystem.IsValidGridPosition(gridPos))
                {
                    positions.Add(gridPos);
                }
            }
        }
        else
        {
            // 使用矩形区域
            for (int x = 0; x < occupancy.size.x; x++)
            {
                for (int y = 0; y < occupancy.size.y; y++)
                {
                    Vector2Int gridPos = baseGridPos + new Vector2Int(x, y);
                    if (GridSystem.IsValidGridPosition(gridPos))
                    {
                        positions.Add(gridPos);
                    }
                }
            }
        }
#endif
        
        return positions;
    }
    
    /// <summary>
    /// 获取家具表面提供的所有网格坐标
    /// </summary>
    public List<Vector2Int> GetSurfaceGridPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
#if UNITY_EDITOR
        if (GridSystem == null || !occupancy.providesSurface)
            return positions;
            
        // 使用忽略高度的方法来获取正确的网格位置
        Vector2Int baseGridPos = GridSystem.WorldToGridIgnoreHeight(transform.position);
        baseGridPos += occupancy.surfaceOffset;
        
        if (occupancy.useCustomSurfaceShape && occupancy.customSurfaceShape.Count > 0)
        {
            // 使用自定义表面形状
            foreach (var shapeOffset in occupancy.customSurfaceShape)
            {
                Vector2Int gridPos = baseGridPos + shapeOffset;
                if (GridSystem.IsValidGridPosition(gridPos))
                {
                    positions.Add(gridPos);
                }
            }
        }
        else
        {
            // 使用矩形表面区域
            for (int x = 0; x < occupancy.surfaceSize.x; x++)
            {
                for (int y = 0; y < occupancy.surfaceSize.y; y++)
                {
                    Vector2Int gridPos = baseGridPos + new Vector2Int(x, y);
                    if (GridSystem.IsValidGridPosition(gridPos))
                    {
                        positions.Add(gridPos);
                    }
                }
            }
        }
#endif
        
        return positions;
    }
    
    /// <summary>
    /// 获取家具的高度层级
    /// </summary>
    public int GetHeightLevel()
    {
#if UNITY_EDITOR
        if (GridSystem == null) return 0;
        // 使用baseHeight而不是transform.position.y来避免等距投影的影响
        return GridSystem.WorldHeightToLevel(GridSystem.Settings.gridOrigin.y + occupancy.baseHeight);
#else
        return 0;
#endif
    }
    
    /// <summary>
    /// 获取表面的高度层级
    /// </summary>
    public int GetSurfaceHeightLevel()
    {
#if UNITY_EDITOR
        if (GridSystem == null) return 0;
        // 表面高度 = 基础高度 + 家具高度
        float surfaceHeight = occupancy.baseHeight + occupancy.furnitureHeight;
        return GridSystem.WorldHeightToLevel(GridSystem.Settings.gridOrigin.y + surfaceHeight);
#else
        return 0;
#endif
    }
    
    /// <summary>
    /// 检查当前位置是否有效（没有与其他物体重叠）
    /// </summary>
    public bool IsValidPosition()
    {
#if UNITY_EDITOR
        var myPositions = GetOccupiedGridPositions();
        var allFurniture = FindObjectsOfType<FurnitureItem>();
        
        foreach (var furniture in allFurniture)
        {
            if (furniture == this) continue;
            
            var otherPositions = furniture.GetOccupiedGridPositions();
            foreach (var pos in myPositions)
            {
                if (otherPositions.Contains(pos))
                    return false;
            }
        }
        
        return true;
#else
        return true;  // 运行时默认返回true
#endif
    }
    
#if UNITY_EDITOR
    void OnValidate()
    {
        // 确保尺寸至少为1
        occupancy.size.x = Mathf.Max(1, occupancy.size.x);
        occupancy.size.y = Mathf.Max(1, occupancy.size.y);
        
        // 确保表面尺寸至少为1
        occupancy.surfaceSize.x = Mathf.Max(1, occupancy.surfaceSize.x);
        occupancy.surfaceSize.y = Mathf.Max(1, occupancy.surfaceSize.y);
        
        // 确保高度为正值
        occupancy.baseHeight = Mathf.Max(0f, occupancy.baseHeight);
        occupancy.furnitureHeight = Mathf.Max(0.1f, occupancy.furnitureHeight);
    }
#endif
}
