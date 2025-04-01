using UnityEngine;

namespace LearnUnity
{
  [CreateAssetMenu(fileName = "EffectData", menuName = "修改属性效果", order = 1)]
  public class ChangePropertyEffect : BaseEffectConfig
  {
    public Property property;
    public override void ApplyEffect(BuffInfo buffInfo, DamageInfo damageInfo = null)
    {
      var character = buffInfo.target.GetComponent<Character>();
      if (character)
      {
        character.property.atk += property.atk;
        character.property.hp += property.hp;
        character.property.speed += property.speed;
      }
    }
  }
}