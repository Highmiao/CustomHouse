# Unity 等距视角统一网格可视化和高亮系统

这是一个专为Unity编辑器设计的**高性能、统一的等距2D网格可视化和表面高亮系统**，支持地面网格、家具、墙体、地板的完整可视化，以及实时表面高亮交互。

**⚠️ 重要：此系统仅在编辑器模式下工作，运行时会自动禁用所有相关组件。**

## 🎯 核心特性

### 性能革命 ⚡
- **统一批处理绘制**：使用GL绘制和材质共享，大幅提升性能
- **智能缓存系统**：组件缓存减少查找开销，支持1秒间隔刷新
- **频率限制**：20fps更新限制，避免过度重绘
- **零运行时开销**：完全的编辑器专用设计

### 可视化系统 🎨
- **多层级网格**：支持地面和多高度层级网格显示
- **家具网格**：显示占用格子和提供的表面格子
- **墙体系统**：墙体占用 + 垂直墙面格子（正确的平行四边形）
- **地板系统**：地板占用 + 地板表面格子
- **等距投影**：专为XY平面等距视角优化

### 实时交互 ✨
- **表面高亮**：鼠标悬停实时高亮可用表面
- **多表面支持**：地面、地板、家具表面、墙面的统一高亮
- **信息显示**：显示表面类型、高度、所属对象
- **优先级系统**：智能的表面选择优先级

### 层级控制 �️
- **全局开关**：`GridVisualization.showGridSystem` 主控制
- **对象开关**：每个物体的独立显示控制
- **功能开关**：表面、高亮等功能的独立控制

## 🚀 快速开始

### 1. 创建网格系统
```
Tools → Grid System → Grid Preview Window  // 打开预览窗口
点击 "Create New" 创建网格系统
```

### 2. 启用统一预览
```
Tools → Grid System → Toggle Grid Preview  // 快速切换
Tools → Grid System → Grid Preview Window  // 详细控制
```
**⚡ 特性：启用后保持常显，即使关闭窗口也持续显示！**

### 3. 添加对象
```
GameObject → Grid System → Create Furniture  // 创建家具
GameObject → Grid System → Create Wall       // 创建墙体  
GameObject → Grid System → Create Floor      // 创建地板
```

### 4. 配置网格设置
在Inspector中调整网格参数：
- `Grid Size`: (1.0, 0.5) - 适合XY平面等距视角
- `Grid Dimensions`: 网格范围
- `Is Isometric`: 启用等距投影
- `Height Per Level`: 层级间距

## 🏗️ 系统架构

### 核心组件
```
GridPreviewSystem (统一控制中心)
├── 批处理绘制系统
│   ├── GridMaterialManager (材质管理)
│   ├── BatchDrawData (批处理数据)
│   └── GL绘制优化
├── 几何体收集器
│   ├── CollectGridGeometry (网格)
│   ├── CollectFurnitureGeometry (家具)
│   ├── CollectWallGeometry (墙体)
│   └── CollectFloorGeometry (地板)
├── 表面高亮系统
│   ├── HandleSurfaceHighlight (高亮处理)
│   ├── SurfaceInfo (表面信息)
│   └── GetSurfacesAtPosition (位置查询)
└── 性能优化
    ├── 组件缓存
    ├── 频率控制
    └── 内存管理
```

### 支持的对象类型

#### GridVisualization (网格核心)
- 坐标转换和网格计算
- 多层级高度管理  
- 等距投影支持

#### FurnitureItem (家具)
- **占用区域**: 地面占据的格子
- **表面区域**: 顶部提供的可用表面
- **高度系统**: 3D层级支持
- **冲突检测**: 实时位置验证

#### WallItem (墙体)
- **墙体占用**: 基座在地面的占用
- **墙面格子**: 垂直面的挂载区域
- **四方向支持**: North/East/South/West
- **平行四边形**: 正确的等距投影

#### FloorItem (地板)
- **地板占用**: 地板本身的区域
- **地板表面**: 可行走的表面区域
- **厚度支持**: 地板高度 + 厚度
- **自定义形状**: 不规则地板支持

## 🎨 可视化系统

### 颜色编码
- **白色网格线**: 基础等距网格
- **红色区域**: 占用格子（家具底部、墙体基座、地板）
- **绿色区域**: 表面格子（家具顶部、地板表面）
- **青色区域**: 墙面格子（垂直挂载面）
- **黄色高亮**: 鼠标悬停的表面高亮

### 几何体形状
- **地面格子**: 水平菱形（等距投影）
- **墙面格子**: 垂直平行四边形（正确的墙面投影）
- **高度层级**: 不同高度的正确3D显示

### 实时信息显示
```
家具信息: (2,3) L1 H:1.0        // 坐标、层级、高度
墙面基础: (1,2) 方向:North       // 位置、朝向
表面类型: FurnitureSurface       // 高亮时显示表面类型
```

## ✨ 表面高亮系统

### 支持的表面类型
```csharp
Ground           // 地面网格
Floor            // 地板表面  
FurnitureSurface // 家具顶部表面
WallSurface      // 墙面挂载面
```

### 高亮功能
- **实时跟踪**: 鼠标移动时实时高亮
- **多表面识别**: 同一位置多个表面的智能选择
- **信息提示**: 显示表面高度、类型、所属对象
- **性能优化**: 20fps限制避免过度刷新

### 使用方法
1. 在Grid Preview Window中启用 "Enable Surface Highlight"
2. 调整高亮颜色和透明度
3. 在Scene视图中移动鼠标查看表面高亮

## 🎛️ 控制层级

系统采用分层控制设计：

```
1. GridVisualization.showGridSystem (全局主开关)
   ├── 控制所有网格可视化的显示/隐藏
   └── 通过菜单 Tools → Toggle Grid Preview 快速切换

2. 对象级开关 (局部控制)
   ├── FurnitureItem.showInSceneView
   ├── WallItem.showInSceneView
   └── FloorItem.showInSceneView

3. 功能级开关 (细节控制)
   ├── showSurfaceInSceneView (表面显示)
   ├── showWallSurfaceInSceneView (墙面显示)
   └── enableSurfaceHighlight (高亮功能)
```

## ⚡ 性能优化

### 批处理绘制
- **材质共享**: `GridMaterialManager` 统一管理材质
- **几何体分组**: 按颜色批处理，减少draw calls
- **GL绘制**: 底层优化的绘制调用

### 智能缓存
```csharp
cachedFurniture  // 家具组件缓存
cachedWalls      // 墙体组件缓存  
cachedFloors     // 地板组件缓存
```
- 1秒刷新间隔，避免重复查找
- 自动检测组件变化

### 频率控制
- **更新限制**: 20fps (0.05秒间隔)
- **事件过滤**: 只在必要时重绘
- **鼠标优化**: 智能的鼠标事件处理

## 🔧 使用技巧

### 快速操作
```
Toggle Grid Preview     // 快速全局开关
Grid Preview Window     // 详细控制面板
Check Overlaps          // 冲突检测和报告
Select All Furniture    // 批量选择对象
Snap to Grid           // 网格对齐
```

### 高度层级控制
- `Show All Levels`: 显示所有高度层级
- `Max Levels`: 限制显示的最大层数
- `Height Per Level`: 设置层级间距

### 表面高亮控制
- `Enable Surface Highlight`: 启用/禁用高亮
- `Highlight Color`: 设置高亮颜色
- `Highlight Alpha`: 调整透明度

### 自定义形状
```csharp
// L形沙发示例
occupancy.useCustomShape = true;
occupancy.customShape = new List<Vector2Int>
{
    Vector2Int.zero,      // (0,0)
    Vector2Int.right,     // (1,0)
    Vector2Int.right * 2, // (2,0)
    Vector2Int.up,        // (0,1)
    new Vector2Int(1, 1)  // (1,1)
};
```

## 💻 示例代码

### 基础操作
```csharp
// 获取网格系统
var gridViz = FindObjectOfType<GridVisualization>();

// 坐标转换
Vector2Int gridPos = gridViz.WorldToGrid(transform.position);
Vector3 worldPos = gridViz.GridToWorld(new Vector2Int(5, 3));

// 家具操作
var furniture = GetComponent<FurnitureItem>();
var positions = furniture.GetOccupiedGridPositions();
bool isValid = furniture.IsValidPosition();

// 表面检测
var surfaces = GridPreviewSystem.GetSurfacesAtPosition(gridPos);
```

### 高级功能
```csharp
// 启用表面高亮
GridPreviewSystem.EnableSurfaceHighlight = true;
GridPreviewSystem.SurfaceHighlightColor = Color.yellow;

// 检查表面类型
foreach (var surface in surfaces)
{
    Debug.Log($"Surface: {surface.type} at {surface.gridPosition}");
    Debug.Log($"Height: {surface.height}, Object: {surface.objectName}");
}
```

## ⚠️ 重要注意事项

### 编辑器专用
- 所有可视化组件仅在编辑器模式下工作
- 运行时自动禁用，零性能开销
- 使用 `#if UNITY_EDITOR` 包装

### 性能考虑
- 批处理绘制大幅提升性能
- 智能缓存减少重复查找
- 频率限制避免过度刷新

### 坐标系统
- 专为XY平面等距视角设计
- 网格坐标从(0,0)开始
- 使用GridVisualization进行坐标转换

### 墙面系统
- 墙面格子是垂直的平行四边形
- 法向量计算决定墙面位置
- 支持四个方向：North/East/South/West

## 🐛 故障排除

### 网格不显示
1. 检查 `GridVisualization.showGridSystem` 是否启用
2. 确认Scene视图聚焦
3. 使用 `Tools → Toggle Grid Preview` 重新启用

### 性能问题
1. 确认使用批处理绘制（新系统）
2. 检查缓存是否正常工作
3. 调整更新频率设置

### 墙面显示异常
1. 确认墙面方向设置正确
2. 检查 `showWallSurfaceInSceneView` 开关
3. 验证墙面高度设置

### 高亮不工作
1. 启用 `Enable Surface Highlight`
2. 检查Scene视图鼠标焦点
3. 调整高亮颜色透明度

## 🚀 扩展功能

### 自定义表面类型
```csharp
// 添加新的表面类型
public enum SurfaceType
{
    Ground, Floor, FurnitureSurface, WallSurface,
    Ceiling,     // 新增：天花板
    Platform,    // 新增：平台
    Slope        // 新增：斜坡
}
```

### 新几何体支持
- 实现 `CollectCustomGeometry()`
- 添加到批处理系统
- 支持新的绘制形状

该系统为Unity等距视角游戏提供完整的网格可视化解决方案，兼具高性能和丰富功能，是关卡设计的强大工具。
