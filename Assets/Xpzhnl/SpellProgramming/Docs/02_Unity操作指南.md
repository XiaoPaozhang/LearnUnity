# Unity 操作指南 - 创建法术测试场景

## 📁 第一步：规划文件结构

我们要创建以下目录结构：

```
Assets/Xpzhnl/SpellProgramming/
├── Scripts/              （代码文件）
│   ├── Step1_SpellType.cs
│   ├── Step2_SpellStats.cs
│   ├── Step3_SpellContext.cs
│   ├── Step4_SpellData.cs
│   ├── Step5_Projectile.cs
│   └── Step6_Wand.cs
│
├── SpellData/           （法术ScriptableObject资源）
│   ├── Projectiles/     （投射物法术）
│   │   ├── Fireball.asset
│   │   └── MagicMissile.asset
│   └── Modifiers/       （修饰符法术）
│       ├── DamageBoost.asset
│       └── TripleCast.asset
│
├── Prefabs/             （预制体）
│   ├── Projectiles/     （投射物预制体）
│   │   ├── FireballProjectile.prefab
│   │   └── MagicMissileProjectile.prefab
│   └── Wand.prefab      （法杖预制体）
│
├── Scenes/              （场景）
│   └── TestScene.unity
│
└── Docs/                （文档）
    ├── 00_学习路径.md
    ├── 01_核心概念.md
    └── 02_Unity操作指南.md (本文件)
```

---

## ✅ 当前进度

- [ ] 第1步：整理脚本文件
- [ ] 第2步：创建文件夹结构
- [ ] 第3步：创建投射物预制体
- [ ] 第4步：创建法术数据资源
- [ ] 第5步：在场景中创建法杖
- [ ] 第6步：测试运行

---

## 详细操作步骤（待展开）

每完成一步，我会标记为 ✅

