using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LearnUnity
{
  /// <summary>
  /// 子弹
  /// </summary>
  public class Bullet : MonoBehaviour
  {
    private float bulletFlySpeed = 5f;
    void Awake()
    {
      Destroy(gameObject, 4);
    }

    void Update()
    {
      transform.Translate(Vector2.up * bulletFlySpeed * Time.deltaTime);
    }


  }
}
