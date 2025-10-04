// ==================================================
// 步骤2：定义策略接口
// ==================================================
// 所有法术行为的统一接口
// 不同类型的法术实现不同的策略

using UnityEngine;
using SpellSystem; // 引用现有的 SpellContext

namespace SpellSystemStrategy
{
    // ========== 策略接口 ==========

    /// <summary>
    /// 法术行为策略接口
    /// 所有法术执行逻辑都通过这个接口
    /// </summary>
    public interface ISpellBehavior
    {
        /// <summary>
        /// 执行法术
        /// </summary>
        /// <param name="context">施法上下文</param>
        /// <param name="spellData">法术数据</param>
        void Execute(SpellContext context, SpellDataV2 spellData);

        /// <summary>
        /// 验证法术是否可以执行
        /// </summary>
        bool CanExecute(SpellContext context, SpellDataV2 spellData);

        /// <summary>
        /// 获取法术描述（用于UI显示）
        /// </summary>
        string GetDescription(SpellDataV2 spellData);
    }

    // ========== 为什么需要接口？ ==========

    /*
     * 【对比现有系统】
     * 
     * 现有系统（Step4_SpellData.cs）：
     * ────────────────────────────────
     * public void Execute(SpellContext context)
     * {
     *     switch (spellType)
     *     {
     *         case SpellType.Projectile:
     *             CastProjectile(context, stats);
     *             break;
     *         case SpellType.Modifier:
     *             ApplyModifier(context, stats);
     *             break;
     *         // ... 更多类型
     *     }
     * }
     * 
     * 问题：
     * - 所有逻辑都在 SpellData 中
     * - 添加新类型要修改 SpellData
     * - switch-case 越来越长
     * 
     * 
     * 新系统（策略模式）：
     * ────────────────────────────────
     * public void Execute(SpellContext context)
     * {
     *     behavior.Execute(context, this);
     * }
     * 
     * 优点：
     * - 每种类型的逻辑独立在各自的策略类中
     * - 添加新类型只需新增策略类，不修改 SpellData
     * - 符合"开闭原则"（对扩展开放，对修改关闭）
     */

    // ========== 抽象基类（可选） ==========

    /// <summary>
    /// 法术行为策略的抽象基类
    /// 提供一些通用实现，子类可以覆盖
    /// </summary>
    public abstract class SpellBehaviorBase : ScriptableObject, ISpellBehavior
    {
        public abstract void Execute(SpellContext context, SpellDataV2 spellData);

        public virtual bool CanExecute(SpellContext context, SpellDataV2 spellData)
        {
            // 默认实现：检查魔力是否足够
            return spellData.baseStats.manaCost >= 0;
        }

        public virtual string GetDescription(SpellDataV2 spellData)
        {
            return $"{spellData.baseStats.spellName}\n{spellData.baseStats.description}";
        }

        /// <summary>
        /// 辅助方法：旋转向量
        /// </summary>
        protected Vector2 RotateVector(Vector2 vector, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }
    }

    // ========== 为什么用 ScriptableObject？ ==========

    /*
     * Unity 中策略模式的实现方式：
     * 
     * 1. 纯接口：需要在运行时 new 对象，不方便
     * 2. ScriptableObject：可以在编辑器中创建资源，直接引用
     * 
     * 使用 ScriptableObject 的好处：
     * - 可以在编辑器中配置策略参数
     * - 可以重用策略实例
     * - 可以在 Inspector 中直接拖拽引用
     * 
     * 例如：
     * - 创建 ProjectileBehavior.asset
     * - 创建 ModifierBehavior.asset
     * - 在 SpellData 中引用对应的 behavior
     */
}


