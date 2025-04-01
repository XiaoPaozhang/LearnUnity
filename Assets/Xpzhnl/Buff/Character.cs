using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LearnUnity
{
  public class Character : MonoBehaviour
  {
    [SerializeField] public Property property;
    /// <summary>
    /// 是否死亡
    /// </summary>
    public bool IsDead => property.hp <= 0;

    void Start()
    {
      GetComponent<BuffHandle>().AddBuff(new BuffInfo(
        Resources.Load<BuffData>("SOData/buffData").buffConfigs[0],
        this.gameObject,
        0,
        0,
        1
      ));
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
      property.hp -= damageInfo.damage;
    }
  }
}
