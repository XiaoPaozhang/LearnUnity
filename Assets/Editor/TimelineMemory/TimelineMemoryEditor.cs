using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Timeline 内存编辑器窗口
/// 提供打开、编辑、保存内存版 Timeline 的功能
/// </summary>
public class TimelineMemoryEditor : EditorWindow
{
    // 当前正在编辑的内存版本
    private static Dictionary<TimelineAsset, TimelineMemorySession> activeSessions = 
        new Dictionary<TimelineAsset, TimelineMemorySession>();

    private TimelineAsset selectedOriginalTimeline;
    private Vector2 scrollPosition;
    private bool showActiveSessions = true;

    [MenuItem("Window/Timeline Memory Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<TimelineMemoryEditor>("Timeline 内存编辑器");
        window.minSize = new Vector2(400, 300);
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        
        // 标题
        GUILayout.Label("Timeline 内存编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "在内存中编辑 Timeline，不会自动保存到磁盘。\n" +
            "你可以随时保存或放弃修改。", 
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 打开新的 Timeline
        DrawOpenSection();

        EditorGUILayout.Space(10);

        // 显示活动的编辑会话
        DrawActiveSessionsSection();
    }

    void DrawOpenSection()
    {
        GUILayout.Label("打开 Timeline（内存模式）", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        selectedOriginalTimeline = EditorGUILayout.ObjectField(
            "Timeline 资源", 
            selectedOriginalTimeline, 
            typeof(TimelineAsset), 
            false) as TimelineAsset;

        GUI.enabled = selectedOriginalTimeline != null;
        if (GUILayout.Button("打开（内存模式）", GUILayout.Width(120)))
        {
            OpenInMemoryMode(selectedOriginalTimeline);
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    void DrawActiveSessionsSection()
    {
        showActiveSessions = EditorGUILayout.Foldout(showActiveSessions, 
            $"活动的编辑会话 ({activeSessions.Count})", true);

        if (!showActiveSessions) return;

        if (activeSessions.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有活动的编辑会话", MessageType.None);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        List<TimelineAsset> toRemove = new List<TimelineAsset>();

        foreach (var kvp in activeSessions)
        {
            var session = kvp.Value;
            
            EditorGUILayout.BeginVertical("box");
            
            // 会话信息
            EditorGUILayout.LabelField("原始资源", session.originalAsset.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("内存版本", session.memoryAsset.name);
            EditorGUILayout.LabelField("状态", session.isDirty ? "已修改 ⚠" : "未修改");
            EditorGUILayout.LabelField("修改次数", session.changeCount.ToString());

            EditorGUILayout.Space(5);

            // 操作按钮
            EditorGUILayout.BeginHorizontal();

            // 在 Timeline 窗口中打开
            if (GUILayout.Button("在 Timeline 窗口中打开"))
            {
                OpenInTimelineWindow(session);
            }

            // 保存按钮
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("💾 保存到磁盘"))
            {
                SaveSession(session);
            }
            GUI.backgroundColor = Color.white;

            // 放弃按钮
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✖ 放弃修改"))
            {
                if (EditorUtility.DisplayDialog(
                    "确认放弃修改", 
                    $"确定要放弃对 '{session.originalAsset.name}' 的所有修改吗？\n此操作无法撤销！", 
                    "放弃", 
                    "取消"))
                {
                    DiscardSession(session);
                    toRemove.Add(kvp.Key);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // 移除已关闭的会话
        foreach (var key in toRemove)
        {
            activeSessions.Remove(key);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 以内存模式打开 Timeline
    /// </summary>
    public static TimelineMemorySession OpenInMemoryMode(TimelineAsset original)
    {
        if (original == null)
        {
            Debug.LogError("无法打开空的 Timeline 资源");
            return null;
        }

        // 检查是否已经打开
        if (activeSessions.ContainsKey(original))
        {
            Debug.LogWarning($"Timeline '{original.name}' 已经在内存模式下打开");
            var existingSession = activeSessions[original];
            OpenInTimelineWindow(existingSession);
            return existingSession;
        }

        // 克隆到内存
        TimelineAsset memoryVersion = TimelineCloner.DeepClone(original);
        
        if (memoryVersion == null)
        {
            Debug.LogError("克隆 Timeline 失败");
            return null;
        }

        // 创建编辑会话
        var session = new TimelineMemorySession(original, memoryVersion);
        activeSessions[original] = session;

        // 订阅变化事件
        ObjectChangeEvents.changesPublished += session.OnChangesPublished;

        Debug.Log($"<color=cyan>【内存模式已启动】</color> {original.name}");
        
        // 自动在 Timeline 窗口中打开
        OpenInTimelineWindow(session);

        return session;
    }

    /// <summary>
    /// 在 Timeline 窗口中打开内存版本
    /// </summary>
    static void OpenInTimelineWindow(TimelineMemorySession session)
    {
        // 查找或创建一个 PlayableDirector
        PlayableDirector director = FindOrCreateDirector();
        
        if (director != null)
        {
            // 设置为内存版本
            director.playableAsset = session.memoryAsset;
            
            // 打开 Timeline 窗口
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
            
            // 选中 Director（这会让 Timeline 窗口显示对应的 Timeline）
            Selection.activeGameObject = director.gameObject;
            
            Debug.Log($"<color=green>【Timeline 窗口已打开】</color> 正在编辑内存版本");
        }
    }

    /// <summary>
    /// 查找或创建 PlayableDirector
    /// </summary>
    static PlayableDirector FindOrCreateDirector()
    {
        // 先尝试查找现有的
        PlayableDirector director = FindObjectOfType<PlayableDirector>();
        
        if (director == null)
        {
            // 创建新的
            GameObject go = new GameObject("Timeline Memory Director");
            director = go.AddComponent<PlayableDirector>();
            Debug.Log("创建了新的 PlayableDirector");
        }
        
        return director;
    }

    /// <summary>
    /// 保存会话
    /// </summary>
    static void SaveSession(TimelineMemorySession session)
    {
        if (!session.isDirty)
        {
            EditorUtility.DisplayDialog("无需保存", "当前没有未保存的修改", "确定");
            return;
        }

        TimelineCloner.SaveToOriginal(session.memoryAsset, session.originalAsset);
        session.isDirty = false;
        session.changeCount = 0;
        
        EditorUtility.DisplayDialog("保存成功", $"Timeline '{session.originalAsset.name}' 已保存到磁盘", "确定");
    }

    /// <summary>
    /// 放弃会话
    /// </summary>
    static void DiscardSession(TimelineMemorySession session)
    {
        // 取消订阅
        ObjectChangeEvents.changesPublished -= session.OnChangesPublished;
        
        // 销毁内存版本
        TimelineCloner.DestroyMemoryVersion(session.memoryAsset);
        
        Debug.Log($"<color=orange>【已放弃修改】</color> {session.originalAsset.name}");
    }

    void OnDestroy()
    {
        // 窗口关闭时检查未保存的会话
        if (activeSessions.Count > 0)
        {
            bool hasUnsaved = false;
            foreach (var session in activeSessions.Values)
            {
                if (session.isDirty)
                {
                    hasUnsaved = true;
                    break;
                }
            }

            if (hasUnsaved)
            {
                Debug.LogWarning("⚠ 有未保存的 Timeline 编辑会话！请在关闭前保存或放弃修改。");
            }
        }
    }

    /// <summary>
    /// 获取指定原始资源的会话
    /// </summary>
    public static TimelineMemorySession GetSession(TimelineAsset original)
    {
        return activeSessions.ContainsKey(original) ? activeSessions[original] : null;
    }

    /// <summary>
    /// 检查是否有活动会话
    /// </summary>
    public static bool HasActiveSession(TimelineAsset original)
    {
        return activeSessions.ContainsKey(original);
    }
}

/// <summary>
/// Timeline 内存编辑会话
/// </summary>
public class TimelineMemorySession
{
    public TimelineAsset originalAsset;      // 原始磁盘资源
    public TimelineAsset memoryAsset;        // 内存克隆版本
    public bool isDirty;                     // 是否有未保存的修改
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
            
            // 检查变化是否与当前内存对象相关
            bool isRelated = false;
            
            switch (eventType)
            {
                case ObjectChangeKind.ChangeAssetObjectProperties:
                    stream.GetChangeAssetObjectPropertiesEvent(i, out var changeEvent);
                    var changedObject = EditorUtility.InstanceIDToObject(changeEvent.instanceId);
                    isRelated = IsRelatedToMemoryAsset(changedObject);
                    break;
                    
                case ObjectChangeKind.CreateAssetObject:
                case ObjectChangeKind.DestroyAssetObject:
                    isRelated = true; // 简化处理，假设创建/删除都相关
                    break;
            }

            if (isRelated)
            {
                isDirty = true;
                changeCount++;
                Debug.Log($"<color=yellow>【内存版本已修改】</color> {originalAsset.name} (修改次数: {changeCount})");
            }
        }
    }

    /// <summary>
    /// 检查对象是否与内存资源相关
    /// </summary>
    bool IsRelatedToMemoryAsset(Object obj)
    {
        if (obj == null) return false;

        if (obj == memoryAsset) return true;

        if (obj is TrackAsset trackAsset)
        {
            // 检查是否属于当前 Timeline
            var rootTracks = memoryAsset.GetRootTracks().ToList();
            return rootTracks.Contains(trackAsset) ||
                   IsChildTrack(memoryAsset, trackAsset);
        }

        if (obj is PlayableAsset playable)
        {
            // 检查是否是某个 Clip 的资源
            foreach (var outputTrack in memoryAsset.GetOutputTracks())
            {
                foreach (var clip in outputTrack.GetClips())
                {
                    if (clip.asset == playable)
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 检查是否是子轨道
    /// </summary>
    bool IsChildTrack(TimelineAsset timeline, TrackAsset track)
    {
        foreach (var rootTrack in timeline.GetRootTracks())
        {
            if (IsChildTrackRecursive(rootTrack, track))
                return true;
        }
        return false;
    }

    bool IsChildTrackRecursive(TrackAsset parent, TrackAsset target)
    {
        foreach (var child in parent.GetChildTracks())
        {
            if (child == target) return true;
            if (IsChildTrackRecursive(child, target)) return true;
        }
        return false;
    }
}

