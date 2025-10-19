using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;

/// <summary>
/// Timeline 保存拦截器
/// 防止内存版本的 Timeline 被意外保存到磁盘
/// </summary>
public class TimelineSaveInterceptor : UnityEditor.AssetModificationProcessor
{
    /// <summary>
    /// 在资源保存前调用
    /// </summary>
    static string[] OnWillSaveAssets(string[] paths)
    {
        // 检查所有待保存的资源
        foreach (var path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            
            if (asset != null && IsMemoryObject(asset))
            {
                Debug.LogWarning($"<color=yellow>【保存已拦截】</color> 内存对象不应保存到磁盘: {asset.name}");
                
                // 从保存列表中移除
                var filteredPaths = new string[paths.Length - 1];
                int index = 0;
                foreach (var p in paths)
                {
                    if (p != path)
                    {
                        filteredPaths[index++] = p;
                    }
                }
                return filteredPaths;
            }
        }
        
        return paths;
    }

    /// <summary>
    /// 检查对象是否是内存对象
    /// </summary>
    static bool IsMemoryObject(Object obj)
    {
        if (obj == null) return false;
        
        // 检查 HideFlags
        if ((obj.hideFlags & HideFlags.DontSave) != 0)
        {
            return true;
        }
        
        // 检查是否是内存版 Timeline
        if (obj is TimelineAsset timeline)
        {
            return timeline.name.Contains("(Memory)");
        }
        
        return false;
    }
}

