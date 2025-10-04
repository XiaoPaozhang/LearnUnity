// ==================================================
// 法杖测试器 - 快速切换法术组合
// ==================================================

using UnityEngine;
using SpellSystem;

[AddComponentMenu("Spell System/Wand Tester")]
public class WandTester : MonoBehaviour
{
  [Header("引用")]
  public Step6_Wand wand;

  [Header("法术库")]
  public Step4_SpellData fireball;
  public Step4_SpellData magicMissile;
  public Step4_SpellData damageBoost;
  public Step4_SpellData tripleCast;

  void Update()
  {
    // 数字键快速切换组合
    if (Input.GetKeyDown(KeyCode.Alpha1))
    {
      SetSpells("基础火球", fireball);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha2))
    {
      SetSpells("增强火球", damageBoost, fireball);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha3))
    {
      SetSpells("三重火球", tripleCast, fireball);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha4))
    {
      SetSpells("增强三重火球", damageBoost, tripleCast, fireball);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha5))
    {
      SetSpells("双倍增强火球", damageBoost, damageBoost, fireball);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha6))
    {
      SetSpells("三重魔法飞弹", tripleCast, magicMissile);
    }

    // C键清空法术
    if (Input.GetKeyDown(KeyCode.C))
    {
      wand.ClearSpells();
      SpellSystem.SpellLogger.LogWarning("法杖已清空");
    }

    // R键重新加载当前组合（用于修复运行时修改的问题）
    if (Input.GetKeyDown(KeyCode.R))
    {
      RefreshCurrentSpells();
    }
  }

  private string currentComboName = "";
  private Step4_SpellData[] currentSpells = null;

  void SetSpells(string name, params Step4_SpellData[] spells)
  {
    currentComboName = name;
    currentSpells = spells;

    wand.ClearSpells();
    foreach (var spell in spells)
    {
      if (spell != null)
        wand.AddSpell(spell);
    }
    SpellSystem.SpellLogger.LogSpellComboChange(name);
    SpellSystem.SpellLogger.LogSeparator();
  }

  void RefreshCurrentSpells()
  {
    if (currentSpells != null && currentSpells.Length > 0)
    {
      wand.ClearSpells();
      foreach (var spell in currentSpells)
      {
        if (spell != null)
          wand.AddSpell(spell);
      }
      SpellSystem.SpellLogger.LogSuccess($"重新加载: {currentComboName}");
    }
    else
    {
      SpellSystem.SpellLogger.LogWarning("没有可重新加载的组合");
    }
  }

  void OnGUI()
  {
    // 在屏幕上显示提示
    GUI.Box(new Rect(10, 10, 300, 260), "法术测试快捷键");

    GUILayout.BeginArea(new Rect(20, 40, 280, 240));
    GUILayout.Label("空格键 - 施放法术");
    GUILayout.Label("");
    GUILayout.Label("1 - 基础火球");
    GUILayout.Label("2 - 增强火球");
    GUILayout.Label("3 - 三重火球");
    GUILayout.Label("4 - 增强三重火球");
    GUILayout.Label("5 - 双倍增强火球");
    GUILayout.Label("6 - 三重魔法飞弹");
    GUILayout.Label("");
    GUILayout.Label("C - 清空法杖");
    GUILayout.Label("R - 重新加载组合");
    GUILayout.Label("");
    GUILayout.Label("<color=yellow>⚠ 运行时不要手动拖资源！</color>");
    GUILayout.EndArea();
  }
}

