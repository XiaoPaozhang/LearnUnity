# BezierCurveLineRenderer 使用说明

## 📋 多种使用模式

这个脚本支持多种起始点设置方式，你可以根据需求选择：

### 1️⃣ UI模式（从UI元素开始）
```csharp
// 从UI按钮开始拖拽
bezierCurve.显示箭头(uiButton.GetComponent<RectTransform>());
```

### 2️⃣ 世界坐标模式（纯3D坐标）
```csharp
// 从指定的世界坐标开始
Vector3 startPos = new Vector3(1, 2, 0);
bezierCurve.显示箭头(startPos);
```

### 3️⃣ 屏幕坐标模式
```csharp
// 从屏幕坐标开始
bezierCurve.显示箭头从屏幕坐标(Input.mousePosition);
```

### 4️⃣ 射线检测模式（3D）
```csharp
// 从鼠标点击的3D物体开始
bool hit = bezierCurve.显示箭头从鼠标射线();
if (!hit) Debug.Log("没有检测到物体");

// 只检测特定层级
bezierCurve.显示箭头从鼠标射线(LayerMask.NameToLayer("Ground"));
```

### 5️⃣ 2D射线检测模式
```csharp
// 从鼠标点击的2D物体开始
bool hit = bezierCurve.显示箭头从鼠标射线2D();
```

## 🎮 实际应用场景

### 场景1：技能拖拽（UI → 世界）
```csharp
public class SkillDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public BezierCurveLineRenderer bezierCurve;
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 从技能图标开始
        bezierCurve.显示箭头(GetComponent<RectTransform>());
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        bezierCurve.更新箭头可视化();
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        bezierCurve.隐藏箭头();
    }
}
```

### 场景2：纯3D游戏（点击开始）
```csharp
public class ObjectClickHandler : MonoBehaviour
{
    public BezierCurveLineRenderer bezierCurve;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 从点击的物体开始
            if (bezierCurve.显示箭头从鼠标射线())
            {
                Debug.Log("开始拖拽");
            }
        }
        
        if (bezierCurve.是否可见)
        {
            bezierCurve.更新箭头可视化();
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            bezierCurve.隐藏箭头();
        }
    }
}
```

### 场景3：2D游戏
```csharp
public class Character2DController : MonoBehaviour
{
    public BezierCurveLineRenderer bezierCurve;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 从角色位置开始
            bezierCurve.显示箭头(transform.position);
        }
        
        if (bezierCurve.是否可见)
        {
            bezierCurve.更新箭头可视化();
        }
    }
}
```

## ⚙️ 设置步骤

1. **创建GameObject**：
   ```
   Scene
   ├── Canvas (可选，只有UI模式需要)
   │   └── SkillButton (RectTransform)
   └── BezierCurveManager (独立GameObject)
       └── BezierCurveLineRenderer 脚本
   ```

2. **配置参数**：
   - `线条宽度`: 0.02-0.05
   - `曲线分辨率`: 50-100
   - 设置箭头精灵（可选）

3. **选择使用模式**：
   - 需要从UI开始 → 使用 `显示箭头(RectTransform)`
   - 纯3D坐标 → 使用 `显示箭头(Vector3)`
   - 点击检测 → 使用射线检测方法

## 🤔 常见问题

**Q: 为什么有这么多种模式？**
A: 不同游戏有不同需求：
- 卡牌游戏：从UI卡牌拖拽到战场
- 策略游戏：从地面点击开始画线
- RPG游戏：从技能栏拖拽到目标

**Q: 我的游戏完全不用UI，应该用哪种？**
A: 使用世界坐标模式或射线检测模式：
```csharp
// 方式1：直接坐标
bezierCurve.显示箭头(transform.position);

// 方式2：点击检测
bezierCurve.显示箭头从鼠标射线();
```

**Q: 可以混合使用吗？**
A: 可以！同一个脚本可以在运行时切换不同模式。

## 🔧 调试功能

右键脚本组件：
- "调试信息"：查看当前模式和坐标
- "切换调试控制点"：显示贝塞尔控制点
- "自动修复缩放"：一键修复坐标缩放问题
- "重置为默认值"：恢复推荐设置

## 🚨 常见问题排查

### 问题1：线条太大/在Game视图看不到
**症状**：Scene视图中线条巨大，Game视图只能看到一个角落
**原因**：坐标系缩放问题
**解决**：
1. 右键脚本选择"自动修复缩放"
2. 或手动调整：
   - `自动调整缩放` = true
   - `UI到世界缩放因子` = 0.005-0.01
   - `最大显示距离` = 10-20

### 问题2：线条显示为矩形
**症状**：看到的是粗矩形而不是线条
**原因**：线条宽度过大
**解决**：
- 调小 `线条宽度` 到 0.02-0.05

### 问题3：箭头位置错误
**症状**：箭头不跟随鼠标
**原因**：坐标转换错误
**解决**：
1. 检查Canvas设置
2. 使用"调试信息"查看坐标转换
3. 尝试不同的使用模式

### 问题4：完全看不到线条
**症状**：什么都看不到
**解决步骤**：
1. 确保脚本不在Canvas下
2. 检查Camera设置
3. 使用"调试信息"查看状态
4. 尝试"重置为默认值"

## 📋 推荐设置

### UI模式（从UI元素开始）
```
自动调整缩放: true
UI到世界缩放因子: 0.005
最大显示距离: 15
线条宽度: 0.03
```

### 纯世界坐标模式
```
自动调整缩放: false 或 true
最大显示距离: 20
线条宽度: 0.05
```

现在你可以完全不依赖UI系统，使用纯世界坐标或射线检测！ 