using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SurfaceType
{
    Horizontal, // 水平表面（如架子）
    Vertical    // 垂直表面（如镜子）
}

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
    public SurfaceType surfaceType = SurfaceType.Horizontal; // 表面类型
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
    
    void Update()
    {
        // 在编辑器中，如果 GameObject 位置改变，自动更新挂载位置
#if UNITY_EDITOR
        if (!Application.isPlaying && GridSystem != null)
        {
            UpdateMountPositionFromTransform();
        }
#endif
    }
    
#if UNITY_EDITOR
    void OnValidate()
    {
        // 当组件属性在 Inspector 中被修改时调用
        if (GridSystem != null)
        {
            // 重新计算世界位置
            float height = GridSystem.GetHeightForLevel(mountConfig.mountHeightLevel) + mountConfig.heightOffset;
            Vector3 worldPos = GridSystem.GridToWorld(mountPosition, height);
            
            // 如果不在播放模式，更新 transform 位置
            if (!Application.isPlaying)
            {
                transform.position = worldPos;
            }
        }
    }
#endif
    
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
                Vector2Int wallSpaceOffset = TransformWallSpaceToGridSpace(shapeOffset.x, 0); // 只处理水平偏移
                positions.Add(mountPosition + mountConfig.gridOffset + wallSpaceOffset);
            }
        }
        else
        {
            // 使用矩形大小 - 只处理水平分布，垂直分布通过GetWallHeightLevels处理
            for (int x = 0; x < mountConfig.gridSize.x; x++)
            {
                Vector2Int gridSpacePos = TransformWallSpaceToGridSpace(x, 0);
                positions.Add(mountPosition + mountConfig.gridOffset + gridSpacePos);
            }
        }
        
        return positions;
    }

    /// <summary>
    /// 获取挂载物占用的高度层级数（用于支持垂直方向的gridSize.y）
    /// </summary>
    public int GetWallHeightLevels()
    {
        if (mountConfig.useCustomShape && mountConfig.customShape.Count > 0)
        {
            // 对于自定义形状，计算Y方向的范围
            int minY = mountConfig.customShape.Min(pos => pos.y);
            int maxY = mountConfig.customShape.Max(pos => pos.y);
            return maxY - minY + 1;
        }
        else
        {
            // 使用gridSize.y作为垂直层级数
            return mountConfig.gridSize.y;
        }
    }

    /// <summary>
    /// 将墙面空间坐标转换为网格空间坐标
    /// wallX: 墙面水平方向的偏移
    /// wallY: 墙面垂直方向的偏移（暂时未使用，通过mountHeightLevel处理）
    /// </summary>
    private Vector2Int TransformWallSpaceToGridSpace(int wallX, int wallY)
    {
        switch (mountDirection)
        {
            case WallDirection.North:
                // 北墙：墙面水平方向对应网格Y轴正方向
                return new Vector2Int(0, wallX);
                
            case WallDirection.East:
                // 东墙：墙面水平方向对应网格X轴正方向
                return new Vector2Int(wallX, 0);
                
            case WallDirection.South:
                // 南墙：墙面水平方向对应网格Y轴负方向
                return new Vector2Int(0, -wallX);
                
            case WallDirection.West:
                // 西墙：墙面水平方向对应网格X轴负方向
                return new Vector2Int(-wallX, 0);
                
            default:
                return new Vector2Int(wallX, wallY);
        }
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
                // x: 沿着墙面水平方向（需要空间转换）
                Vector2Int horizontalOffset = TransformWallSpaceToGridSpace(x, 0);
                
                // y: 沿着向墙面内侧突出的方向（深度）
                Vector2Int depthOffset = surfaceDirection * (y + 1);
                
                Vector2Int surfacePos = basePosition + horizontalOffset + depthOffset;
                positions.Add(surfacePos);
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// 获取表面突出的方向（向墙面内侧）
    /// </summary>
    private Vector2Int GetSurfaceDirection()
    {
        switch (mountDirection)
        {
            case WallDirection.North: return Vector2Int.left;   // 北墙表面向北突出（墙面内侧）
            case WallDirection.East: return Vector2Int.up; // 东墙表面向东突出（墙面内侧）
            case WallDirection.South: return Vector2Int.right; // 南墙表面向南突出（墙面内侧）
            case WallDirection.West: return Vector2Int.down;  // 西墙表面向西突出（墙面内侧）
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
    /// 根据 GameObject 的 Transform 位置更新挂载位置
    /// </summary>
    private void UpdateMountPositionFromTransform()
    {
        if (GridSystem == null) return;
        
        // 将世界坐标转换为网格坐标
        Vector2Int newGridPos = GridSystem.WorldToGridIgnoreHeight(transform.position);
        
        // 如果位置发生变化，更新挂载位置
        if (newGridPos != mountPosition)
        {
            var previousPosition = mountPosition;
            mountPosition = newGridPos;
            
            // 尝试找到这个位置最合适的墙面
            var nearestWall = FindNearestWall(mountPosition);
            if (nearestWall != null)
            {
                mountDirection = nearestWall.WallConfig.direction;
            }
            
            // 如果在编辑器中，标记场景为脏状态
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }
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
            
#if UNITY_EDITOR
            // 在编辑器中标记为脏状态
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
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
    // 注释掉OnDrawGizmos，让GridPreviewSystem统一处理绘制
    /*
    void OnDrawGizmos()
    {
        if (!mountConfig.showInSceneView || GridSystem == null) return;
        
        // 确保挂载位置与 GameObject 位置同步
        UpdateMountPositionFromTransform();
        
        // 绘制挂载区域
        DrawMountArea();
        
        // 绘制提供的表面区域
        if (mountConfig.providesSurface && mountConfig.showSurfaceInSceneView)
        {
            DrawProvidedSurface();
        }
        
        // 绘制连接线和调试信息
        DrawDebugInfo();
    }
    */
    
    private void DrawMountArea()
    {
        Color originalColor = Gizmos.color;
        bool isValid = IsValidPosition();
        
        // 设置红色用于挂载区域
        Gizmos.color = isValid ? Color.red : Color.red;
        
        var positions = GetWallOccupiedGridPositions();
        float height = GetMountWorldHeight();
        
        foreach (var gridPos in positions)
        {
            // 使用与GridPreviewSystem相同的墙面网格绘制逻辑
            DrawWallGridOccupancy(gridPos, mountDirection, height);
        }
        
        Gizmos.color = originalColor;
    }

    /// <summary>
    /// 绘制墙面网格占用区域 - 使用与GridPreviewSystem相同的逻辑
    /// </summary>
    private void DrawWallGridOccupancy(Vector2Int gridPos, WallDirection direction, float height)
    {
        // 使用与GridPreviewSystem完全相同的方法 - GetWallSurfaceCornersAtHeight
        Vector3[] corners = GetWallSurfaceCornersAtHeight(gridPos, height, direction);
        if (corners.Length == 4)
        {
            // 绘制线框 - 使用Gizmos模拟Handles.DrawAAPolyLine的效果
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);
            
            // 在网格中心绘制一个小球，帮助调试对齐
            Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;
            Gizmos.DrawWireSphere(center, 0.05f);
        }
    }

    /// <summary>
    /// 获取墙面格子的角点 - 与GridPreviewSystem完全一致
    /// </summary>
    private Vector3[] GetWallSurfaceCornersAtHeight(Vector2Int gridPos, float height, WallDirection wallDirection)
    {
        if (GridSystem == null) return new Vector3[0];
        
        float heightPerLevel = GridSystem.Settings.heightPerLevel;
        
        // 根据墙面方向确定墙面格子的形状
        Vector3 bottomLeft, bottomRight, topLeft, topRight;
        
        switch (wallDirection)
        {
            case WallDirection.North:
            case WallDirection.South:
                // 南北墙：墙面格子的顶边平行于等距投影的南北向边
                bottomLeft = GridSystem.GridToWorld(gridPos, height);
                bottomRight = GridSystem.GridToWorld(gridPos + Vector2Int.up, height);
                topLeft = GridSystem.GridToWorld(gridPos, height + heightPerLevel);
                topRight = GridSystem.GridToWorld(gridPos + Vector2Int.up, height + heightPerLevel);
                break;
                
            case WallDirection.East:
            case WallDirection.West:
                // 东西墙：墙面格子的顶边平行于等距投影的东西向边
                bottomLeft = GridSystem.GridToWorld(gridPos, height);
                bottomRight = GridSystem.GridToWorld(gridPos + Vector2Int.right, height);
                topLeft = GridSystem.GridToWorld(gridPos, height + heightPerLevel);
                topRight = GridSystem.GridToWorld(gridPos + Vector2Int.right, height + heightPerLevel);
                break;
                
            default:
                // 默认情况，使用原来的逻辑
                bottomLeft = GridSystem.GridToWorld(gridPos, height);
                bottomRight = GridSystem.GridToWorld(gridPos + Vector2Int.right, height);
                topLeft = GridSystem.GridToWorld(gridPos, height + heightPerLevel);
                topRight = GridSystem.GridToWorld(gridPos + Vector2Int.right, height + heightPerLevel);
                break;
        }
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }

    private void DrawProvidedSurface()
    {
        if (!mountConfig.providesSurface) return;
        
        Color originalColor = Gizmos.color;
        
        // 设置绿色用于提供的表面
        Gizmos.color = Color.green;
        
        var surfacePositions = GetProvidedSurfacePositions();
        float surfaceHeight = GetMountWorldHeight() + mountConfig.surfaceProtrusion;
        
        foreach (var gridPos in surfacePositions)
        {
            if (mountConfig.surfaceType == SurfaceType.Horizontal)
            {
                // 水平表面 - 使用地面网格绘制逻辑
                DrawFloorGridOccupancy(gridPos, surfaceHeight);
            }
            else
            {
                // 垂直表面 - 使用墙面网格绘制逻辑
                DrawWallGridOccupancy(gridPos, mountDirection, surfaceHeight);
            }
        }
        
        Gizmos.color = originalColor;
    }

    /// <summary>
    /// 绘制地面网格占用区域 - 使用与GridPreviewSystem相同的逻辑
    /// </summary>
    private void DrawFloorGridOccupancy(Vector2Int gridPos, float height)
    {
        // 使用与GridPreviewSystem相同的地面网格绘制逻辑
        Vector3[] corners = GetGridCellCornersAtHeight(gridPos, height);
        if (corners.Length == 4)
        {
            // 绘制线框
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);
        }
    }

    /// <summary>
    /// 获取地面格子的角点 - 与GridPreviewSystem保持一致
    /// </summary>
    private Vector3[] GetGridCellCornersAtHeight(Vector2Int gridPos, float height)
    {
        if (GridSystem == null) return new Vector3[0];
        
        Vector3 bottomLeft = GridSystem.GridToWorld(gridPos, height);
        Vector3 bottomRight = GridSystem.GridToWorld(gridPos + Vector2Int.right, height);
        Vector3 topRight = GridSystem.GridToWorld(gridPos + Vector2Int.one, height);
        Vector3 topLeft = GridSystem.GridToWorld(gridPos + Vector2Int.up, height);
        
        return new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
    }
    
    private void DrawDebugInfo()
    {
        Color originalColor = Gizmos.color;
        
        // 绘制到挂载点的连接线
        Gizmos.color = Color.yellow;
        Vector3 itemPos = transform.position;
        Vector3 mountPos = GridSystem.GridToWorld(mountPosition, GetMountWorldHeight());
        
        Gizmos.DrawLine(itemPos, mountPos);
        Gizmos.DrawWireSphere(mountPos, 0.1f);
        
        // 绘制方向指示器
        Gizmos.color = Color.blue;
        Vector3 directionOffset = GetDirectionVector() * 0.5f;
        Gizmos.DrawRay(mountPos, directionOffset);
        
        // 绘制网格坐标文本（仅在选中时）
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            var style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            
            Vector3 labelPos = mountPos + Vector3.up * 0.5f;
            string info = $"Mount: ({mountPosition.x}, {mountPosition.y})\\n" +
                         $"Direction: {mountDirection}\\n" +
                         $"Height: L{mountConfig.mountHeightLevel}\\n" +
                         $"Valid: {(IsValidPosition() ? "✓" : "✗")}";
            
            UnityEditor.Handles.Label(labelPos, info, style);
        }
        
        Gizmos.color = originalColor;
    }
    
    /// <summary>
    /// 根据墙面方向获取 Gizmo 的大小
    /// </summary>
    private Vector3 GetGizmoSizeForWallDirection()
    {
        switch (mountDirection)
        {
            case WallDirection.North:
            case WallDirection.South:
                return new Vector3(0.8f, 0.2f, 0.1f); // 东西方向较宽
            case WallDirection.East:
            case WallDirection.West:
                return new Vector3(0.1f, 0.2f, 0.8f); // 南北方向较宽
            default:
                return new Vector3(0.8f, 0.2f, 0.8f);
        }
    }
    
    /// <summary>
    /// 获取方向向量
    /// </summary>
    private Vector3 GetDirectionVector()
    {
        switch (mountDirection)
        {
            case WallDirection.North: return Vector3.forward;
            case WallDirection.East: return Vector3.right;
            case WallDirection.South: return Vector3.back;
            case WallDirection.West: return Vector3.left;
            default: return Vector3.forward;
        }
    }
#endif
}
