using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;

/// <summary>
/// Timeline 内存编辑器使用示例
/// 演示如何通过代码使用内存模式编辑 Timeline
/// </summary>
public class TimelineMemoryEditorExample
{
    /// <summary>
    /// 示例1：打开 Timeline 的内存版本
    /// </summary>
    [MenuItem("Timeline Memory/Example 1 - Open Timeline in Memory Mode")]
    public static void Example1_OpenInMemoryMode()
    {
        // 1. 选择一个 Timeline 资源
        TimelineAsset timeline = Selection.activeObject as TimelineAsset;
        
        if (timeline == null)
        {
            Debug.LogWarning("请先在 Project 窗口中选择一个 Timeline 资源");
            return;
        }

        // 2. 以内存模式打开
        var session = TimelineMemoryEditor.OpenInMemoryMode(timeline);
        
        if (session != null)
        {
            Debug.Log($"<color=green>✓ 成功打开内存模式</color>");
            Debug.Log($"  原始资源: {session.originalAsset.name}");
            Debug.Log($"  内存版本: {session.memoryAsset.name}");
            Debug.Log($"  现在你可以在 Timeline 窗口中编辑，所有修改都在内存中");
        }
    }

    /// <summary>
    /// 示例2：检查当前是否有未保存的修改
    /// </summary>
    [MenuItem("Timeline Memory/Example 2 - Check Unsaved Changes")]
    public static void Example2_CheckUnsavedChanges()
    {
        TimelineAsset timeline = Selection.activeObject as TimelineAsset;
        
        if (timeline == null)
        {
            Debug.LogWarning("请先在 Project 窗口中选择一个 Timeline 资源");
            return;
        }

        var session = TimelineMemoryEditor.GetSession(timeline);
        
        if (session == null)
        {
            Debug.Log("该 Timeline 没有在内存模式下打开");
            return;
        }

        if (session.isDirty)
        {
            Debug.LogWarning($"⚠ 有未保存的修改！修改次数: {session.changeCount}");
        }
        else
        {
            Debug.Log("✓ 没有未保存的修改");
        }
    }

    /// <summary>
    /// 示例3：通过代码保存内存版本
    /// </summary>
    [MenuItem("Timeline Memory/Example 3 - Save Memory Version")]
    public static void Example3_SaveMemoryVersion()
    {
        TimelineAsset timeline = Selection.activeObject as TimelineAsset;
        
        if (timeline == null)
        {
            Debug.LogWarning("请先在 Project 窗口中选择一个 Timeline 资源");
            return;
        }

        var session = TimelineMemoryEditor.GetSession(timeline);
        
        if (session == null)
        {
            Debug.Log("该 Timeline 没有在内存模式下打开");
            return;
        }

        if (!session.isDirty)
        {
            Debug.Log("没有需要保存的修改");
            return;
        }

        // 保存到原始资源
        TimelineCloner.SaveToOriginal(session.memoryAsset, session.originalAsset);
        session.isDirty = false;
        session.changeCount = 0;
        
        Debug.Log($"<color=green>✓ 已保存到磁盘</color>");
    }

    /// <summary>
    /// 示例4：通过代码放弃修改
    /// </summary>
    [MenuItem("Timeline Memory/Example 4 - Discard Changes")]
    public static void Example4_DiscardChanges()
    {
        TimelineAsset timeline = Selection.activeObject as TimelineAsset;
        
        if (timeline == null)
        {
            Debug.LogWarning("请先在 Project 窗口中选择一个 Timeline 资源");
            return;
        }

        var session = TimelineMemoryEditor.GetSession(timeline);
        
        if (session == null)
        {
            Debug.Log("该 Timeline 没有在内存模式下打开");
            return;
        }

        if (EditorUtility.DisplayDialog(
            "确认放弃修改",
            $"确定要放弃对 '{timeline.name}' 的所有修改吗？\n修改次数: {session.changeCount}",
            "放弃",
            "取消"))
        {
            TimelineCloner.DestroyMemoryVersion(session.memoryAsset);
            Debug.Log($"<color=orange>✓ 已放弃所有修改</color>");
        }
    }

    /// <summary>
    /// 示例5：批量处理 - 打开多个 Timeline
    /// </summary>
    [MenuItem("Timeline Memory/Example 5 - Batch Open Multiple Timelines")]
    public static void Example5_BatchOpen()
    {
        // 获取所有选中的 Timeline 资源
        var timelines = Selection.GetFiltered<TimelineAsset>(SelectionMode.Assets);
        
        if (timelines.Length == 0)
        {
            Debug.LogWarning("请先在 Project 窗口中选择一个或多个 Timeline 资源");
            return;
        }

        Debug.Log($"<color=cyan>开始批量打开 {timelines.Length} 个 Timeline...</color>");
        
        int successCount = 0;
        foreach (var timeline in timelines)
        {
            var session = TimelineMemoryEditor.OpenInMemoryMode(timeline);
            if (session != null)
            {
                successCount++;
            }
        }

        Debug.Log($"<color=green>✓ 成功打开 {successCount}/{timelines.Length} 个 Timeline</color>");
    }

    /// <summary>
    /// 示例6：自定义工作流 - 编辑并自动保存
    /// </summary>
    [MenuItem("Timeline Memory/Example 6 - Custom Workflow")]
    public static void Example6_CustomWorkflow()
    {
        TimelineAsset timeline = Selection.activeObject as TimelineAsset;
        
        if (timeline == null)
        {
            Debug.LogWarning("请先在 Project 窗口中选择一个 Timeline 资源");
            return;
        }

        // 1. 打开内存模式
        var session = TimelineMemoryEditor.OpenInMemoryMode(timeline);
        
        if (session == null)
        {
            Debug.LogError("无法打开内存模式");
            return;
        }

        Debug.Log("<color=cyan>【自定义工作流示例】</color>");
        Debug.Log("1. Timeline 已在内存模式下打开");
        Debug.Log("2. 现在你可以在 Timeline 窗口中进行编辑");
        Debug.Log("3. 所有修改都会被监听并记录");
        Debug.Log("4. 使用 'Timeline Memory Editor' 窗口来保存或放弃修改");
        Debug.Log("");
        Debug.Log("提示：打开 Window > Timeline Memory Editor 查看编辑会话");
    }
}

