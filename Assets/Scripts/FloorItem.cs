using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class FloorConfig
{
    [Header("Floor Settings")]
    public Vector2Int size = Vector2Int.one; // 地面占据的网格大小
    public Vector2Int offset = Vector2Int.zero; // 相对于物体位置的偏移
    public List<Vector2Int> customShape = new List<Vector2Int>(); // 自定义形状（相对坐标）
    public bool useCustomShape = false;
    
    [Header("Height Settings")]
    public float floorHeight = 0f; // 地面的高度（世界单位）
    public float thickness = 0.2f; // 地面的厚度
    
    [Header("Surface Settings")]
    public bool providesSurface = true; // 是否提供可用表面（通常为true）
    public Vector2Int surfaceSize = Vector2Int.one; // 表面提供的网格大小
    public Vector2Int surfaceOffset = Vector2Int.zero; // 表面相对于地面位置的偏移
    public List<Vector2Int> customSurfaceShape = new List<Vector2Int>(); // 自定义表面形状
    public bool useCustomSurfaceShape = false;
    
    [Header("Visualization")]
    public Color floorColor = new Color(0.6f, 0.4f, 0.2f, 0.7f); // 地面颜色（棕色）
    public Color surfaceColor = new Color(0.2f, 0.8f, 0.2f, 0.5f); // 表面颜色（绿色）
    public bool showInSceneView = true;
    public bool showSurfaceInSceneView = true; // 是否显示表面
}

public class FloorItem : MonoBehaviour
{
    [SerializeField] private FloorConfig floorConfig = new FloorConfig();
    [SerializeField] private GridVisualization gridSystem;
    
    public FloorConfig FloorConfig => floorConfig;
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
    /// 获取地面占据的所有网格坐标（地面本身的位置）
    /// </summary>
    public List<Vector2Int> GetFloorGridPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
#if UNITY_EDITOR
        if (GridSystem == null)
            return positions;
            
        // 使用忽略高度的方法来获取正确的网格位置
        Vector2Int baseGridPos = GridSystem.WorldToGridIgnoreHeight(transform.position);
        baseGridPos += floorConfig.offset;
        
        if (floorConfig.useCustomShape && floorConfig.customShape.Count > 0)
        {
            // 使用自定义形状
            foreach (var shapeOffset in floorConfig.customShape)
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
            for (int x = 0; x < floorConfig.size.x; x++)
            {
                for (int y = 0; y < floorConfig.size.y; y++)
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
    /// 获取地面表面提供的所有网格坐标
    /// </summary>
    public List<Vector2Int> GetSurfaceGridPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
#if UNITY_EDITOR
        if (GridSystem == null || !floorConfig.providesSurface)
            return positions;
            
        // 使用忽略高度的方法来获取正确的网格位置
        Vector2Int baseGridPos = GridSystem.WorldToGridIgnoreHeight(transform.position);
        baseGridPos += floorConfig.surfaceOffset;
        
        if (floorConfig.useCustomSurfaceShape && floorConfig.customSurfaceShape.Count > 0)
        {
            // 使用自定义表面形状
            foreach (var shapeOffset in floorConfig.customSurfaceShape)
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
            for (int x = 0; x < floorConfig.surfaceSize.x; x++)
            {
                for (int y = 0; y < floorConfig.surfaceSize.y; y++)
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
    /// 获取地面的高度层级
    /// </summary>
    public int GetFloorHeightLevel()
    {
#if UNITY_EDITOR
        if (GridSystem == null) return 0;
        return GridSystem.WorldHeightToLevel(GridSystem.Settings.gridOrigin.y + floorConfig.floorHeight);
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
        // 表面高度 = 地面高度 + 厚度
        float surfaceHeight = floorConfig.floorHeight + floorConfig.thickness;
        return GridSystem.WorldHeightToLevel(GridSystem.Settings.gridOrigin.y + surfaceHeight);
#else
        return 0;
#endif
    }
    
    /// <summary>
    /// 检查当前位置是否有效（没有与其他地面重叠）
    /// </summary>
    public bool IsValidPosition()
    {
#if UNITY_EDITOR
        var myPositions = GetFloorGridPositions();
        var myHeight = GetFloorHeightLevel();
        var allFloors = FindObjectsOfType<FloorItem>();
        
        foreach (var floor in allFloors)
        {
            if (floor == this) continue;
            
            // 检查是否在同一高度层级
            if (floor.GetFloorHeightLevel() == myHeight)
            {
                var otherPositions = floor.GetFloorGridPositions();
                foreach (var pos in myPositions)
                {
                    if (otherPositions.Contains(pos))
                        return false;
                }
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
        floorConfig.size.x = Mathf.Max(1, floorConfig.size.x);
        floorConfig.size.y = Mathf.Max(1, floorConfig.size.y);
        
        // 确保表面尺寸至少为1
        floorConfig.surfaceSize.x = Mathf.Max(1, floorConfig.surfaceSize.x);
        floorConfig.surfaceSize.y = Mathf.Max(1, floorConfig.surfaceSize.y);
        
        // 确保厚度为正值
        floorConfig.thickness = Mathf.Max(0.01f, floorConfig.thickness);
    }
#endif
}
