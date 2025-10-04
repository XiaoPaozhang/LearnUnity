// ==================================================
// 法术系统日志工具
// ==================================================
// 统一管理日志输出，使用颜色分类

using UnityEngine;

namespace SpellSystem
{
    /// <summary>
    /// 法术系统专用日志工具
    /// </summary>
    public static class SpellLogger
    {
        // ========== 颜色定义 ==========
        private const string COLOR_SPELL = "#4EC9FF";      // 青色 - 法术执行
        private const string COLOR_MODIFIER = "#FFD700";   // 金色 - 修饰符
        private const string COLOR_DAMAGE = "#FF6B6B";     // 红色 - 伤害
        private const string COLOR_SUCCESS = "#51CF66";    // 绿色 - 成功
        private const string COLOR_WARNING = "#FFA94D";    // 橙色 - 警告
        private const string COLOR_SYSTEM = "#B0B0B0";     // 灰色 - 系统
        private const string COLOR_PROJECTILE = "#9775FA"; // 紫色 - 投射物

        // ========== 日志开关 ==========
        public static bool EnableSpellLogs = true;
        public static bool EnableModifierLogs = true;
        public static bool EnableDamageLogs = true;
        public static bool EnableProjectileLogs = true;
        public static bool EnableSystemLogs = true;

        // ========== 法术相关日志 ==========

        /// <summary>
        /// 法术开始施放
        /// </summary>
        public static void LogSpellCastStart(int spellCount)
        {
            if (!EnableSpellLogs) return;
            Debug.Log($"<color={COLOR_SPELL}><b>╔══════ 开始施法 ══════╗</b></color>");
            Debug.Log($"<color={COLOR_SPELL}>  法术数量: {spellCount}</color>");
        }

        /// <summary>
        /// 法术执行
        /// </summary>
        public static void LogSpellExecute(int index, string spellName, string spellType)
        {
            if (!EnableSpellLogs) return;
            Debug.Log($"<color={COLOR_SPELL}>[法术 {index}] <b>{spellName}</b> ({spellType})</color>");
        }

        /// <summary>
        /// 法术施放完成
        /// </summary>
        public static void LogSpellCastEnd()
        {
            if (!EnableSpellLogs) return;
            Debug.Log($"<color={COLOR_SPELL}><b>╚══════ 施法完成 ══════╝</b></color>\n");
        }

        /// <summary>
        /// 切换法术组合
        /// </summary>
        public static void LogSpellComboChange(string comboName)
        {
            if (!EnableSpellLogs) return;
            Debug.Log($"<color={COLOR_SUCCESS}><b>★ 切换组合: {comboName} ★</b></color>");
        }

        // ========== 修饰符相关日志 ==========

        /// <summary>
        /// 应用修饰符
        /// </summary>
        public static void LogModifierApply(string modifierName, float damageMultiplier, float speedMultiplier, int projectileCount)
        {
            if (!EnableModifierLogs) return;
            string info = $"<color={COLOR_MODIFIER}>[修饰符] <b>{modifierName}</b>";

            if (damageMultiplier != 1f)
                info += $" | 伤害×{damageMultiplier:F1}";
            if (speedMultiplier != 1f)
                info += $" | 速度×{speedMultiplier:F1}";
            if (projectileCount > 1)
                info += $" | 数量×{projectileCount}";

            info += "</color>";
            Debug.Log(info);
        }

        /// <summary>
        /// 修饰符栈状态
        /// </summary>
        public static void LogModifierStack(int count)
        {
            if (!EnableModifierLogs) return;
            Debug.Log($"<color={COLOR_MODIFIER}>  → 修饰符栈: {count} 个</color>");
        }

        // ========== 投射物相关日志 ==========

        /// <summary>
        /// 投射物初始化
        /// </summary>
        public static void LogProjectileInit(float damage, float speed, float lifetime, int piercing)
        {
            if (!EnableProjectileLogs) return;
            string info = $"<color={COLOR_PROJECTILE}>[投射物] 创建";
            info += $" | 伤害:<b>{damage:F0}</b>";
            info += $" | 速度:{speed:F1}";
            info += $" | 生命:{lifetime:F1}s";
            if (piercing > 0)
                info += $" | 穿透:{piercing}";
            info += "</color>";
            Debug.Log(info);
        }

        /// <summary>
        /// 投射物销毁
        /// </summary>
        public static void LogProjectileDestroy(string reason = "")
        {
            if (!EnableProjectileLogs) return;
            string msg = $"<color={COLOR_SYSTEM}>[投射物] 销毁";
            if (!string.IsNullOrEmpty(reason))
                msg += $" - {reason}";
            msg += "</color>";
            Debug.Log(msg);
        }

        // ========== 伤害相关日志 ==========

        /// <summary>
        /// 造成伤害
        /// </summary>
        public static void LogDamage(string target, float damage)
        {
            if (!EnableDamageLogs) return;
            Debug.Log($"<color={COLOR_DAMAGE}><b>⚔ 伤害: {damage:F1}</b> → {target}</color>");
        }

        /// <summary>
        /// 命中目标
        /// </summary>
        public static void LogHit(string target)
        {
            if (!EnableDamageLogs) return;
            Debug.Log($"<color={COLOR_DAMAGE}>◉ 命中: {target}</color>");
        }

        // ========== 系统日志 ==========

        /// <summary>
        /// 警告信息
        /// </summary>
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"<color={COLOR_WARNING}>⚠ {message}</color>");
        }

        /// <summary>
        /// 成功信息
        /// </summary>
        public static void LogSuccess(string message)
        {
            if (!EnableSystemLogs) return;
            Debug.Log($"<color={COLOR_SUCCESS}>✓ {message}</color>");
        }

        /// <summary>
        /// 系统信息
        /// </summary>
        public static void LogSystem(string message)
        {
            if (!EnableSystemLogs) return;
            Debug.Log($"<color={COLOR_SYSTEM}>{message}</color>");
        }

        // ========== 调试工具 ==========

        /// <summary>
        /// 分隔线
        /// </summary>
        public static void LogSeparator()
        {
            Debug.Log("<color=#303030>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
        }

        /// <summary>
        /// 打印配置
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void PrintConfig()
        {
            Debug.Log($"<color={COLOR_SUCCESS}>SpellLogger 已初始化</color>");
            Debug.Log($"<color={COLOR_SYSTEM}>使用 SpellLogger.EnableXXX 控制日志分类显示</color>");
        }
    }
}

