using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Timeline 变化监听器
/// 监听 Timeline 中的所有操作，包括轨道和 Clip 的增删改
/// 不管是否在预览播放模式下都能正常工作
/// </summary>
[InitializeOnLoad]
public class TimelineChangeMonitor
{
    // 用于缓存 Clip 信息，检测 Clip 属性变化
    private static Dictionary<int, ClipCacheInfo> clipCache = new Dictionary<int, ClipCacheInfo>();
    
    // 用于缓存 Track 信息
    private static Dictionary<int, TrackCacheInfo> trackCache = new Dictionary<int, TrackCacheInfo>();

    static TimelineChangeMonitor()
    {
        // 订阅对象变化事件
        ObjectChangeEvents.changesPublished += OnChangesPublished;
        
        // 订阅撤销/重做事件
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        
        Debug.Log("<color=cyan>═══════════════════════════════════════════════════</color>");
        Debug.Log("<color=cyan>【Timeline 监听器已启动】</color>");
        Debug.Log("<color=cyan>监听范围：轨道增删改、Clip增删改、属性变化</color>");
        Debug.Log("<color=cyan>工作模式：预览和非预览模式下都可监听</color>");
        Debug.Log("<color=cyan>═══════════════════════════════════════════════════</color>");
    }

    /// <summary>
    /// 对象变化事件处理
    /// </summary>
    static void OnChangesPublished(ref ObjectChangeEventStream stream)
    {
        for (int i = 0; i < stream.length; i++)
        {
            var eventType = stream.GetEventType(i);
            
            switch (eventType)
            {
                // 监听资产对象属性变化（包括Timeline、Track、Clip的属性修改）
                case ObjectChangeKind.ChangeAssetObjectProperties:
                    HandleAssetPropertyChange(ref stream, i);
                    break;
                
                // 监听资产对象创建（新增Track或Clip）
                case ObjectChangeKind.CreateAssetObject:
                    HandleAssetCreation(ref stream, i);
                    break;
                
                // 监听资产对象删除（删除Track或Clip）
                case ObjectChangeKind.DestroyAssetObject:
                    HandleAssetDestruction(ref stream, i);
                    break;
            }
        }
    }

    /// <summary>
    /// 处理资产属性变化
    /// </summary>
    static void HandleAssetPropertyChange(ref ObjectChangeEventStream stream, int index)
    {
        stream.GetChangeAssetObjectPropertiesEvent(index, out var changeEvent);
        var changedObject = EditorUtility.InstanceIDToObject(changeEvent.instanceId);
        
        if (changedObject == null) return;

        // 检查是否是Timeline相关对象
        if (changedObject is TimelineAsset timeline)
        {
            OnTimelineAssetChanged(timeline);
        }
        else if (changedObject is TrackAsset track)
        {
            OnTrackAssetChanged(track);
        }
        else if (changedObject is PlayableAsset playableAsset)
        {
            OnPlayableAssetChanged(playableAsset);
        }
    }

    /// <summary>
    /// 处理资产创建
    /// </summary>
    static void HandleAssetCreation(ref ObjectChangeEventStream stream, int index)
    {
        stream.GetCreateAssetObjectEvent(index, out var createEvent);
        var createdObject = EditorUtility.InstanceIDToObject(createEvent.instanceId);
        
        if (createdObject == null) return;

        if (createdObject is TrackAsset createdTrack)
        {
            Debug.Log($"<color=green>【✚ 轨道创建】</color> {createdTrack.name} (类型: <color=yellow>{createdTrack.GetType().Name}</color>)");
            CacheTrackInfo(createdTrack);
        }
        else if (createdObject is PlayableAsset createdClip)
        {
            Debug.Log($"<color=green>【✚ Clip创建】</color> {createdClip.name} (类型: <color=yellow>{createdClip.GetType().Name}</color>)");
        }
    }

    /// <summary>
    /// 处理资产删除
    /// </summary>
    static void HandleAssetDestruction(ref ObjectChangeEventStream stream, int index)
    {
        stream.GetDestroyAssetObjectEvent(index, out var destroyEvent);
        int instanceId = destroyEvent.instanceId;
        
        // 检查缓存中是否有这个对象的信息
        if (trackCache.ContainsKey(instanceId))
        {
            var trackInfo = trackCache[instanceId];
            Debug.Log($"<color=red>【✖ 轨道删除】</color> {trackInfo.name} (类型: <color=yellow>{trackInfo.typeName}</color>)");
            trackCache.Remove(instanceId);
        }
        else if (clipCache.ContainsKey(instanceId))
        {
            var clipInfo = clipCache[instanceId];
            Debug.Log($"<color=red>【✖ Clip删除】</color> InstanceID: {instanceId}");
            clipCache.Remove(instanceId);
        }
        else
        {
            Debug.Log($"<color=red>【✖ Timeline对象删除】</color> InstanceID: {instanceId}");
        }
    }

    /// <summary>
    /// Timeline资产变化处理
    /// </summary>
    static void OnTimelineAssetChanged(TimelineAsset timeline)
    {
        string mode = IsMemoryObject(timeline) ? "[内存模式]" : "[磁盘模式]";
        Debug.Log($"<color=cyan>【⚙ Timeline属性变化】</color> {timeline.name} {mode}");
        Debug.Log($"  ├─ Duration: <color=yellow>{timeline.duration:F3}s</color>");
        Debug.Log($"  ├─ Track数量: <color=yellow>{timeline.outputTrackCount}</color>");
        Debug.Log($"  └─ 根Track数量: <color=yellow>{timeline.rootTrackCount}</color>");
    }

    /// <summary>
    /// Track资产变化处理
    /// </summary>
    static void OnTrackAssetChanged(TrackAsset track)
    {
        string mode = IsMemoryObject(track) ? "[内存模式]" : "[磁盘模式]";
        Debug.Log($"<color=magenta>【⚙ 轨道属性变化】</color> {track.name} (类型: <color=yellow>{track.GetType().Name}</color>) {mode}");
        Debug.Log($"  ├─ Muted: <color=yellow>{track.muted}</color>");
        Debug.Log($"  ├─ Locked: <color=yellow>{track.locked}</color>");

        var clips = track.GetClips().ToList();
        Debug.Log($"  └─ Clip数量: <color=yellow>{clips.Count}</color>");

        // 检查Clip的变化
        CheckClipChanges(track, clips);

        // 更新Track缓存
        CacheTrackInfo(track);
    }

    /// <summary>
    /// PlayableAsset变化处理（Clip的资产）
    /// </summary>
    static void OnPlayableAssetChanged(PlayableAsset playableAsset)
    {
        Debug.Log($"<color=orange>【⚙ Clip资产属性变化】</color> {playableAsset.name} (类型: <color=yellow>{playableAsset.GetType().Name}</color>)");
        
        // 使用SerializedObject获取属性详情
        var serializedObject = new SerializedObject(playableAsset);
        serializedObject.Update();
        
        var iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        int propertyCount = 0;
        
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            // 跳过脚本引用
            if (iterator.propertyPath == "m_Script") continue;
            
            propertyCount++;
            string valueStr = GetPropertyValueString(iterator);
            Debug.Log($"      ├─ {iterator.propertyPath}: <color=yellow>{valueStr}</color>");
            
            if (propertyCount >= 10) // 限制输出数量
            {
                Debug.Log($"      └─ ... (更多属性)");
                break;
            }
        }
    }

    /// <summary>
    /// 检查Clip的变化
    /// </summary>
    static void CheckClipChanges(TrackAsset track, List<TimelineClip> clips)
    {
        foreach (var clip in clips)
        {
            int clipHash = clip.GetHashCode();
            
            if (clipCache.TryGetValue(clipHash, out var cachedInfo))
            {
                // 检查属性是否变化
                bool hasChanged = false;
                List<string> changes = new List<string>();
                
                if (cachedInfo.start != clip.start)
                {
                    changes.Add($"Start: {cachedInfo.start:F3} → {clip.start:F3}");
                    hasChanged = true;
                }
                
                if (cachedInfo.duration != clip.duration)
                {
                    changes.Add($"Duration: {cachedInfo.duration:F3} → {clip.duration:F3}");
                    hasChanged = true;
                }
                
                if (cachedInfo.timeScale != clip.timeScale)
                {
                    changes.Add($"TimeScale: {cachedInfo.timeScale:F3} → {clip.timeScale:F3}");
                    hasChanged = true;
                }
                
                if (cachedInfo.clipIn != clip.clipIn)
                {
                    changes.Add($"ClipIn: {cachedInfo.clipIn:F3} → {clip.clipIn:F3}");
                    hasChanged = true;
                }
                
                if (hasChanged)
                {
                    Debug.Log($"    <color=lime>【⚡ Clip属性变化】</color> '{clip.displayName}'");
                    foreach (var change in changes)
                    {
                        Debug.Log($"        ├─ {change}");
                    }
                }
                
                // 更新缓存
                cachedInfo.UpdateFrom(clip);
            }
            else
            {
                // 新Clip，添加到缓存
                clipCache[clipHash] = new ClipCacheInfo(clip);
            }
        }
    }

    /// <summary>
    /// 缓存Track信息
    /// </summary>
    static void CacheTrackInfo(TrackAsset track)
    {
        int instanceId = track.GetInstanceID();
        if (!trackCache.ContainsKey(instanceId))
        {
            trackCache[instanceId] = new TrackCacheInfo(track);
        }
        else
        {
            trackCache[instanceId].UpdateFrom(track);
        }
    }

    /// <summary>
    /// 撤销/重做事件处理
    /// </summary>
    static void OnUndoRedoPerformed()
    {
        Debug.Log("<color=yellow>【↶ 撤销/重做】</color> 执行了撤销或重做操作");
    }

    /// <summary>
    /// 获取属性值的字符串表示
    /// </summary>
    static string GetPropertyValueString(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                return property.intValue.ToString();
            case SerializedPropertyType.Boolean:
                return property.boolValue.ToString();
            case SerializedPropertyType.Float:
                return property.floatValue.ToString("F3");
            case SerializedPropertyType.String:
                return $"\"{property.stringValue}\"";
            case SerializedPropertyType.Enum:
                return property.enumNames[property.enumValueIndex];
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue != null ? property.objectReferenceValue.name : "null";
            default:
                return property.propertyType.ToString();
        }
    }

    /// <summary>
    /// 检查对象是否是内存对象
    /// </summary>
    static bool IsMemoryObject(Object obj)
    {
        if (obj == null) return false;
        return (obj.hideFlags & HideFlags.DontSave) != 0 || obj.name.Contains("(Memory)");
    }

    #region 缓存数据结构

    /// <summary>
    /// Clip缓存信息
    /// </summary>
    class ClipCacheInfo
    {
        public double start;
        public double duration;
        public double timeScale;
        public double clipIn;
        public string displayName;

        public ClipCacheInfo(TimelineClip clip)
        {
            UpdateFrom(clip);
        }

        public void UpdateFrom(TimelineClip clip)
        {
            start = clip.start;
            duration = clip.duration;
            timeScale = clip.timeScale;
            clipIn = clip.clipIn;
            displayName = clip.displayName;
        }
    }

    /// <summary>
    /// Track缓存信息
    /// </summary>
    class TrackCacheInfo
    {
        public string name;
        public string typeName;
        public bool muted;
        public bool locked;

        public TrackCacheInfo(TrackAsset track)
        {
            UpdateFrom(track);
        }

        public void UpdateFrom(TrackAsset track)
        {
            name = track.name;
            typeName = track.GetType().Name;
            muted = track.muted;
            locked = track.locked;
        }
    }

    #endregion
}

