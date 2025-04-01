using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LearnUnity
{

  [CreateAssetMenu(fileName = "buffData", menuName = "buff", order = 0)]
  public class BuffData : ScriptableObject
  {
    public List<BuffConfig> buffConfigs;
  }

  [Serializable]
  public class BuffConfig
  {
    public int id;
    public string buffName;
    [SerializeField] public Sprite buffIcon;
    public int priority;
    public int maxStack;
    //更新方式
    public E_BuffUpdateTime e_BuffUpdateTime;
    public E_BuffRemoveStackUpdate e_BuffRemoveStackUpdate;

    //时间信息
    public bool isForever;//是否永久buff
    public float duration;
    public float tickTime;

    //回调点
    public BaseEffectConfig onCreate;
    public BaseEffectConfig onRemove;
    public BaseEffectConfig onTick;

    //伤害回调点
    public BaseEffectConfig onHit;
    public BaseEffectConfig onBeHurt;
    public BaseEffectConfig onKill;
    public BaseEffectConfig onBeKill;

  }
}
