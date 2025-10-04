// ==================================================
// 第六步：法杖系统
// ==================================================
// 法杖是法术的容器和执行器

using System.Collections.Generic;
using UnityEngine;

namespace SpellSystem
{
    /// <summary>
    /// 法杖组件 - 管理和执行法术
    /// </summary>
    [AddComponentMenu("Spell System/Wand")]
    public class Step6_Wand : MonoBehaviour
    {
        // ========== 法术列表 ==========
        [Header("法术配置")]
        [Tooltip("法杖中的法术列表（按顺序执行）")]
        public List<Step4_SpellData> spells = new List<Step4_SpellData>();

        // ========== 法杖属性 ==========
        [Header("法杖属性")]
        [Tooltip("施法延迟（秒）")]
        public float castDelay = 0.1f;

        [Tooltip("法杖是否在冷却中")]
        private bool isOnCooldown = false;

        // ========== 施法方法 ==========

        /// <summary>
        /// 施放法术（主要方法）
        /// </summary>
        public void CastSpells()
        {
            // 检查是否在冷却
            if (isOnCooldown)
            {
                SpellLogger.LogSystem("法杖冷却中...");
                return;
            }

            // 检查是否有法术
            if (spells.Count == 0)
            {
                SpellLogger.LogWarning("法杖中没有法术！");
                return;
            }

            SpellLogger.LogSpellCastStart(spells.Count);

            // 创建施法上下文（每次都是全新的）
            SpellContext context = CreateContext();
            context.ClearAllModifiers(); // 确保没有残留

            // 依次执行每个法术
            for (int i = 0; i < spells.Count; i++)
            {
                Step4_SpellData spell = spells[i];
                if (spell == null) continue;

                context.currentSpellIndex = i;
                SpellLogger.LogSpellExecute(i, spell.spellName, spell.spellType.ToString());

                // 执行法术
                spell.Execute(context);
            }

            SpellLogger.LogSpellCastEnd();

            // 开始冷却
            StartCooldown();
        }

        /// <summary>
        /// 创建施法上下文
        /// </summary>
        SpellContext CreateContext()
        {
            // 获取施法位置和方向
            Vector2 position = transform.position;
            Vector2 direction = transform.right; // 使用物体的右方向

            // 创建上下文
            SpellContext context = new SpellContext(gameObject, position, direction);
            context.totalSpells = spells.Count;

            return context;
        }

        /// <summary>
        /// 开始冷却
        /// </summary>
        void StartCooldown()
        {
            isOnCooldown = true;
            Invoke(nameof(EndCooldown), castDelay);
        }

        /// <summary>
        /// 结束冷却
        /// </summary>
        void EndCooldown()
        {
            isOnCooldown = false;
        }

        // ========== 测试快捷键 ==========

        void Update()
        {
            // 按空格键施法（用于测试）
            if (Input.GetKeyDown(KeyCode.Space))
            {
                CastSpells();
            }
        }

        // ========== 辅助方法 ==========

        /// <summary>
        /// 添加法术到法杖
        /// </summary>
        public void AddSpell(Step4_SpellData spell)
        {
            if (spell != null)
            {
                spells.Add(spell);
                SpellLogger.LogSuccess($"添加法术: {spell.spellName}");
            }
        }

        /// <summary>
        /// 清空法杖中的所有法术
        /// </summary>
        public void ClearSpells()
        {
            spells.Clear();
            SpellLogger.LogSystem("清空法杖");
        }
    }
}

