using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LearnUnity
{
  public class DamageMgr : MonoBehaviour
  {
    private DamageMgr Instance;
    void Awake()
    {
      if (!Instance)
      {
        Instance = this;
      }
      else
      {
        Destroy(gameObject);
      }
    }

    public void SubmitDamage(DamageInfo damageInfo)
    {
      BuffHandle creatorBuffHandle = damageInfo.creator?.GetComponent<BuffHandle>();
      BuffHandle targetBuffHandle = damageInfo.target?.GetComponent<BuffHandle>();
      Character targetCharacter = damageInfo.target?.GetComponent<Character>();

      if (creatorBuffHandle)
      {
        foreach (var buffInfo in creatorBuffHandle.buffInfos)
        {
          buffInfo.buffConfig.onHit?.ApplyEffect(buffInfo, damageInfo);
        }
      }

      if (targetBuffHandle)
      {
        foreach (var buffInfo in targetBuffHandle.buffInfos)
        {
          buffInfo.buffConfig.onBeHurt?.ApplyEffect(buffInfo, damageInfo);
        }
        if (targetCharacter)
        {
          targetCharacter.TakeDamage(damageInfo);
          if (targetCharacter.IsDead)
          {

            foreach (var buffInfo in targetBuffHandle.buffInfos)
            {
              buffInfo.buffConfig.onBeKill?.ApplyEffect(buffInfo, damageInfo);
            }

            //再次判断是否被杀死,有可能onbekill中有免死的东西
            if (targetCharacter.IsDead)
            {

              foreach (var buffInfo in targetBuffHandle.buffInfos)
              {
                buffInfo.buffConfig.onKill?.ApplyEffect(buffInfo, damageInfo);
              }
            }
          }
        }
      }
    }
  }
}
