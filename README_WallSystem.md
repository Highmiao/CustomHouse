# Unity 等距视角统一网格可视化系统 - 完整技术文档

这是一个专为Unity编辑器设计的**高性能、统一的等距2D网格可视化和表面高亮系统**，提供完整的地面网格、家具、墙体、地板的可视化解决方案。该系统采用先进的批处理绘制技术，大幅提升编辑器性能。

## 🎯 系统概览

### 技术革新
- **统一批处理绘制**: GL绘制 + 材质共享，性能提升数十倍
- **智能缓存系统**: 组件缓存 + 频率控制，减少系统开销
- **实时表面高亮**: 鼠标悬停智能识别可用表面
- **零运行时开销**: 完全编辑器专用，构建时自动排除

### 支持的可视化类型
- � **基础网格**: 多层级等距网格系统
- � **家具系统**: 占用格子 + 表面格子
- 🧱 **墙体系统**: 地面占用 + 垂直墙面格子
- 🏠 **地板系统**: 地板占用 + 表面格子
- ✨ **表面高亮**: 实时交互式表面识别

## 🏗️ 系统架构详解

### 核心绘制引擎
```
GridPreviewSystem (统一控制中心)
├── 批处理绘制系统
│   ├── GridMaterialManager
│   │   ├── 线条材质管理
│   │   └── 填充材质管理
│   ├── BatchDrawData
│   │   ├── 顶点数据缓存
│   │   ├── 颜色分组
│   │   └── 绘制模式管理
│   └── GL绘制优化
│       ├── GL.Begin(GL.QUADS) 填充
│       └── GL.Begin(GL.LINES) 线条
├── 几何体收集系统
│   ├── CollectGridGeometry() - 基础网格
│   ├── CollectFurnitureGeometry() - 家具几何
│   ├── CollectWallGeometry() - 墙体几何
│   └── CollectFloorGeometry() - 地板几何
├── 表面高亮引擎
│   ├── HandleSurfaceHighlight() - 实时高亮
│   ├── SurfaceInfo结构体 - 表面信息
│   ├── GetSurfacesAtPosition() - 位置查询
│   └── 优先级系统 - 智能选择
└── 性能优化模块
    ├── 组件缓存 (1秒刷新)
    ├── 频率控制 (20fps限制)
    └── 内存管理
```

### 支持的几何体类型

#### 1. 基础网格 (Grid Geometry)
```csharp
// 实现方式
CollectGridGeometry(Dictionary<string, BatchDrawData> batches)
├── AddGridLevelToBatch() - 按层级添加
├── 支持多层级显示 (0-10层可配置)
├── 递减透明度 (高层级更透明)
└── 等距投影正确性保证
```

#### 2. 家具系统 (Furniture System)  
```csharp
// 占用区域
AddFurnitureOccupancyToBatch()
├── 地面占用格子 (Level 0)
├── 冲突检测 (红色警告)
└── 自定义形状支持

// 表面区域  
AddFurnitureSurfaceToBatch()
├── 顶部表面格子 (Level 1+)
├── 高度计算 (baseHeight + furnitureHeight)
└── 可用表面标识
```

#### 3. 墙体系统 (Wall System)
```csharp
// 墙体基座
AddWallOccupancyToBatch()
├── 地面占用 (红色)
├── 四方向支持 (N/E/S/W)
└── 基座位置验证

// 墙面格子
AddWallSurfaceToBatch()
├── 垂直平行四边形绘制
├── 法向量计算 (正确的侧面位置)
├── 多层高度支持 (wallHeight)
└── GetWallSurfaceCornersAtHeight() 专用角点计算
```

#### 4. 地板系统 (Floor System)
```csharp
// 地板占用
AddFloorOccupancyToBatch()
├── 地板本身区域 (棕色)
├── 高度支持 (floorHeight)
└── 自定义形状

// 地板表面
AddFloorSurfaceToBatch()  
├── 可行走表面 (绿色)
├── 厚度计算 (floorHeight + thickness)
└── 表面提供功能
```

## ✨ 表面高亮系统技术详解

### 表面类型定义
```csharp
public enum SurfaceType
{
    Ground,            // 基础地面网格
    Floor,             // 地板表面 (可行走)
    FurnitureSurface,  // 家具表面 (可放置)
    WallSurface        // 墙面表面 (可挂载)
}
```

### 高亮处理流程
```csharp
HandleSurfaceHighlight(SceneView sceneView)
├── 鼠标位置获取
├── 世界坐标转换
├── 网格坐标计算
├── GetSurfacesAtPosition() 表面查询
├── 优先级选择 (家具 > 墙面 > 地板 > 地面)
├── 高亮渲染
└── 信息显示
```

### 性能优化策略
- **频率限制**: 20fps更新，避免过度刷新
- **事件过滤**: 只处理MouseMove/Repaint事件
- **智能缓存**: 避免重复的表面查询
- **延迟更新**: 使用needsRepaint标志

## 🎨 可视化规范

### 颜色编码系统
```csharp
白色网格线     → 基础等距网格 (gridColor)
红色填充      → 占用格子 (occupiedColor) 
绿色填充      → 表面格子 (surfaceColor)
青色填充      → 墙面格子 (wallSurfaceColor)
黄色高亮      → 鼠标悬停高亮 (highlightColor)
递减透明度     → 不同高度层级
```

### 几何体形状规范
```csharp
地面/地板格子   → 水平菱形 (等距投影)
墙面格子      → 垂直平行四边形 (正确墙面投影)
高亮覆盖      → 半透明覆盖层
网格线       → 1像素线条
```

### 信息标签格式
```
格式: {类型} ({x},{y}) L{层级} H:{高度}
示例: 
- 底部占用 (2,3) L0 H:0.0
- 表面提供 (2,3) L1 H:1.0  
- 墙面基础 (1,2) 方向:North
- Surface: FurnitureSurface at (2,3)
```

## ⚡ 性能优化技术

### 批处理绘制优化
```csharp
// 材质状态最小化切换
Dictionary<string, BatchDrawData> batches
├── 按颜色分组几何体
├── 共享材质实例
├── 批量顶点提交
└── 单次GL绘制调用

// 绘制调用优化
GL.Begin(GL.QUADS)   // 填充批次
GL.Begin(GL.LINES)   // 线条批次
```

### 组件缓存策略
```csharp
// 缓存管理
RefreshComponentCache()
├── 时间间隔检查 (1秒)
├── FindObjectsOfType() 缓存
├── 类型转换优化
└── 内存管理

// 缓存数据
cachedFurniture[]  // FurnitureItem缓存
cachedWalls[]      // WallItem缓存 (反射访问)
cachedFloors[]     // FloorItem缓存 (反射访问)
```

### 绘制频率控制
```csharp
// 更新限制
const double UPDATE_INTERVAL = 0.05;  // 20fps
├── lastUpdateTime 时间戳
├── needsRepaint 标志
└── 条件重绘
```

## 🔧 高级配置

### 墙面系统配置
```csharp
// 法向量计算 (墙面位置)
switch (wallDirection)
{
    case North: normalVector = Vector2Int.right;  // 墙面在东侧
    case East:  normalVector = Vector2Int.up;     // 墙面在北侧  
    case South: normalVector = Vector2Int.left;   // 墙面在西侧
    case West:  normalVector = Vector2Int.down;   // 墙面在南侧
}

// 墙面角点计算 (垂直平行四边形)
GetWallSurfaceCornersAtHeight()
├── bottomLeft  = GridToWorld(gridPos, height)
├── bottomRight = GridToWorld(gridPos + right, height)  
├── topRight    = GridToWorld(gridPos + right, height + levelHeight)
└── topLeft     = GridToWorld(gridPos, height + levelHeight)
```

### 层级控制配置
```csharp
// 三级控制系统
1. GridVisualization.showGridSystem        // 全局主开关
   └── 控制整个系统的显示/隐藏

2. Individual.showInSceneView              // 对象级开关
   ├── FurnitureItem.showInSceneView
   ├── WallItem.showInSceneView  
   └── FloorItem.showInSceneView

3. Feature.showSpecificInSceneView         // 功能级开关
   ├── showSurfaceInSceneView
   ├── showWallSurfaceInSceneView
   └── enableSurfaceHighlight
```

## 💻 API参考和示例

### 核心API使用
```csharp
// 获取网格系统
var gridViz = FindObjectOfType<GridVisualization>();

// 坐标转换
Vector2Int gridPos = gridViz.WorldToGrid(transform.position);
Vector3 worldPos = gridViz.GridToWorld(new Vector2Int(5, 3), height);

// 启用统一绘制系统  
GridPreviewSystem.ShowWindow();  // 打开预览窗口
```

### 家具系统API
```csharp
var furniture = GetComponent<FurnitureItem>();

// 获取占用位置
List<Vector2Int> occupied = furniture.GetOccupiedGridPositions();
List<Vector2Int> surfaces = furniture.GetSurfaceGridPositions();

// 位置验证
bool isValid = furniture.IsValidPosition();

// 高度计算
int surfaceLevel = furniture.GetSurfaceHeightLevel();
float surfaceHeight = furniture.Occupancy.baseHeight + furniture.Occupancy.furnitureHeight;
```

### 墙体系统API
```csharp
var wall = GetComponent<WallItem>();

// 墙体信息
List<Vector2Int> wallBase = wall.GetBaseOccupiedGridPositions();
List<Vector3> wallSurfaces = wall.GetWallSurfacePositions();

// 方向和配置
WallDirection direction = wall.WallConfig.direction;
int wallHeight = wall.WallConfig.wallHeight;
bool providesWallSurface = wall.WallConfig.providesWallSurface;
```

### 地板系统API
```csharp
var floor = GetComponent<FloorItem>();

// 地板信息
List<Vector2Int> floorPositions = floor.GetFloorGridPositions();
List<Vector2Int> floorSurfaces = floor.GetSurfaceGridPositions();

// 高度配置
float floorHeight = floor.FloorConfig.floorHeight;
float thickness = floor.FloorConfig.thickness;
bool providesSurface = floor.FloorConfig.providesSurface;
```

### 表面高亮API
```csharp
// 启用高亮系统
GridPreviewSystem.EnableSurfaceHighlight = true;
GridPreviewSystem.SurfaceHighlightColor = Color.yellow;
GridPreviewSystem.SurfaceHighlightAlpha = 0.3f;

// 获取位置的表面信息
List<SurfaceInfo> surfaces = GridPreviewSystem.GetSurfacesAtPosition(gridPos);

foreach (var surface in surfaces)
{
    Debug.Log($"Surface: {surface.type}");
    Debug.Log($"Position: {surface.gridPosition}"); 
    Debug.Log($"Height: {surface.height}");
    Debug.Log($"Object: {surface.objectName}");
}
```

## 🔨 创建和配置示例

### 创建完整房间布局
```csharp
public static void CreateSampleRoom()
{
    // 1. 创建网格系统
    var gridSystem = CreateGridSystem();
    
    // 2. 创建四面墙
    CreateWall(gridSystem, Vector2Int.zero, WallDirection.North, 8);
    CreateWall(gridSystem, new Vector2Int(0, 8), WallDirection.East, 6);
    CreateWall(gridSystem, new Vector2Int(6, 2), WallDirection.South, 8);
    CreateWall(gridSystem, Vector2Int.zero, WallDirection.West, 8);
    
    // 3. 添加地板
    CreateFloor(gridSystem, new Vector2Int(1, 1), new Vector2Int(6, 6));
    
    // 4. 添加家具
    CreateFurniture(gridSystem, new Vector2Int(2, 2), "Table", new Vector2Int(2, 1));
    CreateFurniture(gridSystem, new Vector2Int(1, 3), "Chair", Vector2Int.one);
    CreateFurniture(gridSystem, new Vector2Int(3, 3), "Chair", Vector2Int.one);
    
    Debug.Log("Sample room created!");
}

private static GridVisualization CreateGridSystem()
{
    var go = new GameObject("Grid System");
    var grid = go.AddComponent<GridVisualization>();
    
    // 配置等距网格
    var settings = grid.Settings;
    settings.isIsometric = true;
    settings.gridSize = new Vector2(1f, 0.5f);
    settings.gridDimensions = new Vector2Int(20, 20);
    settings.heightPerLevel = 1.0f;
    
    return grid;
}
```

### 自定义L形沙发
```csharp
public static void CreateLShapedSofa(GridVisualization grid, Vector2Int position)
{
    var sofa = CreateFurniture(grid, position, "L-Shaped Sofa");
    var occupancy = sofa.Occupancy;
    
    // 设置自定义L形状
    occupancy.useCustomShape = true;
    occupancy.customShape = new List<Vector2Int>
    {
        Vector2Int.zero,           // (0,0) 
        Vector2Int.right,          // (1,0)
        Vector2Int.right * 2,      // (2,0)
        Vector2Int.up,             // (0,1)
        new Vector2Int(1, 1)       // (1,1)
    };
    
    // 设置表面
    occupancy.providesSurface = true;
    occupancy.useCustomSurfaceShape = true;
    occupancy.customSurfaceShape = occupancy.customShape; // 使用相同形状
    
    Debug.Log("L-shaped sofa created!");
}
```

### 批量操作工具
```csharp
public static class GridSystemBatchTools
{
    [MenuItem("Tools/Grid System/Batch Operations/Snap All To Grid")]
    public static void SnapAllToGrid()
    {
        var furniture = FindObjectsOfType<FurnitureItem>();
        var walls = FindObjectsOfType<WallItem>();
        var floors = FindObjectsOfType<FloorItem>();
        
        foreach (var item in furniture)
        {
            SnapToGrid(item.transform);
        }
        
        Debug.Log($"Snapped {furniture.Length + walls.Length + floors.Length} objects to grid");
    }
    
    [MenuItem("Tools/Grid System/Batch Operations/Check All Overlaps")]
    public static void CheckAllOverlaps()
    {
        var furniture = FindObjectsOfType<FurnitureItem>();
        var overlapping = new List<FurnitureItem>();
        
        foreach (var item in furniture)
        {
            if (!item.IsValidPosition())
            {
                overlapping.Add(item);
            }
        }
        
        if (overlapping.Count > 0)
        {
            Selection.objects = overlapping.ConvertAll(f => f.gameObject as Object).ToArray();
            Debug.LogWarning($"Found {overlapping.Count} overlapping objects!");
        }
        else
        {
            Debug.Log("No overlaps found. All positions are valid!");
        }
    }
}
```

## ⚠️ 重要技术注意事项

### 编辑器专用设计原则
```csharp
#if UNITY_EDITOR
// 所有可视化代码都在这个宏内
// 确保运行时完全排除，零性能开销
#endif
```

### 坐标系统一致性
- 所有坐标转换必须使用 `GridVisualization` 方法
- 等距投影的Y轴缩放处理 (通常0.5倍)
- 高度计算的一致性 (`heightPerLevel` 统一标准)

### 墙面几何体技术要点
```csharp
// 墙面必须是垂直平行四边形，不是水平菱形
GetWallSurfaceCornersAtHeight(gridPos, height)
├── 从当前高度到下一层级高度
├── 垂直延伸而非水平展开
└── 正确的等距投影形状
```

### 批处理绘制要求
- 顶点必须经过 `IsValidVertex()` 验证
- 颜色分组键必须唯一 (`ColorToString()`)
- 必须在 `EventType.Repaint` 时执行
- GL绘制状态管理要正确

### 性能优化原则
- 避免在循环中调用 `FindObjectsOfType()`
- 使用组件缓存减少反射开销
- 限制绘制频率避免编辑器卡顿
- 批处理几何体减少draw calls

## 🐛 故障排除指南

### 常见问题诊断

#### 1. 网格不显示
```
问题: Scene视图中看不到网格
解决: 
1. 检查 GridVisualization.showGridSystem 状态
2. 使用 Tools → Toggle Grid Preview 重新启用
3. 确认Scene视图获得焦点
4. 检查网格尺寸是否合理
```

#### 2. 墙面位置错误
```
问题: 墙面格子显示在错误位置
解决:
1. 检查法向量计算是否正确
2. 确认 WallDirection 设置
3. 验证 GetWallSurfaceCornersAtHeight() 调用
4. 检查墙面高度设置
```

#### 3. 性能问题
```
问题: 编辑器卡顿或帧率低
解决:
1. 确认使用批处理绘制系统 (新版本)
2. 检查组件缓存是否正常工作
3. 调整 UPDATE_INTERVAL 增加间隔
4. 减少同时显示的层级数量
```

#### 4. 表面高亮不工作
```
问题: 鼠标悬停没有高亮效果
解决:
1. 启用 EnableSurfaceHighlight
2. 检查高亮颜色透明度设置
3. 确认Scene视图鼠标焦点
4. 验证表面类型配置
```

#### 5. 批处理绘制失效
```
问题: 回退到旧的Handles绘制
解决:
1. 检查 DrawBatchedPreview() 是否被调用
2. 确认批处理数据结构正确
3. 验证GL绘制状态
4. 检查材质管理器状态
```

### 调试工具
```csharp
// 启用调试日志
Debug.Log($"Total batches: {batches.Count}");
Debug.Log($"Drawing batch {batchName}: {vertexCount} vertices");

// 检查缓存状态
Debug.Log($"Cached furniture: {cachedFurniture?.Length ?? 0}");
Debug.Log($"Cache refresh time: {lastComponentCacheTime}");

// 验证表面检测
var surfaces = GetSurfacesAtPosition(gridPos);
Debug.Log($"Found {surfaces.Count} surfaces at {gridPos}");
```

## 🚀 未来扩展方向

### 新功能建议
1. **天花板系统**: 支持顶部覆盖区域
2. **斜坡和楼梯**: 非标准高度的几何体
3. **圆形和曲线**: 非矩形的自定义形状
4. **材质纹理**: 表面材质的可视化
5. **光照影响**: 高度对阴影的影响

### 性能优化方向
1. **GPU加速**: 使用ComputeShader处理大量几何体
2. **LOD系统**: 距离相关的细节层级
3. **遮挡剔除**: 不可见区域的剔除
4. **增量更新**: 只更新变化的区域

### 工具链扩展
1. **可视化编辑器**: 拖拽式布局编辑
2. **模板系统**: 预制房间和布局模板  
3. **导入导出**: 布局数据的序列化
4. **版本控制**: 布局变更的追踪

该系统为Unity等距视角游戏提供了完整、高性能的网格可视化解决方案，适用于复杂的关卡设计和空间规划需求。通过统一的批处理绘制、智能的表面高亮和灵活的扩展架构，为开发者提供强大而高效的编辑器工具。
