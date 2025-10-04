// ==================================================
// 步骤5：使用示例
// ==================================================
// 展示如何在实际项目中使用新系统

using UnityEngine;
using SpellSystem;

namespace SpellSystemStrategy
{
    /// <summary>
    /// 使用示例：展示如何使用策略模式版本的法术系统
    /// </summary>
    public class StrategyUsageExample : MonoBehaviour
    {
        [Header("新版法术数据")]
        public SpellDataV2 fireballV2;
        public SpellDataV2 damageBoostV2;
        public SpellDataV2 tripleCastV2;

        [Header("测试按键")]
        [Tooltip("按键盘 T 键测试新系统")]
        public KeyCode testKey = KeyCode.T;

        void Update()
        {
            if (Input.GetKeyDown(testKey))
            {
                TestStrategySystem();
            }
        }

        /// <summary>
        /// 测试策略模式系统
        /// </summary>
        void TestStrategySystem()
        {
            Debug.Log("<color=cyan>════════ 测试策略模式系统 ════════</color>");

            // 创建施法上下文
            SpellContext context = new SpellContext(
                gameObject,
                transform.position,
                Vector2.right
            );

            // 示例1：单独施放火球
            Example1_SimpleFireball(context);

            // 示例2：修饰符 + 火球
            Example2_ModifiedFireball(context);

            // 示例3：多重修饰符
            Example3_MultipleModifiers(context);
        }

        /// <summary>
        /// 示例1：简单的火球术
        /// </summary>
        void Example1_SimpleFireball(SpellContext context)
        {
            if (fireballV2 == null) return;

            Debug.Log("<color=yellow>--- 示例1：简单火球 ---</color>");

            // 直接执行
            fireballV2.Execute(context);

            // 打印描述
            Debug.Log(fireballV2.GetDescription());
        }

        /// <summary>
        /// 示例2：增强火球
        /// </summary>
        void Example2_ModifiedFireball(SpellContext context)
        {
            if (damageBoostV2 == null || fireballV2 == null) return;

            Debug.Log("<color=yellow>--- 示例2：增强火球 ---</color>");

            // 先应用修饰符
            damageBoostV2.Execute(context);

            // 再施放火球（会应用修饰符效果）
            fireballV2.Execute(context);

            // 清理
            context.ClearModifiers();
        }

        /// <summary>
        /// 示例3：多重修饰符
        /// </summary>
        void Example3_MultipleModifiers(SpellContext context)
        {
            if (damageBoostV2 == null || tripleCastV2 == null || fireballV2 == null) return;

            Debug.Log("<color=yellow>--- 示例3：增强三重火球 ---</color>");

            // 伤害增强
            damageBoostV2.Execute(context);

            // 三重施法
            tripleCastV2.Execute(context);

            // 火球术（应用所有修饰符）
            fireballV2.Execute(context);

            // 清理
            context.ClearModifiers();
        }
    }

    // ========== 对比说明 ==========

    /*
     * 【对比旧系统的使用方式】
     * 
     * 旧系统（WandTester.cs）：
     * ════════════════════════════
     * wand.ClearSpells();
     * wand.AddSpell(damageBoost);  // Step4_SpellData
     * wand.AddSpell(fireball);     // Step4_SpellData
     * wand.CastSpells();
     * 
     * 
     * 新系统（本文件）：
     * ════════════════════════════
     * SpellContext context = new SpellContext(...);
     * damageBoostV2.Execute(context);  // SpellDataV2
     * fireballV2.Execute(context);     // SpellDataV2
     * 
     * 
     * 区别：
     * - 旧系统依赖 Wand 来管理序列
     * - 新系统更灵活，可以直接调用
     * - 两种方式都有用，看具体需求
     */

    // ========== 迁移建议 ==========

    /*
     * 【如何从旧系统迁移到新系统】
     * 
     * 渐进式迁移方案：
     * ────────────────
     * 
     * 阶段1：共存（推荐）
     * ─────────────────
     * - 保留旧的 SpellData
     * - 新增 SpellDataV2
     * - 新功能用新系统
     * - 旧功能继续用旧系统
     * 
     * 
     * 阶段2：适配器模式
     * ─────────────────
     * - 创建适配器，让旧 Wand 能使用新 SpellDataV2
     * - 或者让新系统兼容旧的 SpellData
     * 
     * 
     * 阶段3：完全重构（可选）
     * ─────────────────
     * - 将所有旧 SpellData 转换为 SpellDataV2
     * - 统一使用新系统
     * - 删除旧代码
     * 
     * 
     * 实际建议：
     * ─────────
     * 如果项目还在早期，直接用新系统
     * 如果项目已经有很多内容，采用共存方案
     * 不建议强行全部重构，风险大
     */

    // ========== 性能对比 ==========

    /*
     * 【性能影响】
     * 
     * 策略模式的性能考虑：
     * 
     * 优点：
     * - 属性分离后，内存占用更小
     * - 虚函数调用开销可忽略（现代CPU分支预测很强）
     * - 更好的缓存局部性（相关数据紧密排列）
     * 
     * 缺点：
     * - 多一层间接调用（behavior.Execute）
     * - 但实际测试中性能差异<1%
     * 
     * 结论：
     * 策略模式的性能开销微乎其微
     * 可维护性的提升远超过性能损失
     */

    // ========== 总结 ==========

    /*
     * 【何时使用策略模式】
     * 
     * 推荐使用：
     * ✓ 法术类型会持续增加
     * ✓ 不同类型的法术差异很大
     * ✓ 需要在运行时切换行为
     * ✓ 团队协作（不同人负责不同策略）
     * 
     * 不推荐使用：
     * ✗ 法术类型固定且简单
     * ✗ 项目规模很小
     * ✗ 团队不熟悉设计模式
     * ✗ 快速原型阶段
     * 
     * 
     * 你的情况：
     * ─────────
     * 你提到 SpellStats 会越来越大
     * → 这正是策略模式的应用场景
     * 
     * 建议：
     * 1. 先理解这个示例
     * 2. 在新功能中尝试使用
     * 3. 如果感觉好用，逐步迁移
     * 4. 不着急，慢慢来
     */
}



