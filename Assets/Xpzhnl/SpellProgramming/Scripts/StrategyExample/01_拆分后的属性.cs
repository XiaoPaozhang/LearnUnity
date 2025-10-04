// ==================================================
// 步骤1：拆分属性结构
// ==================================================
// 将臃肿的 SpellStats 拆分成多个小结构
// 每种类型的法术只持有自己需要的属性

using SpellSystem;
using UnityEngine;

namespace SpellSystemStrategy
{
    // ========== 基础属性（所有法术共有） ==========

    /// <summary>
    /// 所有法术都有的基础属性
    /// </summary>
    [System.Serializable]
    public struct BaseSpellStats
    {
        [Header("基础信息")]
        public string spellName;
        public string description;

        [Header("消耗")]
        public float manaCost;
        public float castDelay;

        [Header("稀有度")]
        public int rarity;
    }

    // ========== 投射物专属属性 ==========

    /// <summary>
    /// 投射物法术专属属性
    /// 只有投射物法术需要这些字段
    /// </summary>
    [System.Serializable]
    public struct ProjectileSpellStats
    {
        [Header("伤害")]
        public float damage;

        [Header("移动")]
        public float speed;
        public float lifetime;
        public float gravity;

        [Header("碰撞")]
        public int piercing;        // 穿透
        public int bounces;         // 弹跳

        [Header("扩散")]
        public float spread;
        public bool isHoming;       // 追踪
        public float homingStrength;

        [Header("视觉")]
        public float size;
        public GameObject projectilePrefab;
    }

    // ========== 修饰符专属属性 ==========

    /// <summary>
    /// 修饰符法术专属属性
    /// 只有修饰符法术需要这些字段
    /// </summary>
    [System.Serializable]
    public struct ModifierSpellStats
    {
        [Header("乘法修正")]
        public float damageMultiplier;
        public float speedMultiplier;
        public float sizeMultiplier;

        [Header("加法修正")]
        public int piercingBonus;
        public int bouncesBonus;
        public float spreadBonus;

        [Header("多重施法")]
        public int projectileCount;
        public float projectileSpread;

        [Header("修饰符类型")]
        public bool isPersistent;   // 是否影响所有后续法术
        public int duration;        // 持续几个法术
    }

    // ========== 触发器专属属性 ==========

    /// <summary>
    /// 触发器法术专属属性
    /// 只有触发器法术需要这些字段
    /// </summary>
    [System.Serializable]
    public struct TriggerSpellStats
    {
        [Header("触发条件")]
        public TriggerType triggerType;
        public float triggerDelay;      // 延迟触发
        public float triggerRadius;     // 触发范围

        [Header("触发次数")]
        public int triggerCharges;      // 可触发次数
        public float cooldown;          // 触发冷却

        [Header("载荷")]
        public GameObject carrierPrefab;    // 触发器载体（例如地雷）
    }

    // ========== 对比说明 ==========

    /*
     * 【对比现有的 SpellStats】
     * 
     * 现有系统（Step2_SpellStats.cs）：
     * ────────────────────────────────
     * public struct SpellStats {
     *     public float damage;            // 投射物用
     *     public float speed;             // 投射物用
     *     public float damageMultiplier;  // 修饰符用
     *     public float triggerDelay;      // 触发器用
     *     // ... 40+ 字段全部混在一起
     * }
     * 
     * 问题：
     * - 火球术不需要 triggerDelay
     * - 伤害增强不需要 speed
     * - 内存浪费，理解困难
     * 
     * 
     * 新系统（本文件）：
     * ────────────────────────────────
     * 投射物法术持有：
     *   BaseSpellStats + ProjectileSpellStats
     * 
     * 修饰符法术持有：
     *   BaseSpellStats + ModifierSpellStats
     * 
     * 触发器法术持有：
     *   BaseSpellStats + TriggerSpellStats
     * 
     * 优点：
     * - 各司其职，字段清晰
     * - 扩展新类型不影响其他
     * - 内存更高效
     */
}



