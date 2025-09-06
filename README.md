# 等距视角3D网格可视化系统 (编辑器专用)

这个系统可以帮助你在Unity编辑器中可视化XY平面上的等### 4. 配置家具高度属性

选择家具物体，在Inspector中可以设置：

**地面占用：**
- **Size**: 家具占据的网格大小（例如凳子可以设置为2x1）
- **Offset**: 相对于物体位置的偏移
- **Custom Shape**: 自定义形状（用于不规则家具）
- **Occupied Color**: 地面占用区域的显示颜色

**高度和表面：**
- **Base Height**: 家具的基础高度（相对于地面，避免等距投影影响）
- **Furniture Height**: 家具的物理高度（世界单位）
- **Provides Surface**: 是否在顶部提供可用表面
- **Surface Size**: 顶部表面提供的网格大小
- **Surface Offset**: 表面相对于物体位置的偏移
- **Custom Surface Shape**: 自定义表面形状
- **Surface Color**: 表面区域的显示颜色家具的**3D网格占用**，包括地面占用和高度层级的表面提供。

**⚠️ 重要：此系统仅在编辑器模式下工作，运行时会自动禁用所有相关组件。**

## 功能特点

- 🎯 专为XY平面的等距视角2D游戏设计
- 📐 **编辑器专用**：仅在Scene视图中可视化，不影响运行时性能
- 🏗️ **3D高度支持**：显示家具的地面占用和上层表面提供
- 🎨 **多层可视化**：实时预览不同高度层级的网格
- ⚠️ 冲突检测：编辑器中实时检测家具重叠冲突
- 🔧 便捷的编辑器工具和预览窗口
- 📝 支持自定义形状的家具占用区域和表面形状

## 快速开始

### 1. 设置网格系统

1. 打开 `Tools > Grid System > Grid Preview Window` 打开预览窗口
2. 点击 "Create New" 创建网格系统
3. 在Inspector中调整网格设置：
   - `Grid Size`: 每个格子的世界尺寸 (建议X=1.0, Y=0.5 适合XY平面等距视角)
   - `Grid Dimensions`: 网格的格子数量
   - `Is Isometric`: 启用等距视角
   - `Isometric Angle`: 等距视角的角度（通常30度）

### 2. 开启预览

有三种方式控制网格显示：

1. **预览窗口开关**：
   - 打开Grid Preview Window窗口
   - 勾选或取消 "Enable Grid Preview"

2. **快速菜单切换**：
   - 使用菜单 `Tools > Grid System > Toggle Grid Preview`
   - 菜单项会显示当前状态（打勾表示已启用）

3. **窗口按钮**：
   - 在预览窗口中点击 "Enable/Disable Grid Preview" 按钮

**⚡ 重要特性：一旦启用预览，即使关闭预览窗口，网格也会保持显示状态！**

### 3. 添加家具

#### 方法一：使用工具窗口
1. 在Grid System Tools窗口中点击 "Create Bench Prefab" 或 "Create Chair Prefab"
2. 新创建的家具会自动添加到场景中

#### 方法二：手动添加
1. 选择场景中的物体
2. 在Grid System Tools窗口中点击 "Add Furniture Component to Selected"
3. 在Inspector中设置家具的占用大小

### 3. 配置家具属性

选择家具物体，在Inspector中可以设置：

- **Size**: 家具占据的网格大小（例如凳子可以设置为2x1）
- **Offset**: 相对于物体位置的偏移
- **Custom Shape**: 自定义形状（用于不规则家具）
- **Occupied Color**: 在Scene视图中显示的颜色

### 5. 高度层级可视化

在Scene视图中你可以看到：
- 白色网格线（仅在Grid Preview Window启用时显示）
- **地面层（Level 0）**：家具占据的格子（较深颜色）
- **上层（Level 1+）**：家具表面提供的格子（较淡颜色）
- 不同高度层的网格线（递减透明度）
- 红色表示有冲突的区域
- 物体附近显示的网格坐标和层级信息

**标签说明：**
- `底部占用 (x, y) L0`：地面占用区域
- `表面提供 (x, y) L1`：上层表面区域

**注意：所有可视化效果仅在编辑器模式下显示，运行时不会影响性能。**

## 核心组件

### GridVisualization (编辑器专用)
- 管理网格设置和坐标转换
- 支持等距视角和正交视角  
- 提供世界坐标与网格坐标的转换方法
- 运行时自动禁用

### FurnitureItem (编辑器专用)
- 表示场景中的家具物品
- 定义在网格中的占用区域
- 检测与其他家具的冲突
- 运行时自动禁用

### GridPreviewSystem (编辑器窗口)
- 专用的编辑器预览窗口
- 实时控制网格可视化的开关
- 提供便捷的预览控制选项

### GridVisualizationEditor
- 在Scene视图中绘制网格和占用区域
- 提供实时的可视化反馈

### GridSystemTools
- 编辑器工具窗口
- 快速创建和管理网格系统
- 批量操作家具物品

## 使用技巧

### 1. 使用开关控制
- **快速切换**：`Tools > Grid System > Toggle Grid Preview` 
- **预览窗口**：打开Grid Preview Window进行详细控制
- **常显特性**：启用后即使关闭窗口也保持显示
- **状态指示**：菜单项显示当前开关状态

### 2. 使用预览窗口
- 打开 `Tools > Grid System > Grid Preview Window`
- 使用预览窗口控制网格显示的开关
- 实时查看家具占用情况和冲突检测

### 3. 高度层级控制
- 在Grid Preview Window中控制 "Show All Levels"
- 设置 "Max Levels" 来限制显示的最大层数
- 调整 "Height Per Level" 来设置每层的高度间距

### 4. 对齐到网格
- 选择家具物体
- 在Inspector中点击 "Snap to Grid" 按钮
- 或在Grid System Tools中点击 "Snap All to Grid"

### 2. 检查重叠
- Grid System Tools窗口会显示重叠的家具数量
- 点击 "Select Overlapping Items" 可以选中有冲突的物体
- 重叠的区域会在Scene视图中显示为红色

### 5. 检查重叠
- Grid Preview Window会显示重叠的家具数量
- 点击 "Check Overlaps" 可以检查并选中有冲突的物体
- 重叠的区域会在Scene视图中显示为红色

### 6. 自定义形状
- 启用 "Use Custom Shape"
- 在 "Custom Shape" 列表中添加相对坐标
- 例如L形家具可以添加: (0,0), (1,0), (0,1)

### 7. 批量操作
- 使用Grid System Tools窗口进行批量操作
- "Select All Furniture" 选择所有家具
- "Snap All to Grid" 将所有家具对齐到网格

## 示例代码

```csharp
// 获取家具占据的网格位置
var furniture = GetComponent<FurnitureItem>();
var positions = furniture.GetOccupiedGridPositions();

// 检查位置是否有效
bool isValid = furniture.IsValidPosition();

// 世界坐标转网格坐标
var gridViz = FindObjectOfType<GridVisualization>();
Vector2Int gridPos = gridViz.WorldToGrid(transform.position);

// 网格坐标转世界坐标
Vector3 worldPos = gridViz.GridToWorld(new Vector2Int(5, 3));
```

## 注意事项

1. **编辑器专用**：所有可视化组件仅在编辑器模式下工作，运行时会自动禁用
2. 确保场景中只有一个GridVisualization组件
3. 使用Grid Preview Window获得最佳的预览体验
4. 网格坐标从(0,0)开始，向右和向上递增
5. 系统专为XY平面的等距视角设计，坐标转换已自动处理
6. 修改网格设置后需要刷新Scene视图才能看到变化
7. 默认网格比例为X:Y = 1:0.5，适合标准等距视角
8. 关闭预览窗口后，网格可视化会停止，但不影响组件数据

## 扩展功能

你可以根据需要扩展这个系统：
- 添加更多家具类型
- 实现拖拽放置功能
- 添加旋转支持
- 集成物理碰撞检测
- 保存和加载布局数据

## 故障排除

### 网格不显示
- 检查GridVisualization组件是否存在
- 确保在Scene视图中选择了正确的显示模式
- 点击Grid System Tools中的 "Refresh Scene View"

### 家具占用区域不正确
- 检查家具的Size设置
- 确认GridVisualization的网格尺寸设置正确
- 使用 "Snap to Grid" 功能重新对齐

### 坐标转换错误
- 检查 "Is Isometric" 设置是否正确
- 确认 "Isometric Angle" 设置（通常为30度）
- 验证Grid Origin的位置设置
