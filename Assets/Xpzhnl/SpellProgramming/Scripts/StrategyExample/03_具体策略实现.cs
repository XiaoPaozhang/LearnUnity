// ==================================================
// 步骤3：实现具体策略
// ==================================================
// 每种法术类型的具体执行逻辑

using UnityEngine;
using SpellSystem;

namespace SpellSystemStrategy
{
    // ========== 投射物策略 ==========

    /// <summary>
    /// 投射物法术的执行策略
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileBehavior", menuName = "SpellSystem V2/Behaviors/Projectile")]
    public class ProjectileBehavior : SpellBehaviorBase
    {
        public override void Execute(SpellContext context, SpellDataV2 spellData)
        {
            // 获取投射物专属属性
            ProjectileSpellStats stats = spellData.projectileStats;

            // 检查预制体
            if (stats.projectilePrefab == null)
            {
                Debug.LogWarning($"投射物法术 {spellData.baseStats.spellName} 缺少预制体！");
                return;
            }

            // 计算发射数量（可能被修饰符影响）
            int count = Mathf.Max(1, stats.projectilePrefab ? 1 : 0);

            // 生成投射物
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = CalculateDirection(context, i, count, stats);
                CreateProjectile(context, stats, direction);
            }

            SpellLogger.LogSuccess($"发射投射物: {spellData.baseStats.spellName}");
        }

        Vector2 CalculateDirection(SpellContext context, int index, int total, ProjectileSpellStats stats)
        {
            float angleOffset = 0f;

            if (total > 1)
            {
                // 均匀分布
                float totalSpread = 30f; // 可以从修饰符获取
                angleOffset = -totalSpread / 2f + (totalSpread / (total - 1)) * index;
            }

            angleOffset += Random.Range(-stats.spread, stats.spread);
            return RotateVector(context.castDirection, angleOffset);
        }

        void CreateProjectile(SpellContext context, ProjectileSpellStats stats, Vector2 direction)
        {
            GameObject obj = Instantiate(stats.projectilePrefab, context.castPosition, Quaternion.identity);

            // 设置方向
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.Euler(0, 0, angle);
            obj.transform.localScale = Vector3.one * stats.size;

            // 初始化（简化版，实际应该转换属性）
            var projectile = obj.GetComponent<Step5_Projectile>();
            if (projectile != null)
            {
                // 这里需要将 ProjectileSpellStats 转换为 SpellStats
                // 实际项目中可能需要适配器模式
                SpellStats oldStats = ConvertToOldStats(stats);
                projectile.Initialize(context.caster, oldStats, direction);
            }
        }

        // 临时转换方法（实际项目中应该更优雅）
        SpellStats ConvertToOldStats(ProjectileSpellStats newStats)
        {
            SpellStats oldStats = SpellStats.Default();
            oldStats.damage = newStats.damage;
            oldStats.speed = newStats.speed;
            oldStats.lifetime = newStats.lifetime;
            oldStats.size = newStats.size;
            oldStats.piercing = newStats.piercing;
            return oldStats;
        }

        public override string GetDescription(SpellDataV2 spellData)
        {
            var stats = spellData.projectileStats;
            return $"{spellData.baseStats.spellName}\n" +
                   $"伤害: {stats.damage}\n" +
                   $"速度: {stats.speed}\n" +
                   $"生命: {stats.lifetime}s";
        }
    }

    // ========== 修饰符策略 ==========

    /// <summary>
    /// 修饰符法术的执行策略
    /// </summary>
    [CreateAssetMenu(fileName = "ModifierBehavior", menuName = "SpellSystem V2/Behaviors/Modifier")]
    public class ModifierBehavior : SpellBehaviorBase
    {
        public override void Execute(SpellContext context, SpellDataV2 spellData)
        {
            // 获取修饰符专属属性
            ModifierSpellStats stats = spellData.modifierStats;

            // 将修饰符转换为旧格式并压入栈
            // 实际项目中应该重构 SpellContext 来支持新格式
            SpellStats oldStats = ConvertToOldStats(stats);
            context.PushModifier(oldStats);

            SpellLogger.LogModifierApply(
                spellData.baseStats.spellName,
                stats.damageMultiplier,
                stats.speedMultiplier,
                stats.projectileCount
            );
        }

        SpellStats ConvertToOldStats(ModifierSpellStats newStats)
        {
            SpellStats oldStats = SpellStats.Default();
            oldStats.damageMultiplier = newStats.damageMultiplier;
            oldStats.speedMultiplier = newStats.speedMultiplier;
            oldStats.projectileCount = newStats.projectileCount;
            oldStats.projectileSpread = newStats.projectileSpread;
            oldStats.isPersistentModifier = newStats.isPersistent;
            return oldStats;
        }

        public override string GetDescription(SpellDataV2 spellData)
        {
            var stats = spellData.modifierStats;
            string desc = $"{spellData.baseStats.spellName}\n";

            if (stats.damageMultiplier != 1f)
                desc += $"伤害 ×{stats.damageMultiplier}\n";
            if (stats.speedMultiplier != 1f)
                desc += $"速度 ×{stats.speedMultiplier}\n";
            if (stats.projectileCount > 1)
                desc += $"数量 ×{stats.projectileCount}\n";

            return desc;
        }
    }

    // ========== 触发器策略 ==========

    /// <summary>
    /// 触发器法术的执行策略
    /// </summary>
    [CreateAssetMenu(fileName = "TriggerBehavior", menuName = "SpellSystem V2/Behaviors/Trigger")]
    public class TriggerBehavior : SpellBehaviorBase
    {
        public override void Execute(SpellContext context, SpellDataV2 spellData)
        {
            TriggerSpellStats stats = spellData.triggerStats;

            if (stats.carrierPrefab == null)
            {
                Debug.LogWarning($"触发器法术 {spellData.baseStats.spellName} 缺少载体！");
                return;
            }

            // 创建触发器载体
            GameObject carrier = Instantiate(stats.carrierPrefab, context.castPosition, Quaternion.identity);

            // 设置触发器属性（简化版）
            Debug.Log($"创建触发器: {spellData.baseStats.spellName}, 类型: {stats.triggerType}");

            // 实际项目中应该：
            // 1. 给载体添加触发器组件
            // 2. 配置触发条件
            // 3. 设置触发时要执行的法术
        }

        public override string GetDescription(SpellDataV2 spellData)
        {
            var stats = spellData.triggerStats;
            return $"{spellData.baseStats.spellName}\n" +
                   $"触发: {stats.triggerType}\n" +
                   $"延迟: {stats.triggerDelay}s\n" +
                   $"次数: {stats.triggerCharges}";
        }
    }

    // ========== 对比说明 ==========

    /*
     * 【对比现有系统】
     * 
     * 现有系统（Step4_SpellData.cs）：
     * ────────────────────────────────
     * public class SpellData : ScriptableObject
     * {
     *     void ExecuteProjectile(...) { ... }
     *     void ExecuteModifier(...) { ... }
     *     void ExecuteTrigger(...) { ... }
     *     // 所有逻辑都在一个类中
     * }
     * 
     * 新系统（策略模式）：
     * ────────────────────────────────
     * ProjectileBehavior.cs   - 只处理投射物逻辑
     * ModifierBehavior.cs     - 只处理修饰符逻辑
     * TriggerBehavior.cs      - 只处理触发器逻辑
     * 
     * 优点：
     * - 单一职责：每个策略类只负责一种法术
     * - 易于扩展：新增法术类型不影响其他
     * - 易于测试：可以单独测试每个策略
     * - 易于复用：不同法术可以共享策略
     */
}



