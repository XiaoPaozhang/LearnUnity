// ==================================================
// 第三步：法术执行上下文
// ==================================================
// 这个类在法术执行时传递状态信息
// 包括修饰符堆栈、施法者、施法位置等

using System.Collections.Generic;
using UnityEngine;

namespace SpellSystem
{
    /// <summary>
    /// 法术执行上下文
    /// 在法杖施放法术时创建，携带施法所需的所有上下文信息
    /// </summary>
    public class SpellContext
    {
        // ========== 施法者信息 ==========
        /// <summary>施法者游戏对象</summary>
        public GameObject caster;

        /// <summary>施法者的Transform</summary>
        public Transform casterTransform;

        /// <summary>施法位置</summary>
        public Vector2 castPosition;

        /// <summary>施法方向（归一化向量）</summary>
        public Vector2 castDirection;

        // ========== 修饰符系统 ==========
        /// <summary>当前激活的修饰符堆栈</summary>
        private Stack<SpellStats> modifierStack = new Stack<SpellStats>();

        /// <summary>持久性修饰符列表（影响所有后续法术）</summary>
        private List<SpellStats> persistentModifiers = new List<SpellStats>();

        // ========== 施法统计 ==========
        /// <summary>当前施法索引</summary>
        public int currentSpellIndex;

        /// <summary>总施法数量</summary>
        public int totalSpells;

        /// <summary>累积的施法延迟</summary>
        public float accumulatedDelay;

        /// <summary>累积的魔力消耗</summary>
        public float accumulatedManaCost;

        // ========== 构造函数 ==========

        public SpellContext(GameObject caster, Vector2 position, Vector2 direction)
        {
            this.caster = caster;
            this.casterTransform = caster.transform;
            this.castPosition = position;
            this.castDirection = direction.normalized;

            this.currentSpellIndex = 0;
            this.totalSpells = 0;
            this.accumulatedDelay = 0f;
            this.accumulatedManaCost = 0f;
        }

        // ========== 修饰符管理 ==========

        /// <summary>
        /// 添加修饰符到堆栈
        /// </summary>
        public void PushModifier(SpellStats modifier)
        {
            if (modifier.isPersistentModifier)
            {
                persistentModifiers.Add(modifier);
            }
            else
            {
                modifierStack.Push(modifier);
            }
        }

        /// <summary>
        /// 获取组合后的法术属性（应用所有修饰符）
        /// </summary>
        public SpellStats GetModifiedStats(SpellStats baseStats)
        {
            // 创建基础属性的副本
            SpellStats modified = baseStats.Clone();

            // 应用持久性修饰符
            foreach (var persistentMod in persistentModifiers)
            {
                modified.ApplyModifier(persistentMod);
            }

            // 应用堆栈中的修饰符
            foreach (var modifier in modifierStack)
            {
                modified.ApplyModifier(modifier);
            }

            return modified;
        }

        /// <summary>
        /// 清空临时修饰符堆栈（保留持久性修饰符）
        /// </summary>
        public void ClearModifiers()
        {
            modifierStack.Clear();
        }

        /// <summary>
        /// 清空所有修饰符（包括持久性修饰符）
        /// </summary>
        public void ClearAllModifiers()
        {
            int totalCount = modifierStack.Count + persistentModifiers.Count;
            if (totalCount > 0)
            {
                Debug.Log($"<color=#FFA94D>[上下文] 清空 {totalCount} 个残留修饰符</color>");
            }
            modifierStack.Clear();
            persistentModifiers.Clear();
        }

        /// <summary>
        /// 获取当前修饰符数量
        /// </summary>
        public int GetModifierCount()
        {
            return modifierStack.Count + persistentModifiers.Count;
        }

        // ========== 施法统计 ==========

        /// <summary>
        /// 记录施法延迟
        /// </summary>
        public void AddDelay(float delay)
        {
            accumulatedDelay += delay;
        }

        /// <summary>
        /// 记录魔力消耗
        /// </summary>
        public void AddManaCost(float cost)
        {
            accumulatedManaCost += cost;
        }

        // ========== 调试信息 ==========

        /// <summary>
        /// 获取上下文的调试字符串
        /// </summary>
        public override string ToString()
        {
            return $"SpellContext: Caster={caster.name}, " +
                   $"Pos={castPosition}, Dir={castDirection}, " +
                   $"Modifiers={GetModifierCount()}, " +
                   $"Spell={currentSpellIndex}/{totalSpells}";
        }

        /// <summary>
        /// 打印所有修饰符信息（用于调试）
        /// </summary>
        public void DebugPrintModifiers()
        {
            Debug.Log($"=== SpellContext Modifiers ===");
            Debug.Log($"Temporary Modifiers: {modifierStack.Count}");
            foreach (var mod in modifierStack)
            {
                Debug.Log($"  - Damage×{mod.damageMultiplier}, Speed×{mod.speedMultiplier}");
            }

            Debug.Log($"Persistent Modifiers: {persistentModifiers.Count}");
            foreach (var mod in persistentModifiers)
            {
                Debug.Log($"  - Damage×{mod.damageMultiplier}, Speed×{mod.speedMultiplier}");
            }
        }
    }
}

