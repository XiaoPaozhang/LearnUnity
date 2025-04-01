using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LearnUnity
{
  public enum E_BuffUpdateTime
  {
    Keep,
    Add,
    Replace,
  }

  public enum E_BuffRemoveStackUpdate
  {
    Clear,
    Reduce,
  }

  /// <summary>
  /// buff在游戏运行时创建的数据
  /// </summary>
  public class BuffInfo
  {
    public BuffConfig buffConfig;
    public GameObject creator;
    public GameObject target;
    public float curDuration;
    public float curTickTime;
    public float curStack;

    public BuffInfo(
      BuffConfig buffConfig,
      GameObject target,
      float curDuration,
      float curTickTime,
      float curStack,
      GameObject creator = null
      )
    {
      this.buffConfig = buffConfig;
      this.target = target;
      this.curDuration = curDuration;
      this.curTickTime = curTickTime;
      this.curStack = curStack;
    }

    public override string ToString()
    {
      return @$"Buff信息: [
      Buff配置ID: {buffConfig?.id},
      Buff配置名称: {buffConfig?.buffName},
      目标: {target.name},
      创建者: {creator?.name},
      配置持续时间: {buffConfig.duration},
      当前持续时间: {curDuration},
      层数: {curStack}
      ]";
    }
  }

  /// <summary>
  /// 模拟玩家熟悉
  /// </summary>
  [Serializable]
  public class Property
  {
    private int Hp;
    public int hp
    {
      get
      {
        return Hp;
      }
      set
      {
        Hp = Math.Clamp(value, 0, int.MaxValue);
      }
    }
    public int atk;
    public int speed;
  }
}
