using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class GridSettings
{
    [Header("Grid Settings")]
    public Vector2 gridSize = new Vector2(1f, 0.5f);  // XY平面等距视角的典型比例
    public Vector2Int gridDimensions = new Vector2Int(20, 20);
    public Vector3 gridOrigin = Vector3.zero;
    
    [Header("Height Settings")]
    public float heightPerLevel = 1f;  // 每层的高度
    public int maxLevels = 5;  // 最大显示层数
    public bool showAllLevels = true;  // 是否显示所有层级
    
    [Header("Wall Settings")]
    public bool showWallGrids = true;  // 是否显示墙面网格
    public float wallGridSize = 1f;  // 墙面网格的大小
    public int maxWallHeight = 5;  // 墙面最大高度（格子数）
    
    [Header("Isometric Settings")]
    public bool isIsometric = true;
    public float isometricAngle = 30f; // 等距视角的角度
    
    [Header("Visualization")]
    public Color gridColor = Color.white;
    public Color occupiedColor = Color.red;
    public Color surfaceColor = Color.green;  // 上层表面颜色
    public Color wallColor = Color.cyan;  // 墙面颜色
    public float gridLineWidth = 0.05f;
    public float occupiedAlpha = 0.3f;
    public float surfaceAlpha = 0.2f;  // 上层表面透明度
    public float wallAlpha = 0.25f;  // 墙面透明度
}

public class GridVisualization : MonoBehaviour
{
    [SerializeField] private GridSettings gridSettings = new GridSettings();
    
    [Header("Grid Display Control")]
    [SerializeField] private bool showGridSystem = true;  // 主控制开关
    
    public GridSettings Settings => gridSettings;
    public bool ShowGridSystem => showGridSystem;  // 公开访问接口
    
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
    /// 将世界坐标转换为网格坐标
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - gridSettings.gridOrigin;
        
        if (gridSettings.isIsometric)
        {
            // 等距视角坐标转换 (XY平面)
            // 在等距视角中，我们需要从包含投影的Y坐标中分离出真实高度
            // 首先计算基础的等距投影Y值
            float projectedY = localPos.y;
            
            // 逆变换来获取网格坐标
            float gridX = (localPos.x / gridSettings.gridSize.x + projectedY / gridSettings.gridSize.y);
            float gridY = (projectedY / gridSettings.gridSize.y - localPos.x / gridSettings.gridSize.x);
            return new Vector2Int(Mathf.FloorToInt(gridX), Mathf.FloorToInt(gridY));
        }
        else
        {
            // 正交坐标转换 (XY平面)
            float x = localPos.x / gridSettings.gridSize.x;
            float y = localPos.y / gridSettings.gridSize.y;
            return new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
        }
    }
    
    /// <summary>
    /// 将世界坐标转换为网格坐标，忽略高度偏移
    /// </summary>
    public Vector2Int WorldToGridIgnoreHeight(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - gridSettings.gridOrigin;
        
        if (gridSettings.isIsometric)
        {
            // 对于等距视角，我们需要先移除等距投影的影响
            // 假设物体在地面层（height = 0），计算它应该在的网格位置
            float gridX = (localPos.x / gridSettings.gridSize.x + localPos.y / gridSettings.gridSize.y);
            float gridY = (localPos.y / gridSettings.gridSize.y - localPos.x / gridSettings.gridSize.x);
            return new Vector2Int(Mathf.FloorToInt(gridX), Mathf.FloorToInt(gridY));
        }
        else
        {
            // 正交坐标转换，忽略Y坐标
            float x = localPos.x / gridSettings.gridSize.x;
            return new Vector2Int(Mathf.FloorToInt(x), 0);
        }
    }
    
    /// <summary>
    /// 将网格坐标转换为世界坐标（无高度版本，向后兼容）
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return GridToWorld(gridPosition, 0f);
    }
    
    /// <summary>
    /// 将网格坐标转换为世界坐标
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPosition, float height = 0f)
    {
        if (gridSettings.isIsometric)
        {
            // 等距视角坐标转换 (XY平面)
            // 标准等距变换：
            // worldX = (gridX - gridY) * gridSize.x * 0.5
            // worldY = (gridX + gridY) * gridSize.y * 0.5
            float worldX = (gridPosition.x - gridPosition.y) * gridSettings.gridSize.x * 0.5f;
            float worldY = (gridPosition.x + gridPosition.y) * gridSettings.gridSize.y * 0.5f + height;
            return gridSettings.gridOrigin + new Vector3(worldX, worldY, 0);
        }
        else
        {
            // 正交坐标转换 (XY平面)
            float worldX = gridPosition.x * gridSettings.gridSize.x;
            float worldY = gridPosition.y * gridSettings.gridSize.y + height;
            return gridSettings.gridOrigin + new Vector3(worldX, worldY, 0);
        }
    }
    
    /// <summary>
    /// 获取指定高度层的世界Y坐标
    /// </summary>
    public float GetHeightForLevel(int level)
    {
        return level * gridSettings.heightPerLevel;
    }
    
    /// <summary>
    /// 将世界高度转换为层级
    /// </summary>
    public int WorldHeightToLevel(float worldHeight)
    {
        return Mathf.FloorToInt((worldHeight - gridSettings.gridOrigin.y) / gridSettings.heightPerLevel);
    }
    
    /// <summary>
    /// 获取墙面网格在指定方向上的世界坐标
    /// </summary>
    public Vector3 GetWallGridPosition(Vector2Int baseGridPos, int wallHeightLevel, Vector2Int direction)
    {
        // 获取基础世界位置
        Vector3 baseWorldPos = GridToWorld(baseGridPos, 0f);
        
        // 计算垂直高度（墙面格子的Y坐标）
        float verticalHeight = wallHeightLevel * gridSettings.wallGridSize;
        
        // 根据方向计算墙面位置
        Vector3 wallOffset = Vector3.zero;
        
        if (gridSettings.isIsometric)
        {
            // 等距视角下的墙面偏移
            if (direction == Vector2Int.right) // 东墙面
            {
                wallOffset = new Vector3(gridSettings.gridSize.x * 0.5f, verticalHeight, 0);
            }
            else if (direction == Vector2Int.up) // 北墙面
            {
                wallOffset = new Vector3(0, verticalHeight, gridSettings.gridSize.y * 0.5f);
            }
            else if (direction == Vector2Int.left) // 西墙面
            {
                wallOffset = new Vector3(-gridSettings.gridSize.x * 0.5f, verticalHeight, 0);
            }
            else if (direction == Vector2Int.down) // 南墙面
            {
                wallOffset = new Vector3(0, verticalHeight, -gridSettings.gridSize.y * 0.5f);
            }
        }
        else
        {
            // 正交模式下的墙面偏移
            if (direction == Vector2Int.right) // 东墙面
            {
                wallOffset = new Vector3(gridSettings.gridSize.x * 0.5f, verticalHeight, 0);
            }
            else if (direction == Vector2Int.up) // 北墙面
            {
                wallOffset = new Vector3(0, verticalHeight, gridSettings.gridSize.y * 0.5f);
            }
            else if (direction == Vector2Int.left) // 西墙面
            {
                wallOffset = new Vector3(-gridSettings.gridSize.x * 0.5f, verticalHeight, 0);
            }
            else if (direction == Vector2Int.down) // 南墙面
            {
                wallOffset = new Vector3(0, verticalHeight, -gridSettings.gridSize.y * 0.5f);
            }
        }
        
        return baseWorldPos + wallOffset;
    }
    
    /// <summary>
    /// 获取墙面格子的四个顶点（平行四边形）
    /// </summary>
    public Vector3[] GetWallGridCellCorners(Vector2Int baseGridPos, int wallHeightLevel, Vector2Int direction)
    {
        // 获取基础世界位置
        Vector3 baseWorldPos = GridToWorld(baseGridPos, 0f);
        
        // 计算垂直高度
        float verticalHeight = wallHeightLevel * gridSettings.wallGridSize;
        float nextVerticalHeight = (wallHeightLevel + 1) * gridSettings.wallGridSize;
        
        Vector3[] corners = new Vector3[4];
        
        if (gridSettings.isIsometric)
        {
            // 等距视角下的墙面格子
            // 地面菱形的两个基础边向量（在等距投影后的世界坐标系中）
            // 边A: 从 (gridX, gridY) 到 (gridX+1, gridY) 的投影 - 右上方向
            Vector3 edgeRight = new Vector3(gridSettings.gridSize.x * 0.5f, gridSettings.gridSize.y * 0.5f, 0);
            // 边B: 从 (gridX, gridY) 到 (gridX, gridY+1) 的投影 - 左上方向
            Vector3 edgeUp = new Vector3(-gridSettings.gridSize.x * 0.5f, gridSettings.gridSize.y * 0.5f, 0);
            
            Vector3 wallBottom, wallTop;
            Vector3 horizontalEdge;
            
            // direction现在表示墙面延展的方向
            if (direction == Vector2Int.right) // 墙面向右延展
            {
                // 墙面沿X+方向延展，使用edgeRight作为水平边
                wallBottom = baseWorldPos + new Vector3(0, verticalHeight, 0);
                horizontalEdge = edgeRight;
            }
            else if (direction == Vector2Int.left) // 墙面向左延展  
            {
                // 墙面沿X-方向延展，使用-edgeRight作为水平边
                wallBottom = baseWorldPos + new Vector3(0, verticalHeight, 0);
                horizontalEdge = -edgeRight;
            }
            else if (direction == Vector2Int.up) // 墙面向上延展
            {
                // 墙面沿Y+方向延展，使用edgeUp作为水平边
                wallBottom = baseWorldPos + new Vector3(0, verticalHeight, 0);
                horizontalEdge = edgeUp;
            }
            else // direction == Vector2Int.down，墙面向下延展
            {
                // 墙面沿Y-方向延展，使用-edgeUp作为水平边
                wallBottom = baseWorldPos + new Vector3(0, verticalHeight, 0);
                horizontalEdge = -edgeUp;
            }
            
            wallTop = wallBottom + new Vector3(0, gridSettings.wallGridSize, 0);
            
            corners[0] = wallBottom; // 左下
            corners[1] = wallBottom + horizontalEdge; // 右下
            corners[2] = wallTop + horizontalEdge;    // 右上
            corners[3] = wallTop; // 左上
        }
        else
        {
            // 正交模式下简化为正方形
            Vector3 wallPos = GetWallGridPosition(baseGridPos, wallHeightLevel, direction);
            float cellSize = gridSettings.wallGridSize;
            
            corners[0] = wallPos + new Vector3(-cellSize * 0.5f, -cellSize * 0.5f, 0);
            corners[1] = wallPos + new Vector3(cellSize * 0.5f, -cellSize * 0.5f, 0);
            corners[2] = wallPos + new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0);
            corners[3] = wallPos + new Vector3(-cellSize * 0.5f, cellSize * 0.5f, 0);
        }
        
        return corners;
    }
    
    /// <summary>
    /// 检查墙面网格坐标是否有效
    /// </summary>
    public bool IsValidWallGridPosition(Vector2Int baseGridPos, int wallHeight)
    {
        return IsValidGridPosition(baseGridPos) && wallHeight >= 0 && wallHeight < gridSettings.maxWallHeight;
    }
    
    /// <summary>
    /// 检查网格坐标是否在有效范围内
    /// </summary>
    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridSettings.gridDimensions.x &&
               gridPosition.y >= 0 && gridPosition.y < gridSettings.gridDimensions.y;
    }
}
