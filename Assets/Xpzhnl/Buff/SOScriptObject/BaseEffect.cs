using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LearnUnity
{
  using System;
  using UnityEngine;

  public abstract class BaseEffectConfig : ScriptableObject
  {
    public abstract void ApplyEffect(BuffInfo buffInfo, DamageInfo damageInfo = null);
  }
}
