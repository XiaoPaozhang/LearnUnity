using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Timeline 深度克隆工具
/// 创建完全独立的内存版本，不关联磁盘文件
/// </summary>
public static class TimelineCloner
{
    /// <summary>
    /// 深度克隆 TimelineAsset 及其所有子对象
    /// </summary>
    /// <param name="original">原始 Timeline 资源</param>
    /// <returns>内存版本的克隆对象</returns>
    public static TimelineAsset DeepClone(TimelineAsset original)
    {
        if (original == null)
        {
            Debug.LogError("无法克隆空的 TimelineAsset");
            return null;
        }

        // 创建内存版本的 TimelineAsset
        TimelineAsset clone = ScriptableObject.CreateInstance<TimelineAsset>();
        clone.name = original.name + " (Memory)";
        
        // 设置为不保存到磁盘
        clone.hideFlags = HideFlags.DontSave;

        // 用于映射原始对象到克隆对象的字典
        Dictionary<Object, Object> cloneMap = new Dictionary<Object, Object>();
        cloneMap[original] = clone;

        // 克隆基本属性
        CloneTimelineProperties(original, clone);

        // 克隆所有根轨道
        List<TrackAsset> clonedTracks = new List<TrackAsset>();
        foreach (var track in original.GetRootTracks())
        {
            var clonedTrack = CloneTrack(track, clone, cloneMap);
            if (clonedTrack != null)
            {
                clonedTracks.Add(clonedTrack);
            }
        }

        Debug.Log($"<color=cyan>【Timeline 克隆完成】</color> {original.name}");
        Debug.Log($"  ├─ 克隆了 {clonedTracks.Count} 个根轨道");
        Debug.Log($"  └─ 内存对象已创建，不会自动保存到磁盘");

        return clone;
    }

    /// <summary>
    /// 克隆 Timeline 的基本属性
    /// </summary>
    static void CloneTimelineProperties(TimelineAsset source, TimelineAsset target)
    {
        // 使用 SerializedObject 复制属性
        SerializedObject sourceObj = new SerializedObject(source);
        SerializedObject targetObj = new SerializedObject(target);

        // 复制 duration mode
        var durationModeProp = sourceObj.FindProperty("m_DurationMode");
        if (durationModeProp != null)
        {
            targetObj.FindProperty("m_DurationMode").enumValueIndex = durationModeProp.enumValueIndex;
        }

        // 复制 fixed duration
        var fixedDurationProp = sourceObj.FindProperty("m_FixedDuration");
        if (fixedDurationProp != null)
        {
            targetObj.FindProperty("m_FixedDuration").doubleValue = fixedDurationProp.doubleValue;
        }

        // 复制 editor settings
        var editorSettingsProp = sourceObj.FindProperty("m_EditorSettings");
        if (editorSettingsProp != null)
        {
            var targetEditorSettings = targetObj.FindProperty("m_EditorSettings");
            targetEditorSettings.FindPropertyRelative("m_Framerate").doubleValue = 
                editorSettingsProp.FindPropertyRelative("m_Framerate").doubleValue;
            targetEditorSettings.FindPropertyRelative("m_ScenePreview").boolValue = 
                editorSettingsProp.FindPropertyRelative("m_ScenePreview").boolValue;
        }

        targetObj.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 克隆轨道（递归处理子轨道）
    /// </summary>
    static TrackAsset CloneTrack(TrackAsset original, TimelineAsset timeline, Dictionary<Object, Object> cloneMap)
    {
        if (original == null) return null;

        // 创建对应类型的轨道实例
        TrackAsset clone = timeline.CreateTrack(original.GetType(), null, original.name);
        clone.hideFlags = HideFlags.DontSave;
        
        cloneMap[original] = clone;

        // 克隆轨道的基本属性
        CloneTrackProperties(original, clone);

        // 克隆所有 Clip
        foreach (var originalClip in original.GetClips())
        {
            CloneClip(originalClip, clone, cloneMap);
        }

        // 递归克隆子轨道
        foreach (var childTrack in original.GetChildTracks())
        {
            CloneTrack(childTrack, timeline, cloneMap);
        }

        return clone;
    }

    /// <summary>
    /// 克隆轨道属性
    /// </summary>
    static void CloneTrackProperties(TrackAsset source, TrackAsset target)
    {
        SerializedObject sourceObj = new SerializedObject(source);
        SerializedObject targetObj = new SerializedObject(target);

        // 复制 muted
        target.muted = source.muted;
        
        // 复制 locked
        var lockedProp = sourceObj.FindProperty("m_Locked");
        if (lockedProp != null)
        {
            targetObj.FindProperty("m_Locked").boolValue = lockedProp.boolValue;
        }

        targetObj.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 克隆 Clip
    /// </summary>
    static void CloneClip(TimelineClip original, TrackAsset targetTrack, Dictionary<Object, Object> cloneMap)
    {
        // 先创建默认 Clip
        TimelineClip clip = targetTrack.CreateDefaultClip();

        // 克隆 PlayableAsset
        if (original.asset != null)
        {
            // 将 Object 转换为 PlayableAsset
            PlayableAsset originalAsset = original.asset as PlayableAsset;
            if (originalAsset != null)
            {
                PlayableAsset clonedAsset = ClonePlayableAsset(originalAsset);
                cloneMap[original.asset] = clonedAsset;

                // 使用 SerializedObject 设置 asset
                SerializedObject trackObj = new SerializedObject(targetTrack);
                SerializedProperty clipsArrayProp = trackObj.FindProperty("m_Clips");

                if (clipsArrayProp != null && clipsArrayProp.isArray && clipsArrayProp.arraySize > 0)
                {
                    // 获取最后一个 clip（刚创建的）
                    SerializedProperty lastClipProp = clipsArrayProp.GetArrayElementAtIndex(clipsArrayProp.arraySize - 1);
                    SerializedProperty assetProp = lastClipProp.FindPropertyRelative("m_Asset");
                    if (assetProp != null)
                    {
                        assetProp.objectReferenceValue = clonedAsset;
                    }
                }
                trackObj.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 复制 Clip 属性
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

        // 使用 SerializedObject 复制只读属性
        SerializedObject clipObj = new SerializedObject(targetTrack);
        SerializedProperty clipsProp = clipObj.FindProperty("m_Clips");

        if (clipsProp != null && clipsProp.isArray)
        {
            // 找到刚创建的 clip
            for (int i = 0; i < clipsProp.arraySize; i++)
            {
                SerializedProperty clipProp = clipsProp.GetArrayElementAtIndex(i);
                SerializedProperty startProp = clipProp.FindPropertyRelative("m_Start");

                if (startProp != null && System.Math.Abs(startProp.doubleValue - clip.start) < 0.001)
                {
                    // 找到了，设置 extrapolation modes
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

    /// <summary>
    /// 克隆 PlayableAsset
    /// </summary>
    static PlayableAsset ClonePlayableAsset(PlayableAsset original)
    {
        if (original == null) return null;

        // 创建同类型的实例
        PlayableAsset clone = ScriptableObject.CreateInstance(original.GetType()) as PlayableAsset;
        clone.name = original.name;
        clone.hideFlags = HideFlags.DontSave;

        // 使用 JsonUtility 深度复制数据
        string json = JsonUtility.ToJson(original);
        JsonUtility.FromJsonOverwrite(json, clone);

        return clone;
    }

    /// <summary>
    /// 将内存版本的修改保存回原始资源
    /// </summary>
    public static void SaveToOriginal(TimelineAsset memoryVersion, TimelineAsset original)
    {
        if (memoryVersion == null || original == null)
        {
            Debug.LogError("无法保存：内存版本或原始资源为空");
            return;
        }

        Debug.Log($"<color=yellow>【开始保存】</color> 将内存版本保存到: {AssetDatabase.GetAssetPath(original)}");

        // 记录撤销操作
        Undo.RecordObject(original, "Save Timeline from Memory");

        // 清空原始资源的所有轨道
        var originalTracks = original.GetRootTracks().ToList();
        foreach (var track in originalTracks)
        {
            original.DeleteTrack(track);
        }

        // 复制基本属性
        CloneTimelineProperties(memoryVersion, original);

        // 复制所有轨道
        Dictionary<Object, Object> cloneMap = new Dictionary<Object, Object>();
        foreach (var track in memoryVersion.GetRootTracks())
        {
            CloneTrack(track, original, cloneMap);
        }

        // 标记为脏并保存
        EditorUtility.SetDirty(original);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>【保存成功】</color> Timeline 已保存到磁盘");
    }

    /// <summary>
    /// 销毁内存版本对象
    /// </summary>
    public static void DestroyMemoryVersion(TimelineAsset memoryVersion)
    {
        if (memoryVersion == null) return;

        // 销毁所有轨道和 Clip
        var tracks = memoryVersion.GetRootTracks().ToList();
        foreach (var track in tracks)
        {
            DestroyTrackRecursive(track);
        }

        // 销毁 Timeline 本身
        Object.DestroyImmediate(memoryVersion);
        
        Debug.Log("<color=orange>【内存版本已销毁】</color>");
    }

    /// <summary>
    /// 递归销毁轨道及其子对象
    /// </summary>
    static void DestroyTrackRecursive(TrackAsset track)
    {
        if (track == null) return;

        // 销毁所有 Clip 的 Asset
        foreach (var clip in track.GetClips())
        {
            if (clip.asset != null)
            {
                Object.DestroyImmediate(clip.asset);
            }
        }

        // 递归销毁子轨道
        foreach (var childTrack in track.GetChildTracks())
        {
            DestroyTrackRecursive(childTrack);
        }

        // 销毁轨道本身
        Object.DestroyImmediate(track);
    }
}

