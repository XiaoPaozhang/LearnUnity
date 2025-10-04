// ==================================================
// 第二步：定义法术属性结构
// ==================================================
// 这个结构包含了法术的所有可能属性
// 使用结构体而不是类，因为这是纯数据，不需要继承

using UnityEngine;

namespace SpellSystem
{
    /// <summary>
    /// 法术属性结构
    /// 包含了一个法术的所有可配置属性
    /// </summary>
    [System.Serializable]
    public struct SpellStats
    {
        // ========== 基础属性 ==========
        [Header("基础属性")]
        [Tooltip("法术伤害")]
        public float damage;

        [Tooltip("法术消耗的魔力")]
        public float manaCost;

        [Tooltip("施法延迟（秒）")]
        public float castDelay;

        // ========== 投射物属性 ==========
        [Header("投射物属性")]
        [Tooltip("投射物速度")]
        public float speed;

        [Tooltip("投射物生命周期（秒）")]
        public float lifetime;

        [Tooltip("投射物大小倍数")]
        public float size;

        [Tooltip("投射物重力影响")]
        public float gravity;

        [Tooltip("穿透次数（可以击中多少个敌人）")]
        public int piercing;

        [Tooltip("弹跳次数")]
        public int bounces;

        // ========== 扩散和精度 ==========
        [Header("扩散和精度")]
        [Tooltip("发射角度偏移（度）")]
        public float spread;

        [Tooltip("是否追踪敌人")]
        public bool isHoming;

        [Tooltip("追踪强度")]
        public float homingStrength;

        // ========== 多重施法 ==========
        [Header("多重施法")]
        [Tooltip("同时发射的投射物数量")]
        public int projectileCount;

        [Tooltip("投射物之间的角度间隔")]
        public float projectileSpread;

        // ========== 触发器属性 ==========
        [Header("触发器属性")]
        [Tooltip("触发延迟（秒）")]
        public float triggerDelay;

        [Tooltip("触发器激活次数（-1为无限）")]
        public int triggerCharges;

        [Tooltip("触发范围")]
        public float triggerRadius;

        // ========== 元素和特效 ==========
        [Header("元素和特效")]
        [Tooltip("元素类型")]
        public ElementType elementType;

        [Tooltip("爆炸范围（0表示无爆炸）")]
        public float explosionRadius;

        [Tooltip("爆炸伤害")]
        public float explosionDamage;

        // ========== 修饰符属性 ==========
        [Header("修饰符属性")]
        [Tooltip("伤害倍数修正")]
        public float damageMultiplier;

        [Tooltip("速度倍数修正")]
        public float speedMultiplier;

        [Tooltip("施法延迟修正")]
        public float castDelayModifier;

        [Tooltip("修饰符是否影响所有后续法术")]
        public bool isPersistentModifier;

        /// <summary>
        /// 创建默认的法术属性
        /// </summary>
        public static SpellStats Default()
        {
            return new SpellStats
            {
                damage = 10f,
                manaCost = 10f,
                castDelay = 0.1f,
                speed = 10f,
                lifetime = 5f,
                size = 1f,
                gravity = 0f,
                piercing = 0,
                bounces = 0,
                spread = 0f,
                isHoming = false,
                homingStrength = 0f,
                projectileCount = 1,
                projectileSpread = 0f,
                triggerDelay = 0f,
                triggerCharges = 1,
                triggerRadius = 1f,
                elementType = ElementType.None,
                explosionRadius = 0f,
                explosionDamage = 0f,
                damageMultiplier = 1f,
                speedMultiplier = 1f,
                castDelayModifier = 0f,
                isPersistentModifier = false
            };
        }

        /// <summary>
        /// 应用修饰符到这个法术属性
        /// </summary>
        public void ApplyModifier(SpellStats modifier)
        {
            // 调试输出
            int oldCount = projectileCount;

            // 乘法修正
            damage *= modifier.damageMultiplier;
            speed *= modifier.speedMultiplier;
            size *= modifier.size;

            // 加法修正
            castDelay += modifier.castDelayModifier;
            piercing += modifier.piercing;
            bounces += modifier.bounces;
            spread += modifier.spread;
            projectileCount += modifier.projectileCount - 1; // 减1因为默认是1

            // 调试输出
            if (modifier.projectileCount != 1)
            {
                Debug.Log($"<color=#FFA94D>[数量修正] {oldCount} + ({modifier.projectileCount} - 1) = {projectileCount}</color>");
            }

            // 特殊属性
            if (modifier.isHoming)
            {
                isHoming = true;
                homingStrength = Mathf.Max(homingStrength, modifier.homingStrength);
            }

            // 元素属性（后来的覆盖前面的）
            if (modifier.elementType != ElementType.None)
            {
                elementType = modifier.elementType;
            }

            // 爆炸属性
            explosionRadius = Mathf.Max(explosionRadius, modifier.explosionRadius);
            explosionDamage += modifier.explosionDamage;
        }

        /// <summary>
        /// 创建此属性的副本
        /// </summary>
        public SpellStats Clone()
        {
            return (SpellStats)this.MemberwiseClone();
        }
    }
}

