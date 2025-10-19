# Unity Timeline 内存编辑系统 - 技术实现文档

## 目录

1. [核心需求分析](#核心需求分析)
2. [技术架构设计](#技术架构设计)
3. [核心技术原理](#核心技术原理)
4. [模块详细实现](#模块详细实现)
5. [关键技术难点](#关键技术难点)
6. [完整实现流程](#完整实现流程)

---

## 核心需求分析

### 需求描述
在 Unity Timeline 编辑器中实现以下功能：
1. 打开 Timeline 资源时不修改磁盘文件
2. 在内存中创建 Timeline 的完整副本
3. 监听所有编辑操作（轨道增删改、Clip 增删改、属性变化）
4. 支持预览播放模式下的监听
5. 手动控制何时保存到磁盘

### 技术挑战
- Unity Timeline 默认会自动保存修改
- 需要深度克隆复杂的对象层级结构
- 需要监听所有类型的变化（包括自定义 Track/Clip）
- 需要区分内存对象和磁盘对象

---

## 技术架构设计

### 系统架构图

```
┌─────────────────────────────────────────────────────────┐
│                    Timeline 内存编辑系统                   │
└─────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│ TimelineCloner│    │ChangeMonitor │    │SaveInterceptor│
│  深度克隆工具  │    │  变化监听器   │    │  保存拦截器   │
└──────────────┘    └──────────────┘    └──────────────┘
        │                   │                   │
        │                   │                   │
        ▼                   ▼                   ▼
┌──────────────────────────────────────────────────────┐
│              TimelineMemoryEditor                     │
│                 编辑器窗口 & 会话管理                   │
└──────────────────────────────────────────────────────┘
```

### 核心模块

| 模块 | 职责 | 关键技术 |
|------|------|---------|
| **TimelineCloner** | 深度克隆 Timeline 对象 | ScriptableObject, JsonUtility, SerializedObject |
| **TimelineChangeMonitor** | 监听所有编辑操作 | ObjectChangeEvents.changesPublished |
| **TimelineSaveInterceptor** | 防止内存对象保存 | AssetModificationProcessor |
| **TimelineMemoryEditor** | 会话管理和 UI | EditorWindow, Dictionary |

---

## 核心技术原理

### 1. 内存对象创建

#### 原理
使用 `ScriptableObject.CreateInstance<T>()` 创建非持久化对象，并设置 `HideFlags.DontSave` 标记。

#### 关键代码
```csharp
TimelineAsset memoryTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
memoryTimeline.name = original.name + " (Memory)";
memoryTimeline.hideFlags = HideFlags.DontSave;  // 关键：防止保存
```

#### 技术要点
- `HideFlags.DontSave` 告诉 Unity 不要序列化此对象
- 对象只存在于内存中，关闭 Unity 后会丢失
- 名称添加 "(Memory)" 后缀便于识别

---

### 2. 深度克隆 Timeline

#### 原理
Timeline 的层级结构：`TimelineAsset → TrackAsset → TimelineClip → PlayableAsset`

需要递归克隆每一层，并维护引用关系。

#### 克隆策略

**方案对比：**

| 方案 | 优点 | 缺点 | 是否采用 |
|------|------|------|---------|
| `Object.Instantiate()` | 简单 | 会创建磁盘资源 | ❌ |
| `JsonUtility` 序列化 | 深度复制 | 丢失引用关系 | ✅ 部分使用 |
| `SerializedObject` | 保留所有属性 | 需要手动处理 | ✅ 配合使用 |

**最终方案：** 组合使用 `JsonUtility` + `SerializedObject`

#### 完整克隆流程

```
1. 创建 TimelineAsset 内存对象
   ↓
2. 克隆基本属性（duration, frameRate 等）
   ↓
3. 递归克隆所有根轨道
   ├─ 创建 TrackAsset 内存对象
   ├─ 克隆轨道属性
   ├─ 克隆所有 Clip
   │   ├─ 克隆 PlayableAsset
   │   ├─ 创建 TimelineClip
   │   └─ 复制 Clip 属性（包括只读属性）
   └─ 递归克隆子轨道
   ↓
4. 建立引用映射表（Dictionary<Object, Object>）
   ↓
5. 返回完整的内存版 Timeline
```

---

### 3. 监听编辑操作

#### 核心 API：ObjectChangeEvents.changesPublished

**API 说明：**
```csharp
// Unity 编辑器回调，监听所有可撤销的对象变化
ObjectChangeEvents.changesPublished += OnChangesPublished;

void OnChangesPublished(ref ObjectChangeEventStream stream)
{
    // stream 包含所有变化事件
    for (int i = 0; i < stream.length; i++)
    {
        ObjectChangeKind eventType = stream.GetEventType(i);
        // 处理不同类型的事件
    }
}
```

**事件类型：**

| 事件类型 | 说明 | 对应操作 |
|---------|------|---------|
| `CreateAssetObject` | 创建资源对象 | 添加轨道、添加 Clip |
| `DestroyAssetObject` | 销毁资源对象 | 删除轨道、删除 Clip |
| `ChangeAssetObjectProperties` | 属性变化 | 修改任何属性 |
| `UpdatePrefabInstances` | Prefab 更新 | （Timeline 不常用） |

#### 监听实现策略

**1. 类型识别**
```csharp
void HandleAssetPropertyChange(ref ObjectChangeEventStream stream, int index)
{
    stream.GetChangeAssetObjectPropertiesEvent(index, out var evt);
    Object obj = EditorUtility.InstanceIDToObject(evt.instanceId);
    
    if (obj is TimelineAsset timeline)
    {
        // 处理 Timeline 属性变化
    }
    else if (obj is TrackAsset track)
    {
        // 处理 Track 属性变化
    }
    // ... 其他类型
}
```

**2. 内存对象识别**
```csharp
static bool IsMemoryObject(Object obj)
{
    if (obj == null) return false;
    
    // 方法1：检查 HideFlags
    if ((obj.hideFlags & HideFlags.DontSave) != 0)
        return true;
    
    // 方法2：检查名称后缀
    if (obj.name.Contains("(Memory)"))
        return true;
    
    return false;
}
```

**3. Clip 属性变化检测**

由于 `TimelineClip` 不是 `Object` 子类，无法直接监听，需要通过 `TrackAsset` 间接监听：

```csharp
// 缓存 Clip 信息
class ClipInfo
{
    public double start;
    public double duration;
    public double timeScale;
    public double clipIn;
    
    public void UpdateValues(TimelineClip clip)
    {
        start = clip.start;
        duration = clip.duration;
        timeScale = clip.timeScale;
        clipIn = clip.clipIn;
    }
}

// 检测变化
Dictionary<TimelineClip, ClipInfo> clipCache = new Dictionary<TimelineClip, ClipInfo>();

void CheckClipChanges(TrackAsset track)
{
    foreach (var clip in track.GetClips())
    {
        if (!clipCache.ContainsKey(clip))
        {
            clipCache[clip] = new ClipInfo();
            clipCache[clip].UpdateValues(clip);
        }
        else
        {
            var cached = clipCache[clip];
            if (cached.start != clip.start || 
                cached.duration != clip.duration ||
                cached.timeScale != clip.timeScale ||
                cached.clipIn != clip.clipIn)
            {
                // 检测到变化
                LogClipChange(clip, cached);
                cached.UpdateValues(clip);
            }
        }
    }
}
```

---

### 4. 防止自动保存

#### 原理
Unity 在以下情况会自动保存资源：
- 编辑器失去焦点
- 执行某些操作（如播放模式切换）
- 手动保存（Ctrl+S）

需要拦截这些保存操作。

#### 实现方案

**方案1：HideFlags.DontSave（主要防护）**
```csharp
memoryObject.hideFlags = HideFlags.DontSave;
```
- Unity 不会序列化带此标记的对象
- 但不能完全阻止所有保存路径

**方案2：AssetModificationProcessor（二次防护）**
```csharp
public class TimelineSaveInterceptor : UnityEditor.AssetModificationProcessor
{
    static string[] OnWillSaveAssets(string[] paths)
    {
        List<string> filteredPaths = new List<string>();
        
        foreach (var path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            
            // 检查是否是内存对象
            if (asset != null && IsMemoryObject(asset))
            {
                Debug.LogWarning($"[保存拦截] 内存对象不应保存: {path}");
                continue;  // 跳过此路径
            }
            
            filteredPaths.Add(path);
        }
        
        return filteredPaths.ToArray();
    }
}
```

---

## 模块详细实现

### 模块1：TimelineCloner（深度克隆）

#### 核心方法

**1. DeepClone - 主入口**
```csharp
public static TimelineAsset DeepClone(TimelineAsset original)
{
    // 1. 创建内存对象
    TimelineAsset clone = ScriptableObject.CreateInstance<TimelineAsset>();
    clone.name = original.name + " (Memory)";
    clone.hideFlags = HideFlags.DontSave;
    
    // 2. 克隆基本属性
    CloneTimelineProperties(clone, original);
    
    // 3. 克隆所有轨道
    Dictionary<Object, Object> cloneMap = new Dictionary<Object, Object>();
    foreach (var track in original.GetRootTracks())
    {
        CloneTrack(track, clone, cloneMap);
    }
    
    return clone;
}
```

**2. CloneTimelineProperties - 克隆 Timeline 属性**
```csharp
static void CloneTimelineProperties(TimelineAsset target, TimelineAsset source)
{
    // 使用 SerializedObject 复制所有序列化属性
    SerializedObject sourceObj = new SerializedObject(source);
    SerializedObject targetObj = new SerializedObject(target);
    
    // 复制 duration
    SerializedProperty durationProp = sourceObj.FindProperty("m_DurationMode");
    if (durationProp != null)
    {
        targetObj.FindProperty("m_DurationMode").enumValueIndex = durationProp.enumValueIndex;
    }
    
    // 复制 frameRate
    SerializedProperty frameRateProp = sourceObj.FindProperty("m_FrameRate");
    if (frameRateProp != null)
    {
        targetObj.FindProperty("m_FrameRate").doubleValue = frameRateProp.doubleValue;
    }
    
    targetObj.ApplyModifiedPropertiesWithoutUndo();
}
```

**3. CloneTrack - 递归克隆轨道**
```csharp
static TrackAsset CloneTrack(TrackAsset original, PlayableAsset parent, 
                             Dictionary<Object, Object> cloneMap)
{
    // 1. 创建轨道内存对象
    TrackAsset clone = ScriptableObject.CreateInstance(original.GetType()) as TrackAsset;
    clone.name = original.name;
    clone.hideFlags = HideFlags.DontSave;
    
    // 2. 添加到父对象
    if (parent is TimelineAsset timeline)
    {
        timeline.CreateTrack(clone.GetType(), null, clone.name);
        // 获取刚创建的轨道并替换
        var tracks = timeline.GetRootTracks().ToList();
        var lastTrack = tracks[tracks.Count - 1];
        // 使用 SerializedObject 替换
    }
    
    // 3. 克隆轨道属性
    clone.muted = original.muted;
    clone.locked = original.locked;
    
    // 4. 克隆所有 Clip
    foreach (var clip in original.GetClips())
    {
        CloneClip(clip, clone, cloneMap);
    }
    
    // 5. 递归克隆子轨道
    foreach (var subTrack in original.GetChildTracks())
    {
        CloneTrack(subTrack, clone, cloneMap);
    }
    
    cloneMap[original] = clone;
    return clone;
}
```

**4. CloneClip - 克隆 Clip（关键难点）**
```csharp
static void CloneClip(TimelineClip original, TrackAsset targetTrack, 
                      Dictionary<Object, Object> cloneMap)
{
    // 1. 创建默认 Clip
    TimelineClip clip = targetTrack.CreateDefaultClip();
    
    // 2. 克隆 PlayableAsset
    if (original.asset != null)
    {
        PlayableAsset originalAsset = original.asset as PlayableAsset;
        if (originalAsset != null)
        {
            PlayableAsset clonedAsset = ClonePlayableAsset(originalAsset);
            cloneMap[original.asset] = clonedAsset;
            
            // 使用 SerializedObject 设置 asset（因为 clip.asset 是只读的）
            SerializedObject trackObj = new SerializedObject(targetTrack);
            SerializedProperty clipsArrayProp = trackObj.FindProperty("m_Clips");
            
            if (clipsArrayProp != null && clipsArrayProp.isArray)
            {
                // 获取最后一个 clip（刚创建的）
                int lastIndex = clipsArrayProp.arraySize - 1;
                SerializedProperty lastClipProp = clipsArrayProp.GetArrayElementAtIndex(lastIndex);
                SerializedProperty assetProp = lastClipProp.FindPropertyRelative("m_Asset");
                
                if (assetProp != null)
                {
                    assetProp.objectReferenceValue = clonedAsset;
                }
            }
            
            trackObj.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    
    // 3. 复制可写属性
    clip.start = original.start;
    clip.duration = original.duration;
    clip.timeScale = original.timeScale;
    clip.clipIn = original.clipIn;
    clip.displayName = original.displayName;
    clip.easeInDuration = original.easeInDuration;
    clip.easeOutDuration = original.easeOutDuration;
    clip.blendInDuration = original.blendInDuration;
    clip.blendOutDuration = original.blendOutDuration;
    clip.mixInCurve = original.mixInCurve;
    clip.mixOutCurve = original.mixOutCurve;
    
    // 4. 复制只读属性（使用 SerializedObject）
    SerializedObject clipObj = new SerializedObject(targetTrack);
    SerializedProperty clipsProp = clipObj.FindProperty("m_Clips");
    
    if (clipsProp != null && clipsProp.isArray)
    {
        // 找到刚创建的 clip
        for (int i = 0; i < clipsProp.arraySize; i++)
        {
            SerializedProperty clipProp = clipsProp.GetArrayElementAtIndex(i);
            SerializedProperty startProp = clipProp.FindPropertyRelative("m_Start");
            
            // 通过 start 值匹配（因为刚设置过）
            if (startProp != null && Math.Abs(startProp.doubleValue - clip.start) < 0.001)
            {
                // 设置 extrapolation modes（只读属性）
                SerializedProperty preProp = clipProp.FindPropertyRelative("m_PreExtrapolationMode");
                SerializedProperty postProp = clipProp.FindPropertyRelative("m_PostExtrapolationMode");
                
                if (preProp != null)
                    preProp.enumValueIndex = (int)original.preExtrapolationMode;
                if (postProp != null)
                    postProp.enumValueIndex = (int)original.postExtrapolationMode;
                
                break;
            }
        }
    }
    
    clipObj.ApplyModifiedPropertiesWithoutUndo();
}
```

**5. ClonePlayableAsset - 克隆 PlayableAsset**
```csharp
static PlayableAsset ClonePlayableAsset(PlayableAsset original)
{
    // 使用 JsonUtility 进行深度复制
    string json = JsonUtility.ToJson(original);
    PlayableAsset clone = ScriptableObject.CreateInstance(original.GetType()) as PlayableAsset;
    JsonUtility.FromJsonOverwrite(json, clone);
    
    clone.name = original.name;
    clone.hideFlags = HideFlags.DontSave;
    
    return clone;
}
```

**6. SaveToOriginal - 保存到磁盘**
```csharp
public static void SaveToOriginal(TimelineAsset memoryVersion, TimelineAsset original)
{
    // 1. 清空原始 Timeline
    var tracks = original.GetRootTracks().ToList();
    foreach (var track in tracks)
    {
        original.DeleteTrack(track);
    }

    // 2. 克隆内存版本到原始对象
    Dictionary<Object, Object> cloneMap = new Dictionary<Object, Object>();
    foreach (var track in memoryVersion.GetRootTracks())
    {
        CloneTrack(track, original, cloneMap);
    }

    // 3. 克隆属性
    CloneTimelineProperties(original, memoryVersion);

    // 4. 标记为脏并保存
    EditorUtility.SetDirty(original);
    AssetDatabase.SaveAssets();

    Debug.Log($"[Timeline 已保存] {original.name}");
}
```

---

### 模块2：TimelineChangeMonitor（变化监听）

#### 核心实现

**1. 初始化监听器**
```csharp
[InitializeOnLoad]
public class TimelineChangeMonitor
{
    static TimelineChangeMonitor()
    {
        // 订阅对象变化事件
        ObjectChangeEvents.changesPublished += OnChangesPublished;

        // 订阅撤销/重做事件
        Undo.undoRedoPerformed += OnUndoRedoPerformed;

        Debug.Log("【Timeline 监听器已启动】");
    }
}
```

**2. 主事件处理**
```csharp
static void OnChangesPublished(ref ObjectChangeEventStream stream)
{
    for (int i = 0; i < stream.length; i++)
    {
        var eventType = stream.GetEventType(i);

        switch (eventType)
        {
            case ObjectChangeKind.ChangeAssetObjectProperties:
                HandleAssetPropertyChange(ref stream, i);
                break;

            case ObjectChangeKind.CreateAssetObject:
                HandleAssetCreation(ref stream, i);
                break;

            case ObjectChangeKind.DestroyAssetObject:
                HandleAssetDestruction(ref stream, i);
                break;
        }
    }
}
```

**3. 处理属性变化**
```csharp
static void HandleAssetPropertyChange(ref ObjectChangeEventStream stream, int index)
{
    stream.GetChangeAssetObjectPropertiesEvent(index, out var evt);
    Object obj = EditorUtility.InstanceIDToObject(evt.instanceId);

    if (obj == null) return;

    string mode = IsMemoryObject(obj) ? "[内存模式]" : "[磁盘模式]";

    if (obj is TimelineAsset timeline)
    {
        Debug.Log($"【⚙ Timeline 属性变化】 {timeline.name} {mode}");
    }
    else if (obj is TrackAsset track)
    {
        Debug.Log($"【⚙ 轨道属性变化】 {track.name} (类型: {track.GetType().Name}) {mode}");

        // 检查 Clip 变化
        CheckClipChanges(track);
    }
    else if (obj is PlayableAsset asset)
    {
        Debug.Log($"【⚙ PlayableAsset 属性变化】 {asset.name} {mode}");
    }
}
```

**4. 处理对象创建**
```csharp
static void HandleAssetCreation(ref ObjectChangeEventStream stream, int index)
{
    stream.GetCreateAssetObjectEvent(index, out var evt);
    Object obj = EditorUtility.InstanceIDToObject(evt.instanceId);

    if (obj == null) return;

    string mode = IsMemoryObject(obj) ? "[内存模式]" : "[磁盘模式]";

    if (obj is TrackAsset track)
    {
        Debug.Log($"【✚ 轨道创建】 {track.name} (类型: {track.GetType().Name}) {mode}");
    }
    else if (obj is PlayableAsset asset)
    {
        Debug.Log($"【✚ Clip创建】 {asset.name} {mode}");
    }
}
```

**5. 处理对象销毁**
```csharp
static void HandleAssetDestruction(ref ObjectChangeEventStream stream, int index)
{
    stream.GetDestroyAssetObjectEvent(index, out var evt);

    // 注意：对象已被销毁，无法获取详细信息
    Debug.Log($"【✖ 对象删除】 InstanceID: {evt.instanceId}");
}
```

**6. Clip 变化检测**
```csharp
// 缓存结构
class ClipInfo
{
    public double start;
    public double duration;
    public double timeScale;
    public double clipIn;

    public void UpdateValues(TimelineClip clip)
    {
        start = clip.start;
        duration = clip.duration;
        timeScale = clip.timeScale;
        clipIn = clip.clipIn;
    }
}

static Dictionary<TimelineClip, ClipInfo> clipCache = new Dictionary<TimelineClip, ClipInfo>();

static void CheckClipChanges(TrackAsset track)
{
    foreach (var clip in track.GetClips())
    {
        if (!clipCache.ContainsKey(clip))
        {
            // 新 Clip，添加到缓存
            clipCache[clip] = new ClipInfo();
            clipCache[clip].UpdateValues(clip);
        }
        else
        {
            // 检查是否有变化
            var cachedInfo = clipCache[clip];
            bool hasChange = false;

            if (cachedInfo.start != clip.start)
            {
                Debug.Log($"  【⚡ Clip属性变化】 Start: {cachedInfo.start:F3} → {clip.start:F3}");
                hasChange = true;
            }

            if (cachedInfo.duration != clip.duration)
            {
                Debug.Log($"  【⚡ Clip属性变化】 Duration: {cachedInfo.duration:F3} → {clip.duration:F3}");
                hasChange = true;
            }

            if (cachedInfo.timeScale != clip.timeScale)
            {
                Debug.Log($"  【⚡ Clip属性变化】 TimeScale: {cachedInfo.timeScale:F3} → {clip.timeScale:F3}");
                hasChange = true;
            }

            if (cachedInfo.clipIn != clip.clipIn)
            {
                Debug.Log($"  【⚡ Clip属性变化】 ClipIn: {cachedInfo.clipIn:F3} → {clip.clipIn:F3}");
                hasChange = true;
            }

            if (hasChange)
            {
                cachedInfo.UpdateValues(clip);
            }
        }
    }
}
```

**7. 撤销/重做监听**
```csharp
static void OnUndoRedoPerformed()
{
    Debug.Log("【↶ 撤销/重做】 执行了撤销或重做操作");
}
```

---

### 模块3：TimelineSaveInterceptor（保存拦截）

#### 完整实现

```csharp
using UnityEditor;
using UnityEngine;

public class TimelineSaveInterceptor : UnityEditor.AssetModificationProcessor
{
    /// <summary>
    /// Unity 保存资源前的回调
    /// </summary>
    static string[] OnWillSaveAssets(string[] paths)
    {
        List<string> filteredPaths = new List<string>();

        foreach (var path in paths)
        {
            // 加载资源
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (asset == null)
            {
                filteredPaths.Add(path);
                continue;
            }

            // 检查是否是内存对象
            if (IsMemoryObject(asset))
            {
                Debug.LogWarning($"[保存拦截] 内存对象不应保存到磁盘: {path}");
                continue;  // 跳过此路径，不保存
            }

            // 检查是否包含内存子对象
            if (ContainsMemoryObjects(asset))
            {
                Debug.LogWarning($"[保存拦截] 资源包含内存对象: {path}");
                continue;
            }

            filteredPaths.Add(path);
        }

        return filteredPaths.ToArray();
    }

    /// <summary>
    /// 检查是否是内存对象
    /// </summary>
    static bool IsMemoryObject(Object obj)
    {
        if (obj == null) return false;

        // 检查 HideFlags
        if ((obj.hideFlags & HideFlags.DontSave) != 0)
            return true;

        // 检查名称
        if (obj.name.Contains("(Memory)"))
            return true;

        return false;
    }

    /// <summary>
    /// 检查资源是否包含内存子对象
    /// </summary>
    static bool ContainsMemoryObjects(Object asset)
    {
        if (asset is TimelineAsset timeline)
        {
            // 检查所有轨道
            foreach (var track in timeline.GetRootTracks())
            {
                if (IsMemoryObject(track))
                    return true;

                // 检查 Clip
                foreach (var clip in track.GetClips())
                {
                    if (clip.asset != null && IsMemoryObject(clip.asset))
                        return true;
                }
            }
        }

        return false;
    }
}
```

---

### 模块4：TimelineMemoryEditor（会话管理）

#### 核心数据结构

**1. 会话类**
```csharp
public class TimelineMemorySession
{
    public TimelineAsset originalAsset;      // 原始磁盘资源
    public TimelineAsset memoryAsset;        // 内存版本
    public PlayableDirector director;        // 用于预览的 Director
    public bool isDirty;                     // 是否有修改
    public int changeCount;                  // 修改次数

    public TimelineMemorySession(TimelineAsset original, TimelineAsset memory)
    {
        originalAsset = original;
        memoryAsset = memory;
        isDirty = false;
        changeCount = 0;
    }

    /// <summary>
    /// 监听变化事件
    /// </summary>
    public void OnChangesPublished(ref ObjectChangeEventStream stream)
    {
        for (int i = 0; i < stream.length; i++)
        {
            var eventType = stream.GetEventType(i);

            if (eventType == ObjectChangeKind.ChangeAssetObjectProperties)
            {
                stream.GetChangeAssetObjectPropertiesEvent(i, out var evt);
                Object obj = EditorUtility.InstanceIDToObject(evt.instanceId);

                // 检查是否与此会话相关
                if (IsRelatedToSession(obj))
                {
                    isDirty = true;
                    changeCount++;
                }
            }
        }
    }

    bool IsRelatedToSession(Object obj)
    {
        if (obj == memoryAsset) return true;

        if (obj is TrackAsset track)
        {
            return track.timelineAsset == memoryAsset;
        }

        return false;
    }
}
```

**2. 编辑器窗口**
```csharp
public class TimelineMemoryEditor : EditorWindow
{
    // 会话管理
    static Dictionary<TimelineAsset, TimelineMemorySession> activeSessions
        = new Dictionary<TimelineAsset, TimelineMemorySession>();

    TimelineAsset selectedTimeline;
    Vector2 scrollPosition;

    [MenuItem("Window/Timeline Memory Editor")]
    public static void ShowWindow()
    {
        GetWindow<TimelineMemoryEditor>("Timeline Memory Editor");
    }

    /// <summary>
    /// 打开内存模式
    /// </summary>
    public static TimelineMemorySession OpenInMemoryMode(TimelineAsset original)
    {
        if (original == null)
        {
            Debug.LogError("Timeline 资源为空");
            return null;
        }

        // 检查是否已有会话
        if (activeSessions.ContainsKey(original))
        {
            Debug.LogWarning("此 Timeline 已在内存模式中打开");
            return activeSessions[original];
        }

        // 1. 深度克隆
        TimelineAsset memoryVersion = TimelineCloner.DeepClone(original);

        // 2. 创建会话
        var session = new TimelineMemorySession(original, memoryVersion);
        activeSessions[original] = session;

        // 3. 订阅变化事件
        ObjectChangeEvents.changesPublished += session.OnChangesPublished;

        // 4. 在 Timeline 窗口中打开
        OpenInTimelineWindow(session);

        Debug.Log($"【Timeline 克隆完成】 {original.name}");

        return session;
    }

    /// <summary>
    /// 在 Timeline 窗口中打开
    /// </summary>
    static void OpenInTimelineWindow(TimelineMemorySession session)
    {
        // 创建临时 GameObject 和 PlayableDirector
        GameObject go = new GameObject("Timeline Memory Director");
        go.hideFlags = HideFlags.HideAndDontSave;

        PlayableDirector director = go.AddComponent<PlayableDirector>();
        director.playableAsset = session.memoryAsset;

        session.director = director;

        // 选中 Director，Timeline 窗口会自动打开
        Selection.activeGameObject = go;

        // 打开 Timeline 窗口
        EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");

        Debug.Log("【Timeline 窗口已打开】 正在编辑内存版本");
    }

    /// <summary>
    /// 保存会话
    /// </summary>
    public static void SaveSession(TimelineMemorySession session)
    {
        TimelineCloner.SaveToOriginal(session.memoryAsset, session.originalAsset);
        session.isDirty = false;
        session.changeCount = 0;
    }

    /// <summary>
    /// 关闭会话
    /// </summary>
    public static void CloseSession(TimelineMemorySession session)
    {
        // 取消订阅
        ObjectChangeEvents.changesPublished -= session.OnChangesPublished;

        // 销毁内存对象
        TimelineCloner.DestroyMemoryVersion(session.memoryAsset);

        // 销毁 Director
        if (session.director != null)
        {
            DestroyImmediate(session.director.gameObject);
        }

        // 移除会话
        activeSessions.Remove(session.originalAsset);

        Debug.Log($"【会话已关闭】 {session.originalAsset.name}");
    }

    /// <summary>
    /// GUI 绘制
    /// </summary>
    void OnGUI()
    {
        GUILayout.Label("Timeline 内存编辑器", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // 选择 Timeline
        selectedTimeline = EditorGUILayout.ObjectField("Timeline 资源",
            selectedTimeline, typeof(TimelineAsset), false) as TimelineAsset;

        if (selectedTimeline != null)
        {
            if (GUILayout.Button("打开（内存模式）"))
            {
                OpenInMemoryMode(selectedTimeline);
            }
        }

        EditorGUILayout.Space();
        GUILayout.Label("活动会话", EditorStyles.boldLabel);

        // 显示所有会话
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var kvp in activeSessions.ToList())
        {
            var session = kvp.Value;

            EditorGUILayout.BeginVertical("box");

            GUILayout.Label(session.originalAsset.name, EditorStyles.boldLabel);
            GUILayout.Label($"修改次数: {session.changeCount}");
            GUILayout.Label($"状态: {(session.isDirty ? "已修改" : "未修改")}");

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("💾 保存到磁盘"))
            {
                SaveSession(session);
            }

            if (GUILayout.Button("✖ 放弃修改"))
            {
                if (EditorUtility.DisplayDialog("确认",
                    "确定要放弃所有修改吗？", "确定", "取消"))
                {
                    CloseSession(session);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }
}
```

---

## 关键技术难点

### 难点1：TimelineClip 的只读属性

**问题：**
`TimelineClip` 的某些属性是只读的，如：
- `preExtrapolationMode`
- `postExtrapolationMode`
- `asset`（clip 的 PlayableAsset）

**解决方案：**
使用 `SerializedObject` 访问底层序列化字段：

```csharp
SerializedObject trackObj = new SerializedObject(targetTrack);
SerializedProperty clipsProp = trackObj.FindProperty("m_Clips");

// 访问数组中的 clip
SerializedProperty clipProp = clipsProp.GetArrayElementAtIndex(index);

// 访问只读属性的底层字段
SerializedProperty assetProp = clipProp.FindPropertyRelative("m_Asset");
assetProp.objectReferenceValue = newAsset;

// 应用修改（不触发 Undo）
trackObj.ApplyModifiedPropertiesWithoutUndo();
```

**关键点：**
- 使用 `m_` 前缀访问私有字段
- 使用 `ApplyModifiedPropertiesWithoutUndo()` 避免触发 Undo 系统

---

### 难点2：TimelineClip 不是 Object 子类

**问题：**
`TimelineClip` 是结构体（struct），不继承自 `Object`，无法直接监听其变化。

**解决方案：**
通过监听 `TrackAsset` 的属性变化，间接检测 Clip 变化：

```csharp
// 1. 缓存 Clip 信息
Dictionary<TimelineClip, ClipInfo> clipCache;

// 2. 当 TrackAsset 属性变化时，检查所有 Clip
void HandleTrackChange(TrackAsset track)
{
    foreach (var clip in track.GetClips())
    {
        if (!clipCache.ContainsKey(clip))
        {
            // 新 Clip
            clipCache[clip] = new ClipInfo(clip);
        }
        else
        {
            // 对比缓存，检测变化
            var cached = clipCache[clip];
            if (cached.start != clip.start ||
                cached.duration != clip.duration)
            {
                // 检测到变化
                LogChange(clip, cached);
                cached.UpdateValues(clip);
            }
        }
    }
}
```

**注意事项：**
- Clip 是值类型，不能用作 Dictionary 的 key（会导致装箱）
- 实际实现中需要使用其他方式标识 Clip（如 asset 引用）

---

### 难点3：CreateClip API 的限制

**问题：**
`TrackAsset.CreateClip<T>()` 是泛型方法，需要在编译时确定类型。

**解决方案：**
使用 `CreateDefaultClip()` 创建 Clip，然后通过 `SerializedObject` 设置 asset：

```csharp
// 1. 创建默认 Clip
TimelineClip clip = targetTrack.CreateDefaultClip();

// 2. 克隆 PlayableAsset
PlayableAsset clonedAsset = ClonePlayableAsset(original.asset);

// 3. 使用 SerializedObject 设置 asset
SerializedObject trackObj = new SerializedObject(targetTrack);
SerializedProperty clipsProp = trackObj.FindProperty("m_Clips");
int lastIndex = clipsProp.arraySize - 1;
SerializedProperty lastClip = clipsProp.GetArrayElementAtIndex(lastIndex);
SerializedProperty assetProp = lastClip.FindPropertyRelative("m_Asset");
assetProp.objectReferenceValue = clonedAsset;
trackObj.ApplyModifiedPropertiesWithoutUndo();
```

---

### 难点4：引用关系的维护

**问题：**
Timeline 内部有复杂的引用关系：
- Track 引用 Timeline
- Clip 引用 Track
- PlayableAsset 可能引用外部资源

**解决方案：**
使用 `Dictionary<Object, Object>` 维护克隆映射：

```csharp
Dictionary<Object, Object> cloneMap = new Dictionary<Object, Object>();

// 克隆时记录映射
TrackAsset clonedTrack = CloneTrack(originalTrack);
cloneMap[originalTrack] = clonedTrack;

// 后续可以通过映射找到克隆对象
TrackAsset clone = cloneMap[original] as TrackAsset;
```

---

## 完整实现流程

### 流程图

```
用户操作
   │
   ▼
选择 Timeline 资源
   │
   ▼
点击"打开（内存模式）"
   │
   ├─────────────────────────────────┐
   │                                 │
   ▼                                 ▼
TimelineCloner.DeepClone()    创建 TimelineMemorySession
   │                                 │
   ├─ 创建内存对象                    ├─ 订阅 ObjectChangeEvents
   ├─ 克隆属性                        └─ 创建 PlayableDirector
   ├─ 递归克隆轨道
   └─ 递归克隆 Clip
   │                                 │
   └─────────────────┬───────────────┘
                     │
                     ▼
            打开 Timeline 窗口
                     │
                     ▼
            用户编辑 Timeline
                     │
        ┌────────────┼────────────┐
        │            │            │
        ▼            ▼            ▼
    添加轨道      修改Clip      删除对象
        │            │            │
        └────────────┼────────────┘
                     │
                     ▼
    ObjectChangeEvents.changesPublished
                     │
        ┌────────────┼────────────┐
        │            │            │
        ▼            ▼            ▼
   CreateEvent  ChangeEvent  DestroyEvent
        │            │            │
        └────────────┼────────────┘
                     │
                     ▼
         TimelineChangeMonitor
                     │
        ┌────────────┼────────────┐
        │            │            │
        ▼            ▼            ▼
    识别对象类型  检测变化    输出日志
        │            │            │
        └────────────┼────────────┘
                     │
                     ▼
         更新 Session 状态
         (isDirty, changeCount)
                     │
                     ▼
            用户决定保存或放弃
                     │
        ┌────────────┼────────────┐
        │                         │
        ▼                         ▼
    保存到磁盘                 放弃修改
        │                         │
        ▼                         ▼
SaveToOriginal()          DestroyMemoryVersion()
        │                         │
        ├─ 清空原始Timeline        ├─ 销毁内存对象
        ├─ 克隆内存版本            ├─ 销毁Director
        ├─ 标记为脏                └─ 移除会话
        └─ AssetDatabase.SaveAssets()
```

### 关键步骤详解

#### 步骤1：初始化系统

```csharp
// 1. 监听器自动启动（InitializeOnLoad）
[InitializeOnLoad]
public class TimelineChangeMonitor
{
    static TimelineChangeMonitor()
    {
        ObjectChangeEvents.changesPublished += OnChangesPublished;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
    }
}

// 2. 保存拦截器自动注册
public class TimelineSaveInterceptor : AssetModificationProcessor
{
    static string[] OnWillSaveAssets(string[] paths)
    {
        // 自动拦截保存操作
    }
}
```

#### 步骤2：打开内存模式

```csharp
// 用户点击按钮
TimelineMemoryEditor.OpenInMemoryMode(selectedTimeline);

// 内部流程：
// 1. 深度克隆
TimelineAsset memory = TimelineCloner.DeepClone(original);

// 2. 创建会话
var session = new TimelineMemorySession(original, memory);

// 3. 订阅事件
ObjectChangeEvents.changesPublished += session.OnChangesPublished;

// 4. 打开 Timeline 窗口
GameObject go = new GameObject("Timeline Memory Director");
PlayableDirector director = go.AddComponent<PlayableDirector>();
director.playableAsset = memory;
Selection.activeGameObject = go;
```

#### 步骤3：监听编辑操作

```csharp
// 用户编辑 Timeline
// ↓
// Unity 触发 ObjectChangeEvents
// ↓
void OnChangesPublished(ref ObjectChangeEventStream stream)
{
    // 遍历所有变化
    for (int i = 0; i < stream.length; i++)
    {
        var eventType = stream.GetEventType(i);

        // 根据类型处理
        switch (eventType)
        {
            case ObjectChangeKind.CreateAssetObject:
                // 对象创建（添加轨道/Clip）
                break;
            case ObjectChangeKind.ChangeAssetObjectProperties:
                // 属性变化（修改任何属性）
                break;
            case ObjectChangeKind.DestroyAssetObject:
                // 对象销毁（删除轨道/Clip）
                break;
        }
    }
}
```

#### 步骤4：保存或放弃

```csharp
// 保存
TimelineCloner.SaveToOriginal(session.memoryAsset, session.originalAsset);
// ↓
// 1. 清空原始 Timeline
// 2. 克隆内存版本到原始对象
// 3. 标记为脏
// 4. AssetDatabase.SaveAssets()

// 放弃
TimelineMemoryEditor.CloseSession(session);
// ↓
// 1. 取消事件订阅
// 2. 销毁内存对象
// 3. 销毁 Director
// 4. 移除会话
```

---

## 总结

### 核心技术点

1. **内存对象创建**
   - `ScriptableObject.CreateInstance<T>()`
   - `HideFlags.DontSave`

2. **深度克隆**
   - `JsonUtility` 序列化/反序列化
   - `SerializedObject` 访问私有字段
   - 递归克隆层级结构

3. **变化监听**
   - `ObjectChangeEvents.changesPublished`
   - 事件流处理
   - 缓存对比检测变化

4. **保存拦截**
   - `AssetModificationProcessor.OnWillSaveAssets`
   - 过滤内存对象

5. **会话管理**
   - `Dictionary` 管理多个会话
   - `PlayableDirector` 预览
   - 事件订阅/取消订阅

### 实现要点

1. **使用 `[InitializeOnLoad]`** 确保监听器自动启动
2. **使用 `SerializedObject`** 访问只读属性
3. **使用 `ApplyModifiedPropertiesWithoutUndo()`** 避免触发 Undo
4. **维护克隆映射表** 处理引用关系
5. **双重保护** 防止自动保存（HideFlags + Interceptor）
6. **缓存对比** 检测 Clip 变化
7. **清理资源** 关闭会话时销毁所有临时对象

### 扩展建议

1. **支持更多 Track 类型**
   - 为自定义 Track 添加特殊处理
   - 处理特殊的引用关系

2. **优化性能**
   - 大型 Timeline 的克隆优化
   - 变化检测的性能优化

3. **增强 UI**
   - 显示详细的变化历史
   - 支持部分保存（只保存某些轨道）

4. **错误处理**
   - 克隆失败的回滚机制
   - 保存冲突的处理

---

## 参考资料

- [Unity Timeline API 文档](https://docs.unity3d.com/Packages/com.unity.timeline@latest)
- [ObjectChangeEvents API](https://docs.unity3d.com/ScriptReference/ObjectChangeEvents.html)
- [SerializedObject API](https://docs.unity3d.com/ScriptReference/SerializedObject.html)
- [AssetModificationProcessor API](https://docs.unity3d.com/ScriptReference/AssetModificationProcessor.html)

