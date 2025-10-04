// ==================================================
// 调试助手 - 显示详细的修饰符信息
// ==================================================

using UnityEngine;
using SpellSystem;

[AddComponentMenu("Spell System/Debug Helper")]
public class DebugHelper : MonoBehaviour
{
    void Update()
    {
        // 按 D 键显示详细调试信息
        if (Input.GetKeyDown(KeyCode.D))
        {
            SpellLogger.LogSystem("=== 调试模式开启 ===");
            SpellLogger.LogSystem("在 SpellContext.cs 中添加调试输出");
        }
    }
}

