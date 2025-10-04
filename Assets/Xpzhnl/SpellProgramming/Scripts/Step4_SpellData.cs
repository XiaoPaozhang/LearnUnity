// ==================================================
// 第四步：法术数据
// ==================================================
// 这是用来存储法术信息的数据容器

using UnityEngine;

namespace SpellSystem
{
    /// <summary>
    /// 法术数据 - 可以在Unity编辑器中创建
    /// 右键 -> Create -> Spell System -> Spell Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpell", menuName = "Spell System/Spell Data", order = 0)]
    public class Step4_SpellData : ScriptableObject
    {
        // ========== 基本信息 ==========
        [Header("基本信息")]
        public string spellName = "新法术";

        [TextArea(2, 4)]
        public string description = "法术描述";

        public SpellType spellType = SpellType.Projectile;

        // ========== 法术属性 ==========
        [Header("法术属性")]
        public SpellStats stats = SpellStats.Default();

        // ========== 投射物设置 ==========
        [Header("投射物设置（仅投射物法术）")]
        public GameObject projectilePrefab;

        // ========== 执行方法 ==========

        /// <summary>
        /// 执行法术
        /// </summary>
        public void Execute(SpellContext context)
        {
            // 获取应用修饰符后的属性
            SpellStats modifiedStats = context.GetModifiedStats(stats);

            // 根据法术类型执行
            switch (spellType)
            {
                case SpellType.Projectile:
                    CastProjectile(context, modifiedStats);
                    break;

                case SpellType.Modifier:
                    ApplyModifier(context, modifiedStats);
                    break;

                default:
                    Debug.Log($"执行法术: {spellName}");
                    break;
            }
        }

        /// <summary>
        /// 发射投射物
        /// </summary>
        void CastProjectile(SpellContext context, SpellStats modifiedStats)
        {
            if (projectilePrefab == null)
            {
                SpellLogger.LogWarning($"法术 {spellName} 没有投射物预制体！");
                return;
            }

            // 发射多个投射物
            int count = Mathf.Max(1, modifiedStats.projectileCount);

            for (int i = 0; i < count; i++)
            {
                // 计算发射角度
                float angleOffset = CalculateAngleOffset(i, count, modifiedStats);
                Vector2 direction = RotateVector(context.castDirection, angleOffset);

                // 创建投射物
                CreateProjectile(context.castPosition, direction, modifiedStats);
            }

            // 清空修饰符
            context.ClearModifiers();
        }

        /// <summary>
        /// 创建单个投射物
        /// </summary>
        void CreateProjectile(Vector2 position, Vector2 direction, SpellStats stats)
        {
            GameObject obj = Instantiate(projectilePrefab, position, Quaternion.identity);

            // 设置旋转
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.Euler(0, 0, angle);

            // 设置大小
            obj.transform.localScale = Vector3.one * stats.size;

            // 初始化投射物
            Step5_Projectile projectile = obj.GetComponent<Step5_Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(null, stats, direction);
            }
        }

        /// <summary>
        /// 应用修饰符
        /// </summary>
        void ApplyModifier(SpellContext context, SpellStats modifiedStats)
        {
            context.PushModifier(modifiedStats);
            SpellLogger.LogModifierApply(
                spellName,
                modifiedStats.damageMultiplier,
                modifiedStats.speedMultiplier,
                modifiedStats.projectileCount
            );
        }

        // ========== 辅助方法 ==========

        /// <summary>
        /// 计算发射角度偏移
        /// </summary>
        float CalculateAngleOffset(int index, int total, SpellStats stats)
        {
            float angleOffset = 0f;

            if (total > 1)
            {
                // 均匀分布
                float totalSpread = stats.projectileSpread;
                angleOffset = -totalSpread / 2f + (totalSpread / (total - 1)) * index;
            }

            // 添加随机扩散
            angleOffset += Random.Range(-stats.spread, stats.spread);

            return angleOffset;
        }

        /// <summary>
        /// 旋转向量
        /// </summary>
        Vector2 RotateVector(Vector2 vector, float degrees)
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
}

