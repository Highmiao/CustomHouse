# Unity 等距视角网格系统 - 完整版

这是一个专为Unity 2D等距视角游戏设计的完整网格系统，支持地面网格、高度层级和垂直墙面网格的可视化。该系统仅在编辑器模式下工作，为关卡设计提供强大的工具。

## 功能特性

### 核心功能
- 🎯 2D等距视角网格系统（XY平面）
- 📐 编辑器专用，不影响运行时性能
- 🏗️ 多层网格支持（高度系统）
- 🧱 垂直墙面网格系统
- 🎨 实时网格可视化和预览
- 🔧 持久化预览窗口
- ⚡ 全局开关控制

### 家具系统
- 🪑 家具网格占用可视化
- 🎭 自定义家具形状支持
- 📏 高度层级支持
- 🍽️ 表面提供功能（如桌面）
- ⚠️ 位置验证和冲突检测
- 📐 网格对齐工具

### 墙面系统
- 🧱 四方向墙面支持（北、东、南、西）
- 📏 可配置墙面长度和高度
- 🎨 墙面网格可视化
- 🟥 地面占用显示
- 🔗 与家具系统集成
- 🎭 自定义墙面形状

### 编辑器工具
- 🖥️ 网格预览系统窗口
- 📐 自动网格对齐
- 🔍 冲突检测和报告
- 📦 批量对象管理
- 🎨 可视化配置选项

## 快速开始

### 1. 创建基础网格系统

通过菜单创建：
```
GameObject → Grid System → Create Grid System
```

### 2. 启用网格预览

```
Tools → Grid System → Grid Preview Window    // 打开预览窗口
Tools → Grid System → Toggle Grid Preview    // 快速切换
```

### 3. 创建家具

```
GameObject → Grid System → Create Furniture
```

### 4. 创建墙面

```
GameObject → Grid System → Create Wall
GameObject → Grid System → Create Wall Example Scene  // 创建示例房间
```

## 系统组件

### GridVisualization（网格核心）
- 坐标转换和网格计算
- 高度层级管理
- 墙面网格支持

### FurnitureItem（家具组件）
- 地面占用定义
- 高度和表面系统
- 冲突检测

### WallItem（墙面组件）
- 四方向墙面创建
- 地面占用和墙面网格
- 与家具系统集成

### GridPreviewSystem（预览系统）
- 持久化可视化
- 全局开关控制
- 多层级显示

## 可视化说明

### 颜色编码
- **白色网格线**: 基础网格
- **红色区域**: 地面占用
- **绿色区域**: 表面提供
- **青色区域**: 墙面格子
- **递减透明度**: 不同层级

### 信息标签
```
底部占用 (2,3) L0 H:0.0    // 地面占用
表面提供 (2,3) L1 H:1.0    // 表面层
墙面基础 (1,2) 方向:North   // 墙面信息
```

## 使用技巧

### 预览控制
- 使用 `Toggle Grid Preview` 快速开关
- 预览窗口提供详细控制
- 启用后保持常显状态

### 对象管理
- Inspector中的"Snap to Grid"对齐
- 批量选择和操作
- 实时冲突检测

### 高度系统
- `baseHeight`: 对象放置的层级
- `furnitureHeight`: 对象本身高度
- `providesSurface`: 是否提供表面

### 墙面使用
- 四个方向：North、East、South、West
- 可配置长度和高度
- 支持自定义形状

## 编辑器专用设计

所有功能都使用 `#if UNITY_EDITOR` 包装，确保：
- 运行时零性能开销
- 只在编辑器中可视化
- 构建时自动排除

## 扩展示例

### 创建房间布局
```csharp
// 创建四面墙的房间
CreateWall(grid, Vector2Int.zero, WallDirection.North, 5);
CreateWall(grid, new Vector2Int(0, 5), WallDirection.East, 4);
CreateWall(grid, new Vector2Int(4, 1), WallDirection.South, 5);
CreateWall(grid, Vector2Int.zero, WallDirection.West, 5);

// 添加家具
CreateFurniture(grid, new Vector2Int(1, 1), "Chair");
CreateFurniture(grid, new Vector2Int(3, 3), "Table");
```

### 自定义形状
```csharp
// L形沙发
var occupancy = sofa.Occupancy;
occupancy.useCustomShape = true;
occupancy.customShape = new List<Vector2Int>
{
    Vector2Int.zero, Vector2Int.right, Vector2Int.right * 2,
    Vector2Int.up, new Vector2Int(1, 1)
};
```

## 故障排除

1. **网格不显示**: 检查预览开关状态
2. **对象不对齐**: 使用"Snap to Grid"功能
3. **高度异常**: 检查heightPerLevel设置
4. **墙面不显示**: 确认显示开关已启用
5. **编译错误**: 确保Editor脚本在Editor文件夹内

## 技术支持

- 查看Console错误信息
- 使用Grid System Tools诊断
- 验证组件配置
- 确保Unity版本兼容

该系统为Unity 2D等距视角游戏提供完整的关卡设计解决方案，支持网格、家具和墙面的可视化管理。
