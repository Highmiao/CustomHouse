using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class WallMountConfig
{
    [Header("Wall Mount Settings")]
    public Vector2Int gridSize = Vector2Int.one; // 在墙面上占据的网格大小
    public Vector2Int gridOffset = Vector2Int.zero; // 相对于挂载点的偏移
    public List<Vector2Int> customShape = new List<Vector2Int>(); // 自定义墙面占据形状
    public bool useCustomShape = false;
    
    [Header("Mount Position")]
    public int mountHeightLevel = 1; // 挂载的高度层级（0=地面层，1=第一层等）
    public float heightOffset = 0f; // 在该层级内的高度偏移
    
    [Header("Wall Direction Filter")]
    public bool canMountOnNorth = true;
    public bool canMountOnEast = true;
    public bool canMountOnSouth = true;
    public bool canMountOnWest = true;
    
    [Header("Surface Provision")]
    public bool providesSurface = false; // 是否提供表面（如挂壁架子）
    public Vector2Int surfaceSize = Vector2Int.one; // 提供的表面大小
    public Vector2Int surfaceOffset = Vector2Int.zero; // 表面相对于挂载点的偏移
    public float surfaceProtrusion = 0.5f; // 表面向外突出的距离
    
    [Header("Visualization")]
    public Color mountColor = new Color(1f, 0.5f, 0f, 0.7f); // 挂载区域颜色（橙色）
    public Color surfaceColor = new Color(0f, 1f, 0.5f, 0.5f); // 表面颜色（青绿色）
    public bool showInSceneView = true;
    public bool showSurfaceInSceneView = true;
}

public class WallMountableItem : MonoBehaviour
{
    [SerializeField] private WallMountConfig mountConfig = new WallMountConfig();
    [SerializeField] private GridVisualization gridSystem;
    [SerializeField] private Vector2Int mountPosition = Vector2Int.zero; // 在网格中的挂载位置
    [SerializeField] private WallDirection mountDirection = WallDirection.North; // 挂载的墙面方向
    
    public WallMountConfig MountConfig => mountConfig;
    public Vector2Int MountPosition 
    { 
        get => mountPosition; 
        set => mountPosition = value; 
    }
    public WallDirection MountDirection 
    { 
        get => mountDirection; 
        set => mountDirection = value; 
    }
    
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
    
    void Start()
    {
        if (GridSystem == null)
        {
            gridSystem = FindObjectOfType<GridVisualization>();
        }
        
        // 自动对齐到最近的墙面
        if (GridSystem != null)
        {
            AlignToNearestWall();
        }
    }
    
    /// <summary>
    /// 检查当前位置是否有效（是否有合适的墙面可以挂载）
    /// </summary>
    public bool IsValidPosition()
    {
        if (GridSystem == null) return false;
        
        // 检查是否允许挂载在当前方向的墙面上
        if (!CanMountOnDirection(mountDirection)) return false;
        
        // 查找该位置的墙面
        var wallItem = FindWallAtPosition(mountPosition, mountDirection);
        if (wallItem == null) return false;
        
        // 检查墙面是否提供墙面表面
        if (!wallItem.WallConfig.providesWallSurface) return false;
        
        // 检查高度是否在墙面范围内
        if (mountConfig.mountHeightLevel >= wallItem.WallConfig.wallHeight) return false;
        
        // 检查占据的网格位置是否都在墙面范围内
        var occupiedPositions = GetWallOccupiedGridPositions();
        var wallSurfacePositions = GetWallSurfacePositions(wallItem);
        
        foreach (var pos in occupiedPositions)
        {
            if (!wallSurfacePositions.Any(wallPos => 
                Vector2Int.RoundToInt(new Vector2(wallPos.x, wallPos.z)) == pos))
            {
                return false;
            }
        }
        
        // 检查是否与其他墙面挂载物冲突
        return !CheckCollisionWithOtherWallItems();
    }
    
    /// <summary>
    /// 获取挂载物在墙面上占据的网格位置
    /// </summary>
    public List<Vector2Int> GetWallOccupiedGridPositions()
    {
        var positions = new List<Vector2Int>();
        
        if (mountConfig.useCustomShape && mountConfig.customShape.Count > 0)
        {
            // 使用自定义形状
            foreach (var shapeOffset in mountConfig.customShape)
            {
                positions.Add(mountPosition + mountConfig.gridOffset + shapeOffset);
            }
        }
        else
        {
            // 使用矩形大小
            for (int x = 0; x < mountConfig.gridSize.x; x++)
            {
                for (int y = 0; y < mountConfig.gridSize.y; y++)
                {
                    positions.Add(mountPosition + mountConfig.gridOffset + new Vector2Int(x, y));
                }
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// 获取挂载物提供的表面位置（如果有的话）
    /// </summary>
    public List<Vector2Int> GetProvidedSurfacePositions()
    {
        var positions = new List<Vector2Int>();
        
        if (!mountConfig.providesSurface) return positions;
        
        // 计算表面位置（向墙面外侧突出）
        Vector2Int surfaceDirection = GetSurfaceDirection();
        Vector2Int basePosition = mountPosition + mountConfig.surfaceOffset;
        
        for (int x = 0; x < mountConfig.surfaceSize.x; x++)
        {
            for (int y = 0; y < mountConfig.surfaceSize.y; y++)
            {
                Vector2Int surfacePos = basePosition + new Vector2Int(x, y) + surfaceDirection;
                positions.Add(surfacePos);
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// 获取表面突出的方向
    /// </summary>
    private Vector2Int GetSurfaceDirection()
    {
        switch (mountDirection)
        {
            case WallDirection.North: return Vector2Int.down; // 北墙表面向南突出
            case WallDirection.East: return Vector2Int.left;  // 东墙表面向西突出
            case WallDirection.South: return Vector2Int.up;   // 南墙表面向北突出
            case WallDirection.West: return Vector2Int.right; // 西墙表面向东突出
            default: return Vector2Int.zero;
        }
    }
    
    /// <summary>
    /// 检查是否可以挂载在指定方向的墙面上
    /// </summary>
    private bool CanMountOnDirection(WallDirection direction)
    {
        switch (direction)
        {
            case WallDirection.North: return mountConfig.canMountOnNorth;
            case WallDirection.East: return mountConfig.canMountOnEast;
            case WallDirection.South: return mountConfig.canMountOnSouth;
            case WallDirection.West: return mountConfig.canMountOnWest;
            default: return false;
        }
    }
    
    /// <summary>
    /// 在指定位置和方向查找墙面
    /// </summary>
    private WallItem FindWallAtPosition(Vector2Int gridPos, WallDirection direction)
    {
        var walls = FindObjectsOfType<WallItem>();
        
        foreach (var wall in walls)
        {
            if (wall.WallConfig.direction == direction)
            {
                var wallPositions = wall.GetBaseOccupiedGridPositions();
                if (wallPositions.Contains(gridPos))
                {
                    return wall;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取墙面的表面位置
    /// </summary>
    private List<Vector3> GetWallSurfacePositions(WallItem wall)
    {
        var positions = new List<Vector3>();
        
        if (!wall.WallConfig.providesWallSurface) return positions;
        
        var basePositions = wall.GetBaseOccupiedGridPositions();
        
        foreach (var basePos in basePositions)
        {
            for (int h = 0; h < wall.WallConfig.wallHeight; h++)
            {
                float height = h * GridSystem.Settings.heightPerLevel;
                Vector3 worldPos = GridSystem.GridToWorld(basePos, height);
                positions.Add(worldPos);
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// 检查是否与其他墙面挂载物冲突
    /// </summary>
    private bool CheckCollisionWithOtherWallItems()
    {
        var otherWallItems = FindObjectsOfType<WallMountableItem>()
            .Where(item => item != this && item.mountDirection == mountDirection);
        
        var myPositions = GetWallOccupiedGridPositions();
        
        foreach (var other in otherWallItems)
        {
            var otherPositions = other.GetWallOccupiedGridPositions();
            
            // 检查是否在同一高度层级
            if (other.mountConfig.mountHeightLevel == mountConfig.mountHeightLevel)
            {
                // 检查网格位置是否重叠
                if (myPositions.Any(pos => otherPositions.Contains(pos)))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 自动对齐到最近的墙面
    /// </summary>
    private void AlignToNearestWall()
    {
        if (GridSystem == null) return;
        
        // 将世界坐标转换为网格坐标
        Vector2Int gridPos = GridSystem.WorldToGridIgnoreHeight(transform.position);
        mountPosition = gridPos;
        
        // 查找最近的墙面
        var nearestWall = FindNearestWall(gridPos);
        if (nearestWall != null)
        {
            mountDirection = nearestWall.WallConfig.direction;
            
            // 更新世界位置以对齐到网格
            float height = GridSystem.GetHeightForLevel(mountConfig.mountHeightLevel) + mountConfig.heightOffset;
            Vector3 worldPos = GridSystem.GridToWorld(mountPosition, height);
            transform.position = worldPos;
        }
    }
    
    /// <summary>
    /// 查找最近的墙面
    /// </summary>
    private WallItem FindNearestWall(Vector2Int gridPos)
    {
        var walls = FindObjectsOfType<WallItem>();
        WallItem nearestWall = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var wall in walls)
        {
            if (!wall.WallConfig.providesWallSurface) continue;
            if (!CanMountOnDirection(wall.WallConfig.direction)) continue;
            
            var wallPositions = wall.GetBaseOccupiedGridPositions();
            
            foreach (var wallPos in wallPositions)
            {
                float distance = Vector2Int.Distance(gridPos, wallPos);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestWall = wall;
                }
            }
        }
        
        return nearestWall;
    }
    
    /// <summary>
    /// 手动设置挂载位置和方向
    /// </summary>
    public void SetMountPosition(Vector2Int gridPos, WallDirection direction)
    {
        mountPosition = gridPos;
        mountDirection = direction;
        
        if (GridSystem != null)
        {
            float height = GridSystem.GetHeightForLevel(mountConfig.mountHeightLevel) + mountConfig.heightOffset;
            Vector3 worldPos = GridSystem.GridToWorld(mountPosition, height);
            transform.position = worldPos;
        }
    }
    
    /// <summary>
    /// 获取挂载物的世界高度
    /// </summary>
    public float GetMountWorldHeight()
    {
        if (GridSystem == null) return 0f;
        return GridSystem.GetHeightForLevel(mountConfig.mountHeightLevel) + mountConfig.heightOffset;
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!mountConfig.showInSceneView || GridSystem == null) return;
        
        // 绘制挂载区域
        DrawMountArea();
        
        // 绘制提供的表面区域
        if (mountConfig.providesSurface && mountConfig.showSurfaceInSceneView)
        {
            DrawProvidedSurface();
        }
        
        // 绘制连接线
        DrawMountConnection();
    }
    
    private void DrawMountArea()
    {
        Color originalColor = Gizmos.color;
        Gizmos.color = mountConfig.mountColor;
        
        var positions = GetWallOccupiedGridPositions();
        float height = GetMountWorldHeight();
        
        foreach (var gridPos in positions)
        {
            Vector3 worldPos = GridSystem.GridToWorld(gridPos, height);
            Gizmos.DrawWireCube(worldPos + Vector3.up * 0.1f, new Vector3(0.8f, 0.2f, 0.8f));
        }
        
        Gizmos.color = originalColor;
    }
    
    private void DrawProvidedSurface()
    {
        Color originalColor = Gizmos.color;
        Gizmos.color = mountConfig.surfaceColor;
        
        var positions = GetProvidedSurfacePositions();
        float height = GetMountWorldHeight() + mountConfig.surfaceProtrusion;
        
        foreach (var gridPos in positions)
        {
            Vector3 worldPos = GridSystem.GridToWorld(gridPos, height);
            Gizmos.DrawWireCube(worldPos + Vector3.up * 0.2f, new Vector3(0.9f, 0.1f, 0.9f));
        }
        
        Gizmos.color = originalColor;
    }
    
    private void DrawMountConnection()
    {
        Color originalColor = Gizmos.color;
        Gizmos.color = Color.yellow;
        
        Vector3 itemPos = transform.position;
        Vector3 mountPos = GridSystem.GridToWorld(mountPosition, GetMountWorldHeight());
        
        Gizmos.DrawLine(itemPos, mountPos);
        Gizmos.DrawWireSphere(mountPos, 0.1f);
        
        Gizmos.color = originalColor;
    }
#endif
}
