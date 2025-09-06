using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 墙面方向枚举
/// </summary>
public enum WallDirection
{
    North,  // 向北（在等距视角中向左上）
    East,   // 向东（在等距视角中向右上）
    South,  // 向南（在等距视角中向右下）
    West    // 向西（在等距视角中向左下）
}

[System.Serializable]
public class WallConfiguration
{
    [Header("Wall Settings")]
    public WallDirection direction = WallDirection.North;
    public int wallLength = 3; // 墙的长度（格子数）
    public int wallHeight = 3; // 墙的高度（格子数）
    public Vector2Int baseOffset = Vector2Int.zero; // 基础位置偏移
    
    [Header("Wall Surface")]
    public bool providesWallSurface = true; // 是否提供墙面格子
    public List<Vector2Int> customWallShape = new List<Vector2Int>(); // 自定义墙面形状
    public bool useCustomWallShape = false;
    
    [Header("Visualization")]
    public Color baseOccupiedColor = Color.red; // 地面占用颜色
    public Color wallSurfaceColor = Color.cyan; // 墙面颜色
    public bool showInSceneView = true;
    public bool showWallSurfaceInSceneView = true;
}

public class WallItem : MonoBehaviour
{
    [SerializeField] private WallConfiguration wallConfig = new WallConfiguration();
    [SerializeField] private GridVisualization gridSystem;
    
    public WallConfiguration WallConfig => wallConfig;
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
    /// 获取墙面在地面占据的网格坐标
    /// </summary>
    public List<Vector2Int> GetBaseOccupiedGridPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
#if UNITY_EDITOR
        if (GridSystem == null)
            return positions;
            
        Vector2Int baseGridPos = GridSystem.WorldToGridIgnoreHeight(transform.position);
        baseGridPos += wallConfig.baseOffset;
        
        Vector2Int directionVector = GetDirectionVector();
        
        // 沿着墙的方向生成地面占用格子
        for (int i = 0; i < wallConfig.wallLength; i++)
        {
            Vector2Int gridPos = baseGridPos + (directionVector * i);
            if (GridSystem.IsValidGridPosition(gridPos))
            {
                positions.Add(gridPos);
            }
        }
#endif
        
        return positions;
    }
    
    /// <summary>
    /// 获取墙面提供的网格坐标（墙面上的格子）
    /// </summary>
    public List<Vector3> GetWallSurfacePositions()
    {
        List<Vector3> positions = new List<Vector3>();
        
#if UNITY_EDITOR
        if (GridSystem == null || !wallConfig.providesWallSurface)
            return positions;
            
        var basePositions = GetBaseOccupiedGridPositions();
        Vector2Int directionVector = GetDirectionVector();
        Vector2Int normalVector = GetWallNormalVector();
        
        if (wallConfig.useCustomWallShape && wallConfig.customWallShape.Count > 0)
        {
            // 使用自定义墙面形状
            foreach (var basePos in basePositions)
            {
                foreach (var shapeOffset in wallConfig.customWallShape)
                {
                    for (int h = 0; h < wallConfig.wallHeight; h++)
                    {
                        Vector3 wallPos = GridSystem.GetWallGridPosition(basePos, h, normalVector);
                        positions.Add(wallPos);
                    }
                }
            }
        }
        else
        {
            // 标准矩形墙面
            foreach (var basePos in basePositions)
            {
                for (int h = 0; h < wallConfig.wallHeight; h++)
                {
                    Vector3 wallPos = GridSystem.GetWallGridPosition(basePos, h, normalVector);
                    positions.Add(wallPos);
                }
            }
        }
#endif
        
        return positions;
    }
    
    /// <summary>
    /// 获取墙面格子信息（用于正确绘制平行四边形）
    /// </summary>
    public List<(Vector2Int basePos, int heightLevel, Vector2Int direction)> GetWallGridCellInfos()
    {
        List<(Vector2Int, int, Vector2Int)> infos = new List<(Vector2Int, int, Vector2Int)>();
        
#if UNITY_EDITOR
        if (GridSystem == null || !wallConfig.providesWallSurface)
            return infos;
            
        var basePositions = GetBaseOccupiedGridPositions();
        Vector2Int wallDirection = GetDirectionVector(); // 使用墙面延展方向，而不是法向量
        
        if (wallConfig.useCustomWallShape && wallConfig.customWallShape.Count > 0)
        {
            // 使用自定义墙面形状
            foreach (var basePos in basePositions)
            {
                foreach (var shapeOffset in wallConfig.customWallShape)
                {
                    for (int h = 0; h < wallConfig.wallHeight; h++)
                    {
                        infos.Add((basePos, h, wallDirection));
                    }
                }
            }
        }
        else
        {
            // 标准矩形墙面
            foreach (var basePos in basePositions)
            {
                for (int h = 0; h < wallConfig.wallHeight; h++)
                {
                    infos.Add((basePos, h, wallDirection));
                }
            }
        }
#endif
        
        return infos;
    }
    
    /// <summary>
    /// 获取墙面方向向量
    /// </summary>
    private Vector2Int GetDirectionVector()
    {
        switch (wallConfig.direction)
        {
            case WallDirection.North: return Vector2Int.up;
            case WallDirection.East: return Vector2Int.right;
            case WallDirection.South: return Vector2Int.down;
            case WallDirection.West: return Vector2Int.left;
            default: return Vector2Int.up;
        }
    }
    
    /// <summary>
    /// 获取墙面法向量（垂直于墙面方向）
    /// </summary>
    private Vector2Int GetWallNormalVector()
    {
        switch (wallConfig.direction)
        {
            case WallDirection.North: return Vector2Int.right;
            case WallDirection.East: return Vector2Int.up;
            case WallDirection.South: return Vector2Int.left;
            case WallDirection.West: return Vector2Int.down;
            default: return Vector2Int.right;
        }
    }
    
    /// <summary>
    /// 检查当前位置是否有效（没有与其他物体重叠）
    /// </summary>
    public bool IsValidPosition()
    {
#if UNITY_EDITOR
        var myPositions = GetBaseOccupiedGridPositions();
        var allFurniture = FindObjectsOfType<FurnitureItem>();
        var allWalls = FindObjectsOfType<WallItem>();
        
        // 检查与家具的重叠
        foreach (var furniture in allFurniture)
        {
            var otherPositions = furniture.GetOccupiedGridPositions();
            foreach (var pos in myPositions)
            {
                if (otherPositions.Contains(pos))
                    return false;
            }
        }
        
        // 检查与其他墙面的重叠
        foreach (var wall in allWalls)
        {
            if (wall == this) continue;
            
            var otherPositions = wall.GetBaseOccupiedGridPositions();
            foreach (var pos in myPositions)
            {
                if (otherPositions.Contains(pos))
                    return false;
            }
        }
        
        return true;
#else
        return true;
#endif
    }
    
#if UNITY_EDITOR
    void OnValidate()
    {
        // 确保尺寸至少为1
        wallConfig.wallLength = Mathf.Max(1, wallConfig.wallLength);
        wallConfig.wallHeight = Mathf.Max(1, wallConfig.wallHeight);
    }
#endif
}
