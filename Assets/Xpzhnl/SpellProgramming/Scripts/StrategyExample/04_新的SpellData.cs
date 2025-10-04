// ==================================================
// 步骤4：使用策略的新 SpellData
// ==================================================
// 这是重构后的 SpellData，使用策略模式

using UnityEngine;
using SpellSystem;

namespace SpellSystemStrategy
{
    /// <summary>
    /// 重构后的法术数据（V2版本）
    /// 使用策略模式，属性更清晰
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpell", menuName = "SpellSystem V2/Spell Data V2")]
    public class SpellDataV2 : ScriptableObject
    {
        // ========== 公共属性 ==========

        [Header("基础属性（所有法术都有）")]
        public BaseSpellStats baseStats;

        // ========== 类型专属属性 ==========

        [Header("投射物属性（仅投射物法术需要）")]
        public ProjectileSpellStats projectileStats;

        [Header("修饰符属性（仅修饰符法术需要）")]
        public ModifierSpellStats modifierStats;

        [Header("触发器属性（仅触发器法术需要）")]
        public TriggerSpellStats triggerStats;

        // ========== 行为策略 ==========

        [Header("执行策略")]
        [Tooltip("定义这个法术如何执行")]
        public SpellBehaviorBase behavior;

        // ========== 执行方法 ==========

        /// <summary>
        /// 执行法术（统一入口）
        /// </summary>
        public void Execute(SpellContext context)
        {
            if (behavior == null)
            {
                Debug.LogError($"法术 {baseStats.spellName} 没有设置 behavior！");
                return;
            }

            // 检查是否可以执行
            if (!behavior.CanExecute(context, this))
            {
                SpellLogger.LogWarning($"法术 {baseStats.spellName} 无法执行！");
                return;
            }

            // 委派给策略执行
            behavior.Execute(context, this);
        }

        /// <summary>
        /// 获取法术描述
        /// </summary>
        public string GetDescription()
        {
            if (behavior == null)
                return baseStats.description;

            return behavior.GetDescription(this);
        }
    }

    // ========== 对比现有系统 ==========

    /*
     * 【结构对比】
     * 
     * 旧版 SpellData（Step4_SpellData.cs）：
     * ════════════════════════════════════════
     * public class SpellData : ScriptableObject
     * {
     *     public string spellName;
     *     public SpellType spellType;
     *     public SpellStats stats;  ← 包含所有字段
     *     public GameObject projectilePrefab;
     *     
     *     public void Execute(SpellContext context)
     *     {
     *         switch (spellType)
     *         {
     *             case Projectile: CastProjectile(...); break;
     *             case Modifier: ApplyModifier(...); break;
     *             ...
     *         }
     *     }
     *     
     *     void CastProjectile(...) { ... }
     *     void ApplyModifier(...) { ... }
     *     void ExecuteTrigger(...) { ... }
     * }
     * 
     * 问题：
     * 1. SpellStats 太大，包含所有类型的字段
     * 2. 所有执行逻辑都在 SpellData 中
     * 3. 添加新类型要修改多处代码
     * 
     * 
     * 新版 SpellDataV2（本文件）：
     * ════════════════════════════════════════
     * public class SpellDataV2 : ScriptableObject
     * {
     *     public BaseSpellStats baseStats;         ← 公共属性
     *     public ProjectileSpellStats projectileStats;  ← 投射物专属
     *     public ModifierSpellStats modifierStats;      ← 修饰符专属
     *     public TriggerSpellStats triggerStats;        ← 触发器专属
     *     
     *     public SpellBehaviorBase behavior;  ← 策略对象
     *     
     *     public void Execute(SpellContext context)
     *     {
     *         behavior.Execute(context, this);
     *     }
     * }
     * 
     * 优点：
     * 1. 属性分离，各司其职
     * 2. 执行逻辑在独立的策略类中
     * 3. 添加新类型只需创建新策略
     * 4. 更易测试和维护
     */

    // ========== 使用场景 ==========

    /*
     * 【在 Unity 编辑器中】
     * 
     * 创建火球术：
     * ────────────
     * 1. 创建 SpellDataV2 资源
     * 2. 填写 BaseStats（名称、描述、消耗）
     * 3. 填写 ProjectileStats（伤害、速度等）
     * 4. 拖入 ProjectileBehavior 资源到 behavior 字段
     * 5. 完成！
     * 
     * 好处：
     * - ModifierStats 和 TriggerStats 留空，不占用
     * - 如果需要修改行为，只需换个 behavior
     * 
     * 
     * 创建伤害增强：
     * ────────────
     * 1. 创建 SpellDataV2 资源
     * 2. 填写 BaseStats
     * 3. 填写 ModifierStats（伤害倍数等）
     * 4. 拖入 ModifierBehavior 资源到 behavior 字段
     * 5. 完成！
     * 
     * 好处：
     * - ProjectileStats 和 TriggerStats 留空
     * - 清晰知道这是修饰符
     */

    // ========== 扩展示例 ==========

    /*
     * 【如何添加新类型】
     * 
     * 假设要添加"治疗法术"：
     * 
     * 1. 创建属性结构：
     *    [Serializable]
     *    public struct HealSpellStats
     *    {
     *        public float healAmount;
     *        public float healRadius;
     *        public bool healSelf;
     *    }
     * 
     * 2. 在 SpellDataV2 中添加：
     *    public HealSpellStats healStats;
     * 
     * 3. 创建策略类：
     *    public class HealBehavior : SpellBehaviorBase
     *    {
     *        public override void Execute(...)
     *        {
     *            // 治疗逻辑
     *        }
     *    }
     * 
     * 4. 创建 HealBehavior.asset 资源
     * 
     * 5. 完成！不需要修改任何现有代码
     */
}



