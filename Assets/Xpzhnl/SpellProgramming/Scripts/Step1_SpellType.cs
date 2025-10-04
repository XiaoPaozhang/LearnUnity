// ==================================================
// 第一步：定义法术类型枚举
// ==================================================
// 这是法术系统的基础，定义了所有可能的法术类型

using UnityEngine;

namespace SpellSystem
{
    /// <summary>
    /// 法术类型枚举
    /// </summary>
    public enum SpellType
    {
        /// <summary>投射物法术 - 发射实际的投射物（如火球、闪电）</summary>
        Projectile,

        /// <summary>修饰符法术 - 修改后续法术的属性（如伤害增强、多重施法）</summary>
        Modifier,

        /// <summary>触发器法术 - 在特定条件下触发其他法术（如碰撞触发、定时触发）</summary>
        Trigger,

        /// <summary>工具法术 - 立即效果，不产生投射物（如治疗、传送）</summary>
        Utility
    }

    /// <summary>
    /// 触发条件类型
    /// </summary>
    public enum TriggerType
    {
        /// <summary>碰撞时触发</summary>
        OnCollision,

        /// <summary>定时触发</summary>
        OnTimer,

        /// <summary>接近敌人时触发</summary>
        OnNearEnemy,

        /// <summary>投射物死亡时触发</summary>
        OnDeath,

        /// <summary>每隔一定距离触发</summary>
        OnDistance
    }

    /// <summary>
    /// 投射物形状
    /// </summary>
    public enum ProjectileShape
    {
        Sphere,     // 球形
        Bullet,     // 子弹形
        Beam,       // 射线
        Arc,        // 弧形
        Custom      // 自定义
    }

    /// <summary>
    /// 元素类型
    /// </summary>
    public enum ElementType
    {
        None,       // 无元素
        Fire,       // 火
        Ice,        // 冰
        Lightning,  // 雷电
        Poison,     // 毒
        Holy,       // 神圣
        Dark        // 黑暗
    }
}

